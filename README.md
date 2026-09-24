# Simulador Balístico
**Unity 2022.3.62f1 LTS**

Simulador físico de balística donde el jugador ajusta ángulo, fuerza y masa del proyectil para derribar estructuras conectadas con Joints. El sistema de físicas de Unity gobierna cada disparo, colisión y derribo. Al finalizar cada intento se genera un reporte de tiro con datos reales de la simulación.

---

## Requisitos

| | |
|---|---|
| **Motor** | Unity 2022.3.62f1 LTS |
| **Render Pipeline** | Built-in |
| **Paquetes adicionales** | TextMeshPro 3.0.7 |
| **Plataforma de prueba** | Windows (Editor) |

---

## Cómo abrir el proyecto

1. Clonar o descomprimir el repositorio.
2. Abrir **Unity Hub** → **Add project from disk** → seleccionar la carpeta `SimuladorBalistica`.
3. Esperar que Unity importe los assets.
4. Abrir la escena `Assets/Scenes/SampleScene.unity`.
5. Presionar **Play**.

---

## Controles

| Control | Acción |
|---|---|
| **Slider Ángulo** | Ajusta el ángulo de elevación del cañón (0° – 90°) |
| **Slider Fuerza** | Ajusta la potencia del disparo (1 – 100) |
| **Dropdown Masa** | Selecciona la masa del proyectil (Ligero 0.5 kg / Mediano 1 kg / Pesado 3 kg) |
| **Botón DISPARAR** | Lanza el proyectil con los parámetros configurados |
| **Botón REINICIAR** | Reconstruye la torre y habilita un nuevo disparo |

### Vista previa de trayectoria
Antes de disparar, una **línea amarilla** muestra la parábola predicha en tiempo real. Se actualiza automáticamente al mover cualquier slider y desaparece al disparar.

---

## Cómo jugar

1. **Ajustá el ángulo** con el slider superior (45° es un buen punto de partida).
2. **Ajustá la fuerza** — más fuerza = más distancia y velocidad.
3. **Elegí la masa** del proyectil. Un proyectil más pesado transfiere más impulso pero necesita más fuerza para llegar lejos.
4. Observá la **línea de trayectoria** y apuntá a la torre.
5. Presioná **DISPARAR**.
6. Esperá el **Reporte de Tiro** (aparece ~1.5 segundos después del impacto para capturar el efecto dominó).
7. Presioná **REINICIAR** para volver a intentar.

---

## Sistema de Físicas

### Proyectil
- `Rigidbody` con masa configurable por el jugador.
- Lanzamiento mediante `AddForce` con `ForceMode.Impulse`.
- Detección de colisiones en modo **ContinuousDynamic** para evitar tunneling a alta velocidad.

### Estructura de Objetivos
- Torre de cajas generada proceduralmente con `Rigidbody` + `FixedJoint`.
- La base de cada columna está anclada al mundo (`connectedBody = null`).
- Los joints tienen `Break Force = 300 N` — se rompen con impactos suficientemente fuertes.
- Detección de colisiones en modo **Continuous** para recibir impactos correctamente.

---

## Reporte de Tiro

Al finalizar cada disparo se registran y muestran los siguientes datos:

| Dato | Descripción |
|---|---|
| **Tiempo de vuelo** | Segundos desde el disparo hasta el impacto |
| **Punto de impacto** | Coordenada mundial (X, Y, Z) del contacto |
| **Velocidad relativa** | Magnitud de la velocidad relativa en el momento del impacto (m/s) |
| **Impulso de colisión** | Fuerza de impulso transferida en el impacto (N·s) |
| **Piezas derribadas** | Cantidad de bloques que superaron el umbral de derribo |
| **Precisión** | Rating según distancia al centro del objetivo |
| **Potencia** | Rating según velocidad relativa de impacto |

### Fórmula de Puntaje

```
Score = (Piezas × 100)
      + (100 / (distancia_al_centro + 1))
      + (velocidad_relativa × 2)
      + (impulso × 1.5)
```

---

## Estructura del Proyecto

```
Assets/
├── Scenes/
│   └── SampleScene.unity        ← escena principal
└── Scripts/
    ├── GameManager.cs           ← singleton coordinador, estados del juego
    ├── ResultManager.cs         ← registro, puntaje y reporte de tiro
    ├── Projectile.cs            ← comportamiento del proyectil
    ├── Launcher.cs              ← lanzamiento y rotación del cañón
    ├── Target.cs                ← detección de derribo por pieza
    ├── TargetStructure.cs       ← construcción procedural de la torre
    ├── TrajectoryPredictor.cs   ← trayectoria parabólica predicha
    ├── UIController.cs          ← interfaz de usuario
    └── CollapsiblePanel.cs      ← paneles desplegables de UI
```

---

## Criterios de Evaluación Cubiertos

| Criterio | Implementación |
|---|---|
| ✅ Controles de disparo en pantalla | Sliders de ángulo y fuerza + dropdown de masa |
| ✅ Proyectil con Rigidbody y Collider | `Projectile.cs` — `RequireComponent(Rigidbody, SphereCollider)` |
| ✅ Lanzamiento por AddForce según ángulo | `Launcher.cs` — `rb.AddForce(dir * force, ForceMode.Impulse)` |
| ✅ Estructuras con Rigidbodies y Joints | `TargetStructure.cs` — FixedJoint encadenado entre bloques |
| ✅ Estabilidad inicial de la estructura | Torre no cae sola — base anclada al mundo |
| ✅ Tiempo de vuelo registrado | `ShotData.FlightTime` |
| ✅ Punto de impacto registrado | `ShotData.ImpactPoint` (Vector3) |
| ✅ Velocidad relativa registrada | `ShotData.RelativeVelocity` |
| ✅ Impulso de colisión registrado | `ShotData.ImpulseForce` (`collision.impulse.magnitude`) |
| ✅ Piezas derribadas registradas | `ShotData.PiecesKnocked` |
| ✅ Puntuación al final de cada intento | Fórmula ponderada en `ResultManager.CalculateScore()` |
| ✅ Reporte de tiro al final de cada intento | Panel animado con todos los datos y ratings |

---

## Autor

Proyecto desarrollado para la materia de **Desarrollo de Videojuegos**.
Unity 2022.3.62f1 LTS — 2026.
