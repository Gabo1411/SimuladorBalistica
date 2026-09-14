using UnityEngine;

/// <summary>
/// Singleton principal del simulador balístico.
/// Coordina el estado del juego, los parámetros de disparo y la comunicación entre sistemas.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Estado del juego ───────────────────────────────────────────────────────
    public enum GameState { WaitingToShoot, ProjectileInFlight, ShowingResult }
    public GameState CurrentState { get; private set; } = GameState.WaitingToShoot;

    // ── Parámetros de disparo (leídos por Launcher y TrajectoryPredictor) ──────
    [Header("Parámetros de Disparo")]
    [Range(0f, 90f)]
    public float AngleDegrees = 45f;

    [Range(1f, 100f)]
    public float ForceMagnitude = 50f;

    /// <summary>Masa en kg del proyectil. Asignada por UIController.</summary>
    public float ProjectileMass = 1f;

    // ── Referencias a sistemas ─────────────────────────────────────────────────
    [Header("Referencias")]
    public Launcher Launcher;
    public ResultManager ResultManager;
    public UIController UIController;
    public TargetStructure[] TargetStructures;

    // ── Tiempo entre disparo y posibilidad de reiniciar ────────────────────────
    [Header("Configuración")]
    [Tooltip("Segundos que el proyectil puede estar en vuelo antes de forzar resultado.")]
    public float MaxFlightTime = 10f;

    private float _flightTimer;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Patrón Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Vigilar timeout del proyectil en vuelo
        if (CurrentState == GameState.ProjectileInFlight)
        {
            _flightTimer += Time.deltaTime;
            if (_flightTimer >= MaxFlightTime)
            {
                // El proyectil no impactó nada; forzar resultado con lo que hay
                OnProjectileMissed();
            }
        }
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Llamado por UIController al presionar "Disparar".</summary>
    public void RequestShot()
    {
        if (CurrentState != GameState.WaitingToShoot) return;

        CurrentState = GameState.ProjectileInFlight;
        _flightTimer = 0f;
        UIController.SetFireButtonInteractable(false);
        Launcher.Fire(AngleDegrees, ForceMagnitude, ProjectileMass);
    }

    /// <summary>Llamado por Projectile cuando colisiona con algo.</summary>
    public void OnProjectileImpact(ShotData data)
    {
        if (CurrentState != GameState.ProjectileInFlight) return;

        CurrentState = GameState.ShowingResult;
        ResultManager.RegisterResult(data);
        UIController.ShowResultPanel(ResultManager.GetLatestResult());
    }

    /// <summary>Llamado cuando el proyectil supera el tiempo máximo sin impactar.</summary>
    public void OnProjectileMissed()
    {
        if (CurrentState != GameState.ProjectileInFlight) return;

        // Construir datos de "fallo"
        ShotData missData = new ShotData
        {
            FlightTime = _flightTimer,
            ImpactPoint = Vector3.zero,
            RelativeVelocity = Vector3.zero,
            ImpulseForce = 0f,
            PiecesKnocked = CountTotalKnockedPieces(),
            WasMiss = true
        };

        CurrentState = GameState.ShowingResult;
        ResultManager.RegisterResult(missData);
        UIController.ShowResultPanel(ResultManager.GetLatestResult());
    }

    /// <summary>Llamado por UIController al presionar "Reiniciar".</summary>
    public void ResetScene()
    {
        CurrentState = GameState.WaitingToShoot;
        _flightTimer = 0f;

        // Reiniciar todas las estructuras de objetivos
        foreach (var structure in TargetStructures)
        {
            if (structure != null)
                structure.ResetStructure();
        }

        UIController.HideResultPanel();
        UIController.SetFireButtonInteractable(true);
    }

    // ── Auxiliares ─────────────────────────────────────────────────────────────

    private int CountTotalKnockedPieces()
    {
        int total = 0;
        foreach (var structure in TargetStructures)
        {
            if (structure != null)
                total += structure.GetKnockedCount();
        }
        return total;
    }
}
