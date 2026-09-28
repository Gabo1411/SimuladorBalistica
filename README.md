# Simulador Balístico
**Unity 2022.3.62f1 LTS**

Simulador físico de balística donde el jugador ajusta ángulo, fuerza y masa del proyectil para derribar estructuras conectadas con Joints. El sistema de físicas de Unity gobierna cada disparo, colisión y derribo. Al finalizar cada intento se genera un reporte de tiro con datos reales de la simulación, los cuales **se persisten en la nube utilizando Unity Game Services (UGS)**.

---

## Requisitos

| | |
|---|---|
| **Motor** | Unity 2022.3.62f1 LTS |
| **Render Pipeline** | Built-in |
| **Paquetes adicionales** | TextMeshPro, UGS Authentication, UGS Cloud Save |
| **Plataforma de prueba** | Windows (Editor) |

---

## Cómo abrir el proyecto

1. Clonar o descomprimir el repositorio.
2. Abrir **Unity Hub** → **Add project from disk** → seleccionar la carpeta `SimuladorBalistica`.
3. Esperar que Unity importe los assets.
4. Abrir la escena `Assets/Scenes/SampleScene.unity`.
5. Presionar **Play**. *(El juego se conectará automáticamente a los servicios de Unity de forma anónima).*

---

## Controles

| Control | Acción |
|---|---|
| **Slider Ángulo** | Ajusta el ángulo de elevación del cañón (0° – 90°) |
| **Slider Fuerza** | Ajusta la potencia del disparo (1 – 100) |
| **Dropdown Masa** | Selecciona la masa del proyectil (Ligero 0.5 kg / Mediano 1 kg / Pesado 3 kg) |
| **Botón DISPARAR** | Lanza el proyectil con los parámetros configurados |
| **Botón REINICIAR** | Reconstruye la torre y habilita un nuevo disparo |
| **Botón VER HISTORIAL** | Abre un panel con el registro en la nube de todos los disparos anteriores |

### Vista previa de trayectoria
Antes de disparar, una **línea amarilla** muestra la parábola predicha en tiempo real. Se actualiza automáticamente al mover cualquier slider y desaparece al disparar.

---

## Persistencia de Datos en la Nube (UGS)

El proyecto integra **Unity Game Services** para guardar de forma persistente los resultados de cada sesión de juego.

1. **Autenticación Anónima:** Al iniciar la escena, `UGSManager` realiza un sign-in anónimo del jugador.
2. **Cloud Save:** Al concluir el vuelo del proyectil, se empaquetan los datos de configuración (Ángulo, Fuerza, Masa) y los resultados (Acierto/Fallo, Distancia, Piezas Derribadas) en un objeto `ShotRecord`.
3. **Serialización Nativa:** El registro se añade al historial local y se guarda asincrónicamente en los servidores de Unity.
4. **Visualización:** El panel de Historial recupera los datos de Cloud Save y los muestra en un ScrollView, persistiendo los datos incluso si se cierra y vuelve a abrir el juego.

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
    ├── UIController.cs          ← interfaz de usuario principal
    │
    ├── UGSManager.cs            ← [NUEVO] Inicialización UGS y guardado/carga
    ├── ShotRecord.cs            ← [NUEVO] Modelo de datos serializable para la nube
    └── HistoryPanel.cs          ← [NUEVO] Lógica del panel de historial de disparos
```

---

## Criterios de Evaluación Cubiertos

### Físicas y Gameplay (Base)
| Criterio | Implementación |
|---|---|
| ✅ Controles de disparo en pantalla | Sliders de ángulo y fuerza + dropdown de masa |
| ✅ Proyectil con Rigidbody y Collider | `Projectile.cs` — `RequireComponent(Rigidbody, SphereCollider)` |
| ✅ Lanzamiento por AddForce según ángulo | `Launcher.cs` — `rb.AddForce(dir * force, ForceMode.Impulse)` |
| ✅ Estructuras con Rigidbodies y Joints | `TargetStructure.cs` — FixedJoint encadenado entre bloques |
| ✅ Puntuación y variables al final | Múltiples variables registradas + Reporte de tiro |

### Persistencia de Datos (Parcial 1)
| Criterio | Implementación |
|---|---|
| ✅ Guardar en UGS al finalizar el disparo | `GameManager.cs` llama a `UGSManager.Instance.SaveShotAsync()` |
| ✅ Guardar Ángulo, Fuerza y Masa | Incluidos en la clase `ShotRecord` |
| ✅ Guardar Resultado de impacto y Distancia | `WasHit` y `DistanceToCenter` en `ShotRecord` |
| ✅ Guardar Cantidad de objetos afectados | `PiecesKnocked` en `ShotRecord` |
| ✅ Botón en menú para lista de resultados | Botón UI "Ver Historial" que abre `HistoryPanel` |

---
Link del video de YouTube: https://youtu.be/y9z2QkP3bII
---
## Autor

Gabriel Giménez Surdel

Proyecto desarrollado para la materia de **Desarrollo de Videojuegos**.
Unity 2022.3.62f1 LTS — 2026.
