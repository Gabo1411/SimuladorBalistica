using UnityEngine;

/// <summary>
/// Representa una pieza individual de la estructura de objetivos.
/// Detecta si fue derribada por velocidad angular o inclinación excesiva.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Target : MonoBehaviour
{
    // ── Configuración ──────────────────────────────────────────────────────────
    [Header("Umbrales de Derribo")]
    [Tooltip("Ángulo de inclinación (en grados) respecto al estado inicial para considerar 'derribada'.")]
    public float KnockAngleThreshold = 45f;

    [Tooltip("Velocidad angular mínima (rad/s) para considerar en movimiento.")]
    public float KnockAngularVelocityThreshold = 1.5f;

    [Tooltip("Segundos de gracia antes de empezar a verificar si fue derribada (evita falsos positivos al inicio).")]
    public float GracePeriod = 1f;

    // ── Estado ─────────────────────────────────────────────────────────────────
    private Rigidbody  _rb;
    private Quaternion _initialRotation;
    private Vector3    _initialPosition;
    private bool       _isKnocked;
    private float      _timeAlive;

    // Referencia a la estructura padre (asignada por TargetStructure)
    [HideInInspector] public TargetStructure ParentStructure;

    // ── Propiedades ────────────────────────────────────────────────────────────
    public bool IsKnocked => _isKnocked;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        RecordInitialState();
    }

    private void FixedUpdate()
    {
        if (_isKnocked) return;

        _timeAlive += Time.fixedDeltaTime;
        if (_timeAlive < GracePeriod) return;

        CheckIfKnocked();
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Reinicia la pieza a su estado original.</summary>
    public void ResetTarget()
    {
        _isKnocked = false;
        _timeAlive = 0f;

        // Reposicionar
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // Detener física
        _rb.linearVelocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Re-activar si estaba desactivado
        _rb.isKinematic = false;
    }

    /// <summary>Guarda el estado inicial (llamar después de configurar la escena).</summary>
    public void RecordInitialState()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _isKnocked       = false;
        _timeAlive       = 0f;
    }

    // ── Lógica de derribo ──────────────────────────────────────────────────────

    private void CheckIfKnocked()
    {
        // Criterio 1: Ángulo de inclinación respecto al estado original
        float angleDiff = Quaternion.Angle(transform.rotation, _initialRotation);

        // Criterio 2: Alta velocidad angular (la pieza está girando rápido)
        bool highAngularVelocity = _rb.angularVelocity.magnitude > KnockAngularVelocityThreshold;

        if (angleDiff >= KnockAngleThreshold || highAngularVelocity)
        {
            _isKnocked = true;
            Debug.Log($"[Target] '{gameObject.name}' DERRIBADA (ángulo: {angleDiff:F1}°, vel.ang: {_rb.angularVelocity.magnitude:F2})");

            // Notificar a la estructura padre
            ParentStructure?.OnPieceKnocked(this);
        }
    }
}
