# Diagramas UML — TFG Videojuego Estrategia Marítima

Diagramas Mermaid generados a partir del código fuente real en `Assets/Scripts/`.
Renderizables en GitHub, VS Code (extensión Mermaid Preview) o [mermaid.live](https://mermaid.live).

## Índice de diagramas

| Archivo | Tipo | Descripción |
|---|---|---|
| [01_navegacion_escenas.mmd](01_navegacion_escenas.mmd) | `flowchart` | Navegación completa entre las cuatro escenas Unity (MenuPrincipal, Mapamundi, Ciudad, Mercado) y los paneles internos de Ciudad. Muestra qué método de `SceneController` activa cada transición y los singletons persistentes con `DontDestroyOnLoad`. |
| [02_estados_comerciante.mmd](02_estados_comerciante.mmd) | `stateDiagram-v2` | Máquina de estados de `ComerciantePNJController`. Cubre los estados `EnPuerto`, `Viajando`, `Comerciando` y `Huyendo`, incluyendo las condiciones de transición (análisis de margen, saturación de rutas, stock cero) y el algoritmo de estimación de precios con ruido según `InteligenciaComercial`. |
| [03_estados_pirata.mmd](03_estados_pirata.mmd) | `stateDiagram-v2` | Máquina de estados de `PirataPNJController` + `PirataBrain`. Refleja la arquitectura asíncrona: `BucleDeteccion` (Task) decide `Patrullando` / `Interceptando`; `BucleNavegacion` (Task) calcula rutas A* sin bloquear el hilo principal; `TickHuyendo` gestiona el cooldown de 2 días tras un combate. |
| [04_flujo_combate.mmd](04_flujo_combate.mmd) | `flowchart` | Ciclo completo del sistema de combate naval. Desde la detección de proximidad en `FlotaIconoMapamundi` hasta la resolución turno a turno en `CombateEnCurso` (cada hora de juego). Incluye las dos ramas de finalización: combate del jugador (con UI de resultado) y combate PNJ vs PNJ silencioso (con captura de barcos). |
| [05_guardado_carga.mmd](05_guardado_carga.mmd) | `flowchart` | Los 10 pasos de `SaveManager.GuardarPartida()` y los 9 pasos de `LoadManager.CargarPartida()` en paralelo. Detalla el orden de inserción en SQLite (respetando dependencias de FK), la estrategia DELETE+REINSERT para flotas PNJ y el fallback por tipo de módulo al restaurar barcos. |
| [06_patron_decorator.mmd](06_patron_decorator.mmd) | `classDiagram` | Diagrama de clases completo del patrón Decorator aplicado al sistema de barcos. Muestra `IBarco` (Component), `TipoCascoData` (ConcreteComponent), `CascoDecorador` abstracto, los cuatro cascos concretos (`CascoCog`, `CascoHulk`, `CascoCarraca`, `CascoGalera`) con sus stats hardcoded, `ModuloBarcoData` con sus deltas de stats, `BarcoJugador` como cliente que agrega casco + módulos, y `AstilleroManager` como factoría. Incluye todas las relaciones de herencia, implementación y composición. |

## Cómo visualizar

### VS Code
Instala la extensión **Markdown Preview Mermaid Support** o **Mermaid Preview**.
Abre el `.mmd` y lanza la previsualización con `Ctrl+Shift+V`.

### GitHub
Los archivos `.mmd` con el bloque de código ` ```mermaid ` se renderizan automáticamente.
Para previsualizar este README con los diagramas incrustados, copia el contenido del `.mmd` dentro de un bloque de código Mermaid en un `.md`.

### mermaid.live
Pega el contenido de cualquier `.mmd` en [https://mermaid.live](https://mermaid.live) para edición interactiva y exportación a SVG/PNG.
