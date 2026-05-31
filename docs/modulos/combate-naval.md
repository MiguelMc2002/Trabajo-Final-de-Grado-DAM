# Módulo 3 — Combate Naval

**Estado:** ⚠️ Parcial — resolución asíncrona automática implementada; tablero grid manual pendiente  
**Dependencias:** Módulo de Flotas, Módulo de Mundo y Navegación, Módulo de PNJs

El sistema de combate soporta dos modos: resolución instantánea (PNJ vs PNJ en segundo plano) y resolución asíncrona por turnos con interfaz (jugador vs PNJ). Un turno equivale a una hora de juego. Timeout de 5 días (120 turnos) = el defensor evade al atacante.

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.CombateNavalResolver> | Clase estática | Resolución instantánea sin animación. Usada para combates en segundo plano entre flotas PNJ. |
| <xref:MareImperium.CombateEnCurso> | Clase C# | Combate asíncrono por turnos. Gestiona los stats de ambas flotas y avanza un turno por llamada a `ResolverTurno()`. |
| <xref:MareImperium.GestorCombatesActivos> | `MonoBehaviour` singleton | Registro global de combates activos. Se suscribe a `SimulacionTiempo.OnNuevaHora` para avanzar turnos. |
| <xref:MareImperium.CombateEventos> | Clase estática | Bus de eventos de combate: `OnCombateIniciado` y `OnCombateTerminado`. Controla el flag `CombateJugadorEnCurso`. |
| <xref:MareImperium.ResultadoCombate> | Clase C# inmutable | Snapshot del resultado: ganador, bajas, daño, botín en oro y mercancía, texto narrativo. |
| <xref:MareImperium.ResultadoCombateUI> | `MonoBehaviour` | Panel post-combate. Acciones: Destruir / Saquear (40 % de carga + oro) / Capturar. Pausa `Time.timeScale`. |
| <xref:MareImperium.EncuentroNavalUI> | `MonoBehaviour` | Panel de decisión al interceptar una flota: Luchar / Huir. Reactivo a `CombateEventos.OnCombateIniciado`. |

---

## Constantes de combate

| Constante | Valor | Descripción |
|---|---|---|
| `MaxTurnos` | 120 | Timeout (5 días); si se alcanza el defensor escapa. |
| `FuerzaBaseXBarco` | 5.0 | Fuerza base de combate por barco. |
| `MultiplicadorDanio` | 0.15 | Factor de daño por turno con varianza ±30 %. |
| `ProbabilidadCapturaBase` | 0.30 | Probabilidad base de capturar un barco enemigo. |
| `ProbabilidadCapturaMax` | 0.50 | Probabilidad máxima (tripulación al límite). |

---

## Pendiente

- Tablero grid manual: movimiento por casillas, selección de sección a atacar (timón / velas / armamento / flotación).
- Fase de abordaje: combate de tripulación en cubierta.
