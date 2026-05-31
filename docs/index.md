# Mare Imperium — Documentación Técnica

Videojuego 2D de estrategia marítima medieval desarrollado en Unity 2D con C#.
Ambientado en la época de la Liga Hanseática. Las mecánicas de referencia son la saga **Patrician** (especialmente Patrician III/IV): comercio marítimo entre ciudades, gestión de flotas, combate naval por turnos y simulación económica reactiva.

**Alumno:** Miguel Menéndez Caro  
**Centro:** Colegios Marianistas Santa Ana y San Rafael — 2º DAM  
**Motor:** Unity 2D · **Lenguaje:** C# · **Base de datos:** SQLite

---

## Módulos del proyecto

### Módulos Verticales (jugabilidad)

| # | Módulo | Estado | Clases principales |
|---|---|---|---|
| 1 | [Económico](modulos/economico.md) | ✅ Implementado | `MarketManager`, `BienData`, `OficinaComercial` |
| 2 | [Producción y Cadenas](modulos/produccion.md) | ⚠️ Parcial | Schema SQLite definido; gestor pendiente |
| 3 | [Combate Naval](modulos/combate-naval.md) | ⚠️ Parcial | `CombateNavalResolver`, `GestorCombatesActivos`, `CombateEnCurso` |
| 4 | [Construcción de Navíos](modulos/construccion-navios.md) | ✅ Implementado | `AstilleroManager`, `BarcoJugador`, `IBarco` |
| 5 | [Ciudades](modulos/ciudades.md) | ✅ Implementado | `CiudadController`, `CiudadData`, `EdificioClickable` |

### Módulos Transversales (infraestructura)

| # | Módulo | Estado | Clases principales |
|---|---|---|---|
| 6 | [Mundo y Navegación](modulos/mundo-navegacion.md) | ✅ Implementado | `MapamundiController`, `NavegacionJugadorController`, `RutaCalculadorTilemap` |
| 7 | [Flotas y Tripulación](modulos/flotas-tripulacion.md) | ✅ Implementado | `FlotaJugador`, `TabernaManager`, `ConvoyManager` |
| 8 | [Comportamiento de PNJs](modulos/pnjs.md) | ✅ Implementado | `FlotaManager`, `ComerciantePNJController`, `PirataBrain` |
| 9 | [Tiempo y Simulación](modulos/tiempo-simulacion.md) | ✅ Implementado | `SimulacionTiempo`, `EstadoPartida` |
| 10 | [Interfaz de Usuario](modulos/interfaz-usuario.md) | ✅ Implementado | `SceneController`, `HUDDinero`, `HUDTiempo` |
| 11 | [Guardado y Carga](modulos/guardado-carga.md) | ✅ Implementado | `DatabaseManager`, `SaveManager`, `LoadManager` |
| 12 | [Audio y Feedback Visual](modulos/audio-feedback.md) | ❌ Pendiente | — |

---

## Referencia de API

La **Referencia de API** contiene la documentación generada automáticamente de todas las clases públicas a partir de los comentarios XMLDoc del código fuente. Accesible desde la barra de navegación superior.
