using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona un grupo de piezas Target conectadas con Joints.
/// Construye la torre proceduralmente según BoxCount y BoxMass.
/// Permite reiniciar y reconstruir la estructura dinámicamente.
/// </summary>
public class TargetStructure : MonoBehaviour
{
    // ── Configuración de la torre ──────────────────────────────────────────────
    [Header("Configuración de la Torre")]
    [Tooltip("Cantidad de cajas por columna (altura).")]
    [Range(1, 15)]
    public int BoxCount = 4;

    [Tooltip("Cantidad de columnas (ancho).")]
    [Range(1, 6)]
    public int BoxColumns = 1;

    [Tooltip("Masa de cada caja en kg.")]
    [Range(0.5f, 30f)]
    public float BoxMass = 2f;

    [Tooltip("Tamaño de cada caja.")]
    public Vector3 BoxSize = Vector3.one;

    [Tooltip("Separación entre columnas.")]
    public float ColumnSpacing = 1.1f;

    [Tooltip("Fuerza necesaria para romper el FixedJoint entre cajas.")]
    public float JointBreakForce = 300f;

    // ── Estado ─────────────────────────────────────────────────────────────────
    public List<Target> Pieces { get; private set; } = new List<Target>();

    private int _knockedCount;

    /// <summary>Número de piezas actualmente derribadas.</summary>
    public int GetKnockedCount() => _knockedCount;

    /// <summary>Total de piezas en la estructura.</summary>
    public int TotalPieces => Pieces.Count;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildTower();
    }

    // ── Construcción procedural ────────────────────────────────────────────────

    /// <summary>
    /// Destruye las cajas existentes y reconstruye la torre con los
    /// valores actuales de BoxCount y BoxMass.
    /// </summary>
    public void BuildTower()
    {
        // Destruir piezas existentes
        foreach (var piece in Pieces)
            if (piece != null) Destroy(piece.gameObject);
        Pieces.Clear();
        _knockedCount = 0;

        // Centrar las columnas respecto al origen de la estructura
        float totalWidth = (BoxColumns - 1) * ColumnSpacing;

        for (int col = 0; col < BoxColumns; col++)
        {
            float xOffset = col * ColumnSpacing - totalWidth / 2f;
            Target previousInColumn = null;

            for (int row = 0; row < BoxCount; row++)
            {
                // Crear cubo
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Block_C{col + 1}_R{row + 1}";
                block.transform.SetParent(transform);
                block.transform.localScale    = BoxSize;
                block.transform.localRotation = Quaternion.identity;

                // Posición: centrado en X por columna, apilado en Y por fila
                float yPos = BoxSize.y * 0.5f + row * BoxSize.y;
                block.transform.localPosition = new Vector3(xOffset, yPos, 0f);

                // Rigidbody
                Rigidbody rb  = block.AddComponent<Rigidbody>();
                rb.mass        = BoxMass;
                rb.drag        = 0.5f;
                rb.angularDrag = 0.5f;

                // Script Target
                Target target          = block.AddComponent<Target>();
                target.ParentStructure = this;
                Pieces.Add(target);

                // FixedJoint — conectar con la caja de abajo en la misma columna
                FixedJoint joint  = block.AddComponent<FixedJoint>();
                joint.breakForce  = JointBreakForce;
                joint.breakTorque = JointBreakForce;

                if (previousInColumn != null)
                    joint.connectedBody = previousInColumn.GetComponent<Rigidbody>();
                // Si previousInColumn == null → base de columna anclada al mundo

                previousInColumn = target;
            }
        }

        // Registrar estado inicial de todas las piezas
        foreach (var piece in Pieces)
            piece.RecordInitialState();

        Debug.Log($"[TargetStructure] Torre: {BoxColumns} col × {BoxCount} filas × {BoxMass} kg");
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Llamado por Target cuando una pieza es derribada.</summary>
    public void OnPieceKnocked(Target piece)
    {
        _knockedCount++;
        Debug.Log($"[TargetStructure] {_knockedCount}/{Pieces.Count} piezas derribadas.");
    }

    /// <summary>
    /// Reinicia la estructura reconstruyéndola desde cero
    /// con los parámetros actuales (BoxCount y BoxMass).
    /// </summary>
    public void ResetStructure()
    {
        BuildTower();
        Debug.Log($"[TargetStructure] Estructura reiniciada.");
    }

    /// <summary>Cambia la masa de todas las cajas existentes sin reconstruir.</summary>
    public void ApplyMassToAllPieces()
    {
        foreach (var piece in Pieces)
        {
            if (piece == null) continue;
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null) rb.mass = BoxMass;
        }
    }
}
