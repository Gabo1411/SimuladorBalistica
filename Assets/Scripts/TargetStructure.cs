using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona un grupo de piezas Target conectadas con Joints.
/// Registra cuántas cayeron y permite reiniciar la estructura completa.
/// </summary>
public class TargetStructure : MonoBehaviour
{
    // ── Configuración ──────────────────────────────────────────────────────────
    [Header("Configuración")]
    [Tooltip("Si es true, busca automáticamente todos los Target en hijos al iniciar.")]
    public bool AutoFindTargets = true;

    [Tooltip("Lista manual de piezas (si AutoFindTargets = false).")]
    public List<Target> Pieces = new List<Target>();

    // ── Estado ─────────────────────────────────────────────────────────────────
    private int _knockedCount;
    private List<JointData> _jointDataList = new List<JointData>();

    /// <summary>Número de piezas actualmente derribadas.</summary>
    public int GetKnockedCount() => _knockedCount;

    /// <summary>Total de piezas en la estructura.</summary>
    public int TotalPieces => Pieces.Count;

    // ── Estructura auxiliar para guardar estado de Joints ──────────────────────
    private class JointData
    {
        public Joint   JointComponent;
        public Vector3 Anchor;
    }

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (AutoFindTargets)
        {
            Pieces.Clear();
            // Buscar todos los Target en hijos (no incluye este mismo GameObject)
            Target[] found = GetComponentsInChildren<Target>(includeInactive: true);
            foreach (var t in found)
            {
                t.ParentStructure = this;
                Pieces.Add(t);
            }
        }
        else
        {
            // Asignar referencia al padre manualmente
            foreach (var t in Pieces)
                if (t != null) t.ParentStructure = this;
        }

        // Guardar estado inicial de los Joints
        SaveJointStates();

        Debug.Log($"[TargetStructure] '{gameObject.name}' inicializada con {Pieces.Count} piezas.");
    }

    private void Start()
    {
        // Registrar estado inicial de todas las piezas
        foreach (var piece in Pieces)
            piece?.RecordInitialState();

        _knockedCount = 0;
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Llamado por Target cuando una pieza es derribada.</summary>
    public void OnPieceKnocked(Target piece)
    {
        _knockedCount++;
        Debug.Log($"[TargetStructure] '{gameObject.name}': {_knockedCount}/{Pieces.Count} piezas derribadas.");
    }

    /// <summary>Reinicia todas las piezas y Joints a su estado original.</summary>
    public void ResetStructure()
    {
        _knockedCount = 0;

        // Reiniciar físicamente cada pieza
        foreach (var piece in Pieces)
            piece?.ResetTarget();

        // Restaurar Joints (re-conectar los que se rompieron)
        RestoreJoints();

        Debug.Log($"[TargetStructure] '{gameObject.name}' reiniciada.");
    }

    // ── Joints ─────────────────────────────────────────────────────────────────

    private void SaveJointStates()
    {
        _jointDataList.Clear();
        Joint[] joints = GetComponentsInChildren<Joint>(includeInactive: true);
        foreach (var joint in joints)
        {
            _jointDataList.Add(new JointData
            {
                JointComponent = joint,
                Anchor         = joint.anchor
            });
        }
    }

    private void RestoreJoints()
    {
        // Joint no hereda de Behaviour, no tiene 'enabled'.
        // Reactivar el GameObject del joint si fue desactivado durante el juego.
        foreach (var data in _jointDataList)
        {
            if (data.JointComponent != null && !data.JointComponent.gameObject.activeSelf)
            {
                data.JointComponent.gameObject.SetActive(true);
            }
        }
    }
}
