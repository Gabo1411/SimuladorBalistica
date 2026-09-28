using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel UI que muestra el historial de disparos guardados en UGS Cloud Save.
/// Estructura esperada:
///   HistoryPanel
///   ├── TitleLabel      (TMP_Text)
///   ├── StatusLabel     (TMP_Text)
///   ├── ScrollView
///   │   └── Viewport → Content → HistoryText (TMP_Text)
///   ├── RefreshButton   (Button)
///   └── CloseButton     (Button)
/// </summary>
public class HistoryPanel : MonoBehaviour
{
    // ── Referencias UI ─────────────────────────────────────────────────────────
    [Header("Referencias")]
    public TMP_Text  StatusLabel;    // "Cargando..." / "10 disparos"
    public TMP_Text  HistoryText;    // Texto con todos los registros
    public Button    RefreshButton;  // Recargar desde la nube
    public Button    CloseButton;    // Cerrar el panel

    // ── Estado ─────────────────────────────────────────────────────────────────
    private bool _isLoading = false;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        RefreshButton?.onClick.AddListener(OnRefreshPressed);
        CloseButton?.onClick.AddListener(OnClosePressed);
    }

    private void OnEnable()
    {
        // No cargar automáticamente — UGS puede no estar listo todavía.
        // El usuario debe presionar "Actualizar".
        if (HistoryText != null) HistoryText.text = "Presiona 'Actualizar' para cargar el historial.";
        SetStatus("Listo");
    }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    private async void OnRefreshPressed()
    {
        if (_isLoading) return;

        // Verificar UGS
        if (UGSManager.Instance == null || !UGSManager.Instance.IsInitialized)
        {
            SetStatus("UGS no esta listo. Intenta en un momento.");
            return;
        }

        // AVISO SALVAVIDAS: Si olvidó conectar el texto en el inspector
        if (HistoryText == null)
        {
            SetStatus("ERROR: Falta conectar HistoryText!");
            Debug.LogError("[HistoryPanel] OJO: No arrastraste el componente de texto al campo 'History Text' en el Inspector.");
            return; // Cortamos acá para que el usuario se dé cuenta
        }

        _isLoading = true;
        SetStatus("Cargando historial...");
        HistoryText.text = "Buscando en la nube...";
        
        if (RefreshButton != null) RefreshButton.interactable = false;

        try
        {
            List<ShotRecord> records = await UGSManager.Instance.LoadHistoryAsync();
            DisplayRecords(records);
        }
        catch (System.Exception e)
        {
            SetStatus("Error al cargar de la nube");
            HistoryText.text = "Error: " + e.Message;
            Debug.LogError("[HistoryPanel] Excepción: " + e);
        }
        finally
        {
            // ESTO ES CLAVE: Siempre vuelve a habilitar el botón, pase lo que pase
            if (RefreshButton != null) RefreshButton.interactable = true;
            _isLoading = false;
        }
    }

    private void OnClosePressed()
    {
        gameObject.SetActive(false);
    }

    // ── Mostrar registros ──────────────────────────────────────────────────────

    private void DisplayRecords(List<ShotRecord> records)
    {
        if (records == null || records.Count == 0)
        {
            SetStatus("No hay disparos guardados.");
            if (HistoryText != null) HistoryText.text = "Realiza un disparo para ver el historial aqui.";
            return;
        }

        SetStatus($"{records.Count} disparos guardados");

        // Mostrar del más reciente al más antiguo
        records.Reverse();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int index = records.Count;

        foreach (var r in records)
        {
            string hit    = r.WasHit ? "IMPACTO" : "FALLO";
            string pieces = r.PiecesKnocked > 0 ? $"{r.PiecesKnocked} piezas" : "0 piezas";

            sb.AppendLine($"-- Disparo #{index} --");
            sb.AppendLine($"  Fecha:   {r.Timestamp} UTC");
            sb.AppendLine($"  Angulo:  {r.Angle:F1}  | Fuerza: {r.Force:F0} | Masa: {r.Mass} kg");
            sb.AppendLine($"  Result:  {hit} | Dist: {r.DistanceToCenter:F2} m | {pieces}");
            sb.AppendLine($"  Puntaje: {r.Score:F0} pts");
            sb.AppendLine();
            index--;
        }

        if (HistoryText != null)
            HistoryText.text = sb.ToString();
    }

    // ── Auxiliares ─────────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (StatusLabel != null) StatusLabel.text = msg;
        Debug.Log($"[HistoryPanel] {msg}");
    }
}
