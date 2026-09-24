using UnityEngine;

/// <summary>
/// Componente del proyectil. Gestiona su ciclo de vida físico y registra datos
/// de impacto al colisionar con cualquier objeto.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Projectile : MonoBehaviour
{
    // ── Configuración ──────────────────────────────────────────────────────────
    [Header("Configuración")]
    [Tooltip("Segundos antes de auto-destruirse si no impacta nada.")]
    public float LifeTime = 12f;

    [Tooltip("El proyectil se destruye tras el primer impacto con un objetivo.")]
    public bool DestroyOnFirstImpact = true;

    [Tooltip("Capas que cuentan como 'objetivo' válido (para registrar impacto).")]
    public LayerMask TargetLayers = ~0; // Por defecto: todas las capas

    // ── Estado interno ─────────────────────────────────────────────────────────
    private Rigidbody _rb;
    private float     _launchTime;
    private bool      _hasImpacted;

    // ── Propiedades públicas ───────────────────────────────────────────────────
    /// <summary>Masa configurada externamente antes de Awake (por Launcher).</summary>
    public float InitialMass { get; set; } = 1f;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        // Aplicar masa configurada por el Launcher
        _rb.mass = InitialMass;

        // ContinuousDynamic evita el tunneling cuando el proyectil va muy rápido
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Registrar momento de lanzamiento
        _launchTime  = Time.time;
        _hasImpacted = false;

        Destroy(gameObject, LifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Evitar registrar múltiples impactos
        if (_hasImpacted && DestroyOnFirstImpact) return;

        // Ignorar colisiones con el propio cañón / lanzador (en el frame de disparo)
        if (Time.time - _launchTime < 0.1f) return;

        _hasImpacted = true;

        // ── Recopilar datos del impacto ──────────────────────────────────────
        float   flightTime       = Time.time - _launchTime;
        Vector3 impactPoint      = collision.GetContact(0).point;
        Vector3 relativeVelocity = collision.relativeVelocity;

        // Impulso = suma de todos los ContactPoints
        float impulse = 0f;
        foreach (ContactPoint cp in collision.contacts)
        {
            // La magnitud del impulso se aproxima con impulseForce de Unity 2019.3+
        }
        // Unity expone el impulso directamente en la colisión
        impulse = collision.impulse.magnitude;

        // Contar piezas derribadas hasta ahora
        int piecesKnocked = CountKnockedPieces();

        ShotData data = new ShotData
        {
            FlightTime       = flightTime,
            ImpactPoint      = impactPoint,
            RelativeVelocity = relativeVelocity,
            ImpulseForce     = impulse,
            PiecesKnocked    = piecesKnocked,
            WasMiss          = false
        };

        Debug.Log($"[Projectile] Impacto en {impactPoint}. Vel. relativa: {relativeVelocity.magnitude:F2} m/s. Impulso: {impulse:F2} N·s");

        // Notificar al GameManager (esperar un frame para que Unity resuelva las físicas)
        StartCoroutine(ReportImpactNextFrame(data));

        if (DestroyOnFirstImpact)
            Destroy(gameObject, 2.5f); // Debe ser mayor que el WaitForSeconds de la corrutina (1.5f)
    }

    // ── Auxiliares ─────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator ReportImpactNextFrame(ShotData data)
    {
        // Esperar que las físicas se asienten para contar bien las piezas caídas
        yield return new WaitForSeconds(1.5f);

        // Actualizar conteo de piezas (puede haber más caídas con el efecto dominó)
        data.PiecesKnocked = CountKnockedPieces();

        if (GameManager.Instance != null)
            GameManager.Instance.OnProjectileImpact(data);
    }

    private int CountKnockedPieces()
    {
        if (GameManager.Instance == null) return 0;

        int total = 0;
        foreach (var structure in GameManager.Instance.TargetStructures)
        {
            if (structure != null)
                total += structure.GetKnockedCount();
        }
        return total;
    }
}
