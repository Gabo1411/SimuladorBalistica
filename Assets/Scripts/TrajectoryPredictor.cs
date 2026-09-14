using UnityEngine;

/// <summary>
/// Dibuja la trayectoria parabólica predicha del proyectil usando un LineRenderer.
/// Se actualiza en tiempo real cuando cambian los parámetros de disparo.
/// Usa cinemática (sin simular físicas reales) para un cálculo rápido.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    // ── Configuración ──────────────────────────────────────────────────────────
    [Header("Predicción")]
    [Tooltip("Número de puntos que forman la línea de trayectoria.")]
    [Range(10, 100)]
    public int LinePoints = 50;

    [Tooltip("Intervalo de tiempo (en segundos) entre cada punto de la trayectoria.")]
    public float TimeStep = 0.1f;

    [Tooltip("Si el proyectil simulado pasa por debajo de esta altura Y, la línea se corta.")]
    public float GroundY = 0f;

    // ── Referencias ────────────────────────────────────────────────────────────
    [Header("Referencias")]
    [Tooltip("Launcher del que se obtiene la posición inicial y dirección.")]
    public Launcher LauncherRef;

    // ── Componentes ────────────────────────────────────────────────────────────
    private LineRenderer _lineRenderer;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        ConfigureLineRenderer();
    }

    private void Start()
    {
        UpdateTrajectory();
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Recalcula y dibuja la trayectoria predicha con los parámetros actuales del GameManager.</summary>
    public void UpdateTrajectory()
    {
        if (GameManager.Instance == null || LauncherRef == null)
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        float   angle = GameManager.Instance.AngleDegrees;
        float   force = GameManager.Instance.ForceMagnitude;
        float   mass  = GameManager.Instance.ProjectileMass;
        Vector3 start = LauncherRef.GetFirePoint();
        Vector3 dir   = LauncherRef.GetLaunchDirection(angle);

        // Velocidad inicial: v = F/m (ForceMode.Impulse)
        Vector3 velocity = dir * (force / mass);

        DrawParabola(start, velocity);
    }

    /// <summary>Oculta la línea de trayectoria (cuando el proyectil está en vuelo).</summary>
    public void HideTrajectory()
    {
        _lineRenderer.positionCount = 0;
    }

    /// <summary>Muestra la línea de trayectoria.</summary>
    public void ShowTrajectory()
    {
        UpdateTrajectory();
    }

    // ── Dibujo de parábola ─────────────────────────────────────────────────────

    private void DrawParabola(Vector3 startPosition, Vector3 initialVelocity)
    {
        Vector3[] points = new Vector3[LinePoints];
        int validPoints = 0;

        Vector3 pos = startPosition;
        Vector3 vel = initialVelocity;

        for (int i = 0; i < LinePoints; i++)
        {
            points[i] = pos;
            validPoints++;

            // Integración de Euler simple: v += g·Δt, p += v·Δt
            vel += Physics.gravity * TimeStep;
            pos += vel * TimeStep;

            // Cortar la línea si toca el suelo
            if (pos.y <= GroundY)
            {
                // Interpolar para dar exactamente en GroundY
                points[i] = InterpolateGroundHit(points[i > 0 ? i - 1 : 0], pos);
                validPoints = i + 1;
                break;
            }
        }

        _lineRenderer.positionCount = validPoints;
        _lineRenderer.SetPositions(points);
    }

    /// <summary>Interpola el punto exacto donde la trayectoria cruza GroundY.</summary>
    private Vector3 InterpolateGroundHit(Vector3 above, Vector3 below)
    {
        if (Mathf.Approximately(above.y, below.y)) return above;
        float t = (GroundY - above.y) / (below.y - above.y);
        return Vector3.Lerp(above, below, t);
    }

    // ── Configuración visual del LineRenderer ──────────────────────────────────

    private void ConfigureLineRenderer()
    {
        _lineRenderer.startWidth   = 0.05f;
        _lineRenderer.endWidth     = 0.02f;
        _lineRenderer.useWorldSpace = true;

        // Material simple (se puede cambiar en el Inspector)
        if (_lineRenderer.material == null || _lineRenderer.material.name.Contains("Default"))
        {
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Línea amarilla semi-transparente
        Color startColor = new Color(1f, 0.9f, 0f, 0.8f);
        Color endColor   = new Color(1f, 0.5f, 0f, 0.2f);
        _lineRenderer.startColor = startColor;
        _lineRenderer.endColor   = endColor;

        // Línea punteada: 10 segmentos
        _lineRenderer.numCornerVertices = 3;
        _lineRenderer.numCapVertices    = 3;
    }
}
