using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro serializable de un disparo individual para guardar en UGS Cloud Save.
/// </summary>
[Serializable]
public class ShotRecord
{
    public string Id;               // GUID único del disparo
    public string Timestamp;        // Fecha y hora UTC (yyyy-MM-dd HH:mm:ss)
    public float  Angle;            // Ángulo de disparo (grados)
    public float  Force;            // Fuerza aplicada
    public float  Mass;             // Masa del proyectil (kg)
    public bool   WasHit;           // ¿Impactó algún objetivo?
    public float  DistanceToCenter; // Distancia al centro del objetivo (m)
    public int    PiecesKnocked;    // Piezas derribadas
    public float  Score;            // Puntaje obtenido
}

/// <summary>
/// Colección de disparos — se serializa completa como JSON en Cloud Save.
/// </summary>
[Serializable]
public class ShotHistory
{
    public List<ShotRecord> Records = new List<ShotRecord>();
}
