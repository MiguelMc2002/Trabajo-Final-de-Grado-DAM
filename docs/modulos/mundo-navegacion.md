# Módulo 6 — Mundo y Navegación

**Estado:** ✅ Implementado  
**Dependencias:** Módulo de PNJs, Módulo de Combate, Módulo de Flotas

Mapamundi hexagonal con niebla de guerra, cálculo de rutas A* y detección de encuentros entre flotas. La cámara soporta zoom, scroll por bordes, arrastre y teclado WASD.

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.MapamundiController> | `MonoBehaviour` singleton | Controlador central del mapamundi. Gestiona iconos de flotas, detección de combate, navegación del jugador y estado del mapa. |
| <xref:MareImperium.NavegacionJugadorController> | `MonoBehaviour` singleton | Gestiona la navegación del jugador: click derecho → ruta A*, llegada a ciudad → `PopUpEntradaCiudad`, persecución en modo pirata. |
| <xref:MareImperium.FlotaIconoMapamundi> | `MonoBehaviour` | Icono de una flota en el mapa. Se mueve siguiendo una lista de waypoints, colorea en naranja durante combate y puede huir al puerto más cercano. |
| <xref:MareImperium.RutaCalculadorTilemap> | `MonoBehaviour` | Algoritmo A* sobre el Tilemap hexagonal. Variante con ruido para rutas más naturales. |
| <xref:HansaTrader.Mapamundi.TileNavegable> | `ScriptableObject` | Tile del mapa: coste de movimiento A* e indicador de si es transitable. |
| <xref:MareImperium.MapamundiCamara> | `MonoBehaviour` | Controla la cámara del mapamundi: zoom, scroll por bordes, WASD y arrastre. Click derecho delega en `NavegacionJugadorController`. |
| <xref:MareImperium.MarcadorCiudad> | `MonoBehaviour` | Sprite interactuable de una ciudad en el tilemap. Escala al 120 % en hover. Click delega en `NavegacionJugadorController.SolicitarEntradaCiudad`. |
| <xref:MareImperium.CamaraFija> | `MonoBehaviour` | Fija el tamaño ortográfico de la cámara en escenas donde no debe haber zoom. |

---

## Algoritmo de navegación

`RutaCalculadorTilemap` implementa A* estándar sobre casillas hexagonales. La variante `CalcularRutaConRuido` añade una perturbación aleatoria al coste de cada casilla para producir rutas que no parecen mecánicas.

## Interacción con combate

Cuando una flota PNJ alcanza el radio de detección del jugador, `MapamundiController.ComprobarProximidadCombate()` bloquea los iconos y dispara `CombateEventos.DispararCombate`. Al terminar el combate, `CombateEventos.DispararFinCombate` reanuda la navegación.
