using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Datos de un disparo. Rellenados por Projectile y TargetStructure.
/// </summary>
[System.Serializable]
public class ShotData
{
    public float   FlightTime;       // Segundos desde disparo hasta impacto
    public Vector3 ImpactPoint;      // Coordenada mundial del impacto
    public Vector3 RelativeVelocity; // Velocidad relativa en el momento del impacto
    public float   ImpulseForce;     // Magnitud del impulso (N·s)
    public int     PiecesKnocked;    // Piezas derribadas
    public bool    WasMiss;          // ¿El proyectil no impactó ningún objetivo?
}

/// <summary>
/// Resultado calculado de un disparo: datos crudos + puntaje + reporte textual.
/// </summary>
[System.Serializable]
public class ShotResult
{
    public ShotData Data;
    public float    Score;
    public string   Report;
}

/// <summary>
/// Registra resultados de cada disparo, calcula puntaje y genera el reporte de tiro.
/// </summary>
public class ResultManager : MonoBehaviour
{
    // ── Historial ──────────────────────────────────────────────────────────────
    public List<ShotResult> History { get; private set; } = new List<ShotResult>();

    // ── Pesos de la fórmula de puntaje ─────────────────────────────────────────
    [Header("Pesos del Puntaje")]
    [Tooltip("Puntos por cada pieza derribada.")]
    public float PointsPerPiece = 100f;

    [Tooltip("Multiplicador de velocidad relativa de impacto.")]
    public float VelocityMultiplier = 2f;

    [Tooltip("Multiplicador de impulso de colisión.")]
    public float ImpulseMultiplier = 1.5f;

    // Centro de referencia de los objetivos (asignar desde Inspector)
    [Header("Referencia Espacial")]
    [Tooltip("Transform del centro del grupo de objetivos (para calcular precisión).")]
    public Transform TargetCenter;

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Registra el resultado de un disparo y devuelve el ShotResult calculado.</summary>
    public ShotResult RegisterResult(ShotData data)
    {
        float score = CalculateScore(data);
        string report = GenerateReport(data, score);

        ShotResult result = new ShotResult
        {
            Data   = data,
            Score  = score,
            Report = report
        };

        History.Add(result);
        Debug.Log($"[ResultManager] Disparo #{History.Count} registrado. Score: {score:F1}");
        return result;
    }

    /// <summary>Devuelve el resultado del último disparo.</summary>
    public ShotResult GetLatestResult()
    {
        if (History.Count == 0) return null;
        return History[History.Count - 1];
    }

    /// <summary>Devuelve el total de disparos realizados.</summary>
    public int TotalShots => History.Count;

    /// <summary>Devuelve la puntuación acumulada de todos los disparos.</summary>
    public float TotalScore()
    {
        float total = 0f;
        foreach (var r in History) total += r.Score;
        return total;
    }

    // ── Cálculo de puntaje ─────────────────────────────────────────────────────

    /// <summary>
    /// Fórmula:
    /// Score = (PiecesKnocked × PointsPerPiece)
    ///       + (100 / (distancia_al_centro + 1))    ← precisión
    ///       + (|RelativeVelocity| × VelocityMultiplier)
    ///       + (ImpulseForce × ImpulseMultiplier)
    /// </summary>
    private float CalculateScore(ShotData data)
    {
        if (data.WasMiss)
            return Mathf.Max(0f, data.PiecesKnocked * PointsPerPiece * 0.5f); // Penalización por miss

        float pieceScore    = data.PiecesKnocked * PointsPerPiece;
        float accuracyScore = 0f;
        if (TargetCenter != null)
        {
            float dist = Vector3.Distance(data.ImpactPoint, TargetCenter.position);
            accuracyScore = 100f / (dist + 1f);
        }
        float velocityScore = data.RelativeVelocity.magnitude * VelocityMultiplier;
        float impulseScore  = data.ImpulseForce * ImpulseMultiplier;

        return Mathf.Max(0f, pieceScore + accuracyScore + velocityScore + impulseScore);
    }

    // ── Generación de Reporte ──────────────────────────────────────────────────

    private string GenerateReport(ShotData data, float score)
    {
        if (data.WasMiss)
        {
            return $"⚠ DISPARO FALLADO\n" +
                   $"El proyectil no impactó ningún objetivo.\n" +
                   $"Tiempo de vuelo: {data.FlightTime:F2} s\n" +
                   $"Piezas derribadas: {data.PiecesKnocked}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"PUNTAJE: {score:F0} pts";
        }

        string precision = GetPrecisionRating(data);
        string power     = GetPowerRating(data);

        return $"✦ REPORTE DE TIRO #{GameManager.Instance?.ResultManager?.TotalShots ?? 1}\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Tiempo de vuelo:   {data.FlightTime:F2} s\n" +
               $"Punto de impacto:  ({data.ImpactPoint.x:F1}, {data.ImpactPoint.y:F1}, {data.ImpactPoint.z:F1})\n" +
               $"Vel. relativa:     {data.RelativeVelocity.magnitude:F2} m/s\n" +
               $"Impulso:           {data.ImpulseForce:F2} N·s\n" +
               $"Piezas derribadas: {data.PiecesKnocked}\n" +
               $"Precisión:         {precision}\n" +
               $"Potencia:          {power}\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"PUNTAJE: {score:F0} pts";
    }

    private string GetPrecisionRating(ShotData data)
    {
        if (TargetCenter == null) return "N/A";
        float dist = Vector3.Distance(data.ImpactPoint, TargetCenter.position);
        if (dist < 1f)  return "⭐⭐⭐ EXACTO";
        if (dist < 3f)  return "⭐⭐ BUENO";
        if (dist < 6f)  return "⭐ ACEPTABLE";
        return "✗ LEJOS";
    }

    private string GetPowerRating(ShotData data)
    {
        float speed = data.RelativeVelocity.magnitude;
        if (speed > 30f) return "⭐⭐⭐ DEVASTADOR";
        if (speed > 15f) return "⭐⭐ FUERTE";
        if (speed > 5f)  return "⭐ MODERADO";
        return "✗ DÉBIL";
    }
}
