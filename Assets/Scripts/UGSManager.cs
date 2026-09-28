using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

/// <summary>
/// Singleton que gestiona la integración con Unity Game Services:
/// - Inicialización (Core + Authentication anónima)
/// - Guardado de disparos en Cloud Save
/// - Carga del historial desde Cloud Save
/// </summary>
public class UGSManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static UGSManager Instance { get; private set; }

    // ── Clave de Cloud Save ────────────────────────────────────────────────────
    private const string HISTORY_KEY    = "shot_history";
    private const int    MAX_RECORDS    = 50; // límite de registros guardados

    // ── Estado ─────────────────────────────────────────────────────────────────
    private ShotHistory _localHistory   = new ShotHistory();
    private bool        _isInitialized  = false;

    public bool IsInitialized => _isInitialized;

    // ── Eventos ────────────────────────────────────────────────────────────────
    /// <summary>Se dispara con mensajes de estado para mostrar en la UI.</summary>
    public event Action<string> OnStatusChanged;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    // ── Inicialización ─────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa Unity Services y realiza sign-in anónimo.
    /// Llamar al inicio del juego.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            NotifyStatus("Conectando a UGS...");

            // Inicializar el core de Unity Services
            await UnityServices.InitializeAsync();

            // Sign-in anónimo (no requiere cuenta del jugador)
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _isInitialized = true;
            string pid = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[UGS] Conectado. Player ID: {pid}");
            NotifyStatus($"UGS OK");
        }
        catch (Exception e)
        {
            _isInitialized = false;
            Debug.LogError($"[UGS] Error al inicializar: {e.Message}");
            NotifyStatus("UGS sin conexion");
        }
    }

    // ── Guardar ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Guarda un nuevo registro de disparo en Cloud Save.
    /// Carga el historial existente, agrega el nuevo registro y vuelve a guardar.
    /// </summary>
    public async Task SaveShotAsync(ShotRecord record)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[UGS] No inicializado. No se pudo guardar el disparo.");
            return;
        }

        try
        {
            NotifyStatus("Guardando disparo...");

            // Primero cargar el historial actual de la nube
            await PullHistoryFromCloudAsync();

            // Agregar el nuevo registro
            record.Id        = Guid.NewGuid().ToString();
            record.Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            _localHistory.Records.Add(record);

            // Mantener máximo MAX_RECORDS
            while (_localHistory.Records.Count > MAX_RECORDS)
                _localHistory.Records.RemoveAt(0);

            // Guardar en Cloud Save usando serialización nativa de UGS
            await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, object> { { HISTORY_KEY, _localHistory } }
            );

            Debug.Log($"[UGS] Disparo guardado. Total: {_localHistory.Records.Count} registros.");
            NotifyStatus($"Guardado ({_localHistory.Records.Count} disparos)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al guardar: {e.Message}");
            NotifyStatus("Error al guardar");
        }
    }

    // ── Cargar ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Carga el historial de disparos desde Cloud Save y lo devuelve.
    /// </summary>
    public async Task<List<ShotRecord>> LoadHistoryAsync()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[UGS] No inicializado. No se pudo cargar el historial.");
            return new List<ShotRecord>();
        }

        try
        {
            NotifyStatus("Cargando historial...");
            await PullHistoryFromCloudAsync();
            NotifyStatus($"{_localHistory.Records.Count} disparos cargados");
            return new List<ShotRecord>(_localHistory.Records);
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al cargar: {e.Message}");
            NotifyStatus("Error al cargar");
            return new List<ShotRecord>();
        }
    }

    // ── Interno ────────────────────────────────────────────────────────────────

    private async Task PullHistoryFromCloudAsync()
    {
        try
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { HISTORY_KEY }
            );

            if (result.TryGetValue(HISTORY_KEY, out var item))
            {
                try
                {
                    // Intentar cargar usando la serialización nativa de UGS
                    _localHistory = item.Value.GetAs<ShotHistory>();
                }
                catch
                {
                    // Si falla por el formato string de las pruebas anteriores, lo reiniciamos
                    Debug.LogWarning("[UGS] Formato antiguo detectado. Reiniciando historial local.");
                    _localHistory = new ShotHistory();
                }

                if (_localHistory == null) _localHistory = new ShotHistory();
            }
            else
            {
                // Primera vez: no hay datos guardados todavía
                _localHistory = new ShotHistory();
            }
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogError($"[UGS] Validation error: {e.Message}");
            _localHistory = new ShotHistory();
        }
        catch (Exception e) when (e.Message.Contains("not found") || e.Message.Contains("404"))
        {
            // Clave inexistente → primera sesión
            _localHistory = new ShotHistory();
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] PullHistory error: {e.Message}");
        }
    }

    private void NotifyStatus(string message) => OnStatusChanged?.Invoke(message);
}
