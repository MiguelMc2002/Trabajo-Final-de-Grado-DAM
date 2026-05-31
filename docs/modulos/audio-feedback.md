# Módulo 12 — Audio y Feedback Visual

**Estado:** ❌ Pendiente

Este módulo no tiene implementación activa en la versión actual del proyecto.

---

## Funcionalidades planificadas

- **Efectos sonoros contextuales** — sonidos diferenciados para ciudad, combate, comercio y mapamundi.
- **Música ambiental** — pista diferente por escena.
- **Indicadores visuales de daño** — resaltado de secciones dañadas en combate (timón, velas, armamento, flotación).
- **Animaciones de impacto** — animación al recibir daño y al destruir una sección.
- **Marcadores en mapamundi** — iconos de estado sobre flotas (encuentro, peligro, ruta activa, en combate).

---

## Integración prevista

Los efectos sonoros se activarán suscribiéndose a los eventos ya existentes:

| Evento | Módulo origen | Acción de audio prevista |
|---|---|---|
| `SimulacionTiempo.OnNuevoDia` | Tiempo | Sonido de campanada diaria |
| `GestorCombatesActivos.OnCombateJugadorTerminado` | Combate | Fanfarria de victoria / derrota |
| `MarketManager.OnMercadoActualizado` | Económico | Sonido de transacción comercial |
| `CombateEventos.OnCombateIniciado` | Combate | Música de combate |
| `CombateEventos.OnCombateTerminado` | Combate | Vuelta a música del mapamundi |
