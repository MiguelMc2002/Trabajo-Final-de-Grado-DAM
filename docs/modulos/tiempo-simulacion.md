# Módulo 9 — Tiempo y Simulación

**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos

Gestiona el tiempo de juego con cuatro velocidades y pausa. Emite eventos periódicos que los demás módulos consumen para actualizar su estado (producción, navegación PNJ, combate por turnos).

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.SimulacionTiempo> | `MonoBehaviour` singleton | Avanza la fecha del juego y dispara los eventos de tiempo. Soporta 0.25x, 1x, 2x, 10x y pausa. |
| <xref:MareImperium.EstadoPartida> | Clase C# serializable | Contenedor del estado completo de una partida: mercados, flotas, barcos, edificios y memoria comercial. Gestionado exclusivamente por `GameManager`. |

---

## Eventos de `SimulacionTiempo`

| Evento | Tipo | Frecuencia | Consumidores principales |
|---|---|---|---|
| `OnNuevoDia` | `static event Action` | 1 vez/día | `FlotaManager.TickTodosLosControladores`, producción futura |
| `OnNuevoMes` | `static event Action` | 1 vez/mes | Estadísticas, renovación de stock |
| `OnNuevaHora` | `static event Action` | 24 veces/día | `GestorCombatesActivos` (avance de turno de combate) |
| `OnVelocidadCambiada` | `static event Action<float>` | Al cambiar velocidad | `HUDTiempo.ActualizarUI` |

---

## Velocidades soportadas

`0.25x` · `1x` · `2x` · `10x` · **Pausa**

Se cicla con `SubirVelocidad()` / `BajarVelocidad()`. La pausa se distingue de velocidad 0: `TogglePausa()` conserva la velocidad anterior.

---

## Fechas de inicio y tecnologías

El año de inicio de la partida determina qué tecnologías están disponibles. En particular, los módulos de armamento con `requierePolvora = true` en <xref:MareImperium.ModuloBarcoData> solo se desbloquean a partir del año 1380.
