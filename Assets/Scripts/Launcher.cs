using UnityEngine;

/// <summary>
/// Controla el cañón: instancia el proyectil y aplica la fuerza de lanzamiento
/// según el ángulo, magnitud y masa configurados.
/// </summary>
public class Launcher : MonoBehaviour
{
    // ── Referencias ────────────────────────────────────────────────────────────
    [Header("Referencias")]
    [Tooltip("Transform hijo que indica la posición y dirección de salida del proyectil.")]
    public Transform FirePoint;

    [Tooltip("Prefab del proyectil (debe tener Rigidbody, Collider y Projectile.cs).")]
    public GameObject ProjectilePrefab;

    // ── Configuración visual del cañón ────────────────────────────────────────
    [Header("Rotación Visual del Cañón")]
    [Tooltip("Si es true, el cañón (o un hijo) se rota para coincidir con el ángulo de disparo.")]
    public bool RotateBarrel = true;

    [Tooltip("Transform del 'cañón' que se gira visualmente (puede ser este mismo GameObject).")]
    public Transform BarrelTransform;

    // ── Estado ─────────────────────────────────────────────────────────────────
    private GameObject _currentProjectile;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (FirePoint == null)
        {
            Debug.LogError("[Launcher] No se asignó FirePoint. Crea un hijo vacío llamado 'FirePoint'.");
        }
        if (ProjectilePrefab == null)
        {
            Debug.LogError("[Launcher] No se asignó el prefab del proyectil.");
        }
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>
    /// Lanza el proyectil con los parámetros dados.
    /// </summary>
    /// <param name="angleDegrees">Ángulo de elevación en grados (0–90).</param>
    /// <param name="forceMagnitude">Magnitud de la fuerza de impulso.</param>
    /// <param name="mass">Masa del proyectil en kg.</param>
    public void Fire(float angleDegrees, float forceMagnitude, float mass)
    {
        if (ProjectilePrefab == null || FirePoint == null)
        {
            Debug.LogError("[Launcher] Faltan referencias. No se puede disparar.");
            return;
        }

        // Destruir proyectil anterior si existe
        if (_currentProjectile != null)
            Destroy(_currentProjectile);

        // Calcular dirección de lanzamiento
        Vector3 direction = CalculateLaunchDirection(angleDegrees);

        // Rotar el cañón visualmente
        if (RotateBarrel && BarrelTransform != null)
        {
            float angle = -angleDegrees; // negativo porque apunta "hacia arriba" en local
            BarrelTransform.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        // Instanciar el proyectil en el FirePoint
        _currentProjectile = Instantiate(ProjectilePrefab, FirePoint.position, FirePoint.rotation);

        // Configurar masa ANTES de aplicar la fuerza
        Projectile projectileScript = _currentProjectile.GetComponent<Projectile>();
        if (projectileScript != null)
            projectileScript.InitialMass = mass;

        Rigidbody rb = _currentProjectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[Launcher] El prefab del proyectil no tiene Rigidbody.");
            return;
        }

        // Asignar masa al Rigidbody directamente también
        rb.mass = mass;

        // Aplicar fuerza de impulso
        // Usamos ForceMode.Impulse para una fuerza instantánea (F = m·a → v = F/m)
        rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);

        Debug.Log($"[Launcher] Disparado: ángulo={angleDegrees}°, fuerza={forceMagnitude}, masa={mass}kg, dirección={direction}");
    }

    /// <summary>
    /// Precalcula la dirección de lanzamiento para los controles de UI / predictor de trayectoria.
    /// </summary>
    public Vector3 GetLaunchDirection(float angleDegrees) => CalculateLaunchDirection(angleDegrees);

    /// <summary>
    /// Posición del punto de disparo (usada por TrajectoryPredictor).
    /// </summary>
    public Vector3 GetFirePoint() => FirePoint != null ? FirePoint.position : transform.position;

    /// <summary>Rota el cañón visualmente en tiempo real según el ángulo dado.</summary>
    public void UpdateBarrelRotation(float angleDegrees)
    {
        if (RotateBarrel && BarrelTransform != null)
        {
            // Negativo → el cañón se inclina hacia ARRIBA al aumentar el ángulo
            BarrelTransform.localRotation = Quaternion.Euler(-angleDegrees, 0f, 0f);
        }
    }

    /// <summary>
    /// Convierte el ángulo de elevación en un vector dirección.
    /// El lanzador apunta en su eje forward (+Z). El ángulo eleva sobre el plano XZ.
    /// </summary>
    private Vector3 CalculateLaunchDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;

        // Descomposición: componente horizontal (forward del lanzador) y vertical (up)
        Vector3 forward  = transform.forward;
        Vector3 up       = Vector3.up;

        // Proyectar forward en el plano horizontal para evitar inclinación indeseada
        forward.y = 0f;
        forward.Normalize();

        // Dirección final: cos(θ)·forward + sin(θ)·up
        Vector3 direction = Mathf.Cos(rad) * forward + Mathf.Sin(rad) * up;
        return direction.normalized;
    }
}
