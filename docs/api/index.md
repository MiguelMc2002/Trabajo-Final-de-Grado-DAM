# Referencia de API — Mare Imperium

Índice de todas las clases públicas organizadas por módulo.
Cada enlace lleva a la página generada por DocFX con la documentación XMLDoc completa.

---

## Módulo 1: Económico

Simulación de mercado en múltiples ciudades. Precios reactivos a oferta/demanda.
Fórmula: `precio_actual = precio_base × (stock_max / max(stock_actual, 1))`

| Clase | Descripción |
|---|---|
| [BienData](xref:MareImperium.BienData) | ScriptableObject con los datos estáticos de un bien comercial. |
| [CategoriaBien](xref:MareImperium.CategoriaBien) | Enum: Primario / Intermedio / Avanzado. |
| [MarketManager](xref:MareImperium.MarketManager) | Gestor del mercado de una ciudad: stocks, precios y transacciones. |
| [OficinaComercial](xref:MareImperium.OficinaComercial) | Intermediario de operaciones entre mercado, almacén y bodega. |
| [MercadoUI](xref:MareImperium.MercadoUI) | Panel modal del mercado en la escena Ciudad. |
| [MarketRowUI](xref:MareImperium.MarketRowUI) | Fila del mercado: muestra datos de un bien y gestiona botones de compra/venta. |

---

## Módulo 2: Producción y Cadenas

Edificios de producción y transformación de materias primas en bienes intermedios y avanzados.

| Clase | Descripción |
|---|---|
| [EntradaMercado](xref:MareImperium.EntradaMercado) | Entrada de mercado de una ciudad: stock, producción, consumo y precio. |

---

## Módulo 3: Combate Naval

Resolución de combates entre flotas. Automático (PNJ vs PNJ) y asíncrono por turnos (jugador).

| Clase | Descripción |
|---|---|
| [CombateNavalResolver](xref:MareImperium.CombateNavalResolver) | Resolución instantánea de combate entre dos flotas. |
| [CombateEnCurso](xref:MareImperium.CombateEnCurso) | Estado de un combate activo, resuelto un turno por hora de juego. |
| [GestorCombatesActivos](xref:MareImperium.GestorCombatesActivos) | Singleton que gestiona todos los combates en el mapamundi. |
| [CombateEventos](xref:MareImperium.CombateEventos) | Clase estática de eventos globales de combate. |
| [ResultadoCombate](xref:MareImperium.ResultadoCombate) | DTO con el resultado de un combate ya resuelto. |
| [EncuentroNavalUI](xref:MareImperium.EncuentroNavalUI) | UI del panel de encuentro naval (atacar / huir / ignorar). |
| [ResultadoCombateUI](xref:MareImperium.ResultadoCombateUI) | Panel de resultados tras finalizar un combate. |

---

## Módulo 4: Construcción y Personalización de Navíos

Cascos base con patrón Decorador, módulos de mejora y gestión del astillero.

| Clase | Descripción |
|---|---|
| [IBarco](xref:MareImperium.IBarco) | Interfaz base que define las propiedades de cualquier barco. |
| [CascoDecorador](xref:MareImperium.CascoDecorador) | Clase base abstracta del patrón Decorador para módulos de barco. |
| [TipoCascoData](xref:MareImperium.TipoCascoData) | ScriptableObject con las estadísticas base de un casco. |
| [CascoCog](xref:MareImperium.CascoCog) | Implementación del casco Cog (mercante rápido). |
| [CascoHulk](xref:MareImperium.CascoHulk) | Implementación del casco Hulk (alta capacidad de carga). |
| [CascoCarraca](xref:MareImperium.CascoCarraca) | Implementación del casco Carraca (equilibrado). |
| [CascoGalera](xref:MareImperium.CascoGalera) | Implementación del casco Galera (combate rápido). |
| [ModuloBarcoData](xref:MareImperium.ModuloBarcoData) | ScriptableObject de un módulo de mejora (armamento, velas, bodega). |
| [TipoModulo](xref:MareImperium.TipoModulo) | Enum de tipos de módulo. |
| [BarcoJugador](xref:MareImperium.BarcoJugador) | Estado en tiempo de ejecución de un barco del jugador. |
| [AstilleroManager](xref:MareImperium.AstilleroManager) | Lógica de negocio del astillero: compra, venta y mejoras de barcos. |
| [AstilleroUI](xref:MareImperium.AstilleroUI) | Panel UI del astillero. |

---

## Módulo 5: Ciudades

Datos estáticos de ciudades, pantalla de ciudad y acceso a sus instalaciones.

| Clase | Descripción |
|---|---|
| [CiudadData](xref:MareImperium.CiudadData) | ScriptableObject con los datos fijos de una ciudad (mercado inicial, edificios). |
| [CiudadController](xref:MareImperium.CiudadController) | Controlador de la escena Ciudad: abre y cierra paneles de mercado, astillero y taberna. |
| [EdificioClickable](xref:MareImperium.EdificioClickable) | Edificio interactivo en la escena Ciudad que notifica al controlador al hacer clic. |
| [PanelAstilleroUI](xref:MareImperium.PanelAstilleroUI) | Contenedor del panel del astillero dentro de la escena Ciudad. |
| [PanelTabernaUI](xref:MareImperium.PanelTabernaUI) | Contenedor del panel de la taberna dentro de la escena Ciudad. |

---

## Módulo 6: Mundo y Navegación

Tilemap hexagonal con pathfinding A*, marcadores de ciudad y sistema de niebla de guerra.

| Clase | Descripción |
|---|---|
| [MapamundiController](xref:MareImperium.MapamundiController) | Controlador principal del mapamundi: inicializa flotas, iconos y gestiona viajes. |
| [MapamundiCamara](xref:MareImperium.MapamundiCamara) | Cámara del mapamundi con zoom, scroll por bordes y desplazamiento WASD. |
| [RutaCalculadorTilemap](xref:MareImperium.RutaCalculadorTilemap) | Pathfinding A* sobre tilemap hexagonal. |
| [TileNavegable](xref:HansaTrader.Mapamundi.TileNavegable) | Tile personalizado con coste de movimiento y bandera de transitabilidad. |
| [MarcadorCiudad](xref:MareImperium.MarcadorCiudad) | Marcador clickable de ciudad en el mapamundi. |
| [NavegacionJugadorController](xref:MareImperium.NavegacionJugadorController) | Gestiona la ruta del jugador y la detección de llegada a ciudades. |
| [DebugRutaCalculador](xref:MareImperium.DebugRutaCalculador) | Utilidad de depuración para el calculador de rutas (solo en Editor). |

---

## Módulo 7: Flotas y Gestión de Tripulación

Flotas del jugador, flotas PNJ en tiempo de ejecución y su representación visual en el mapa.

| Clase | Descripción |
|---|---|
| [FlotaJugador](xref:MareImperium.FlotaJugador) | Flota del jugador: lista de barcos, modo pirata y conversión a FlotaRuntimeData. |
| [FlotaRuntimeData](xref:MareImperium.FlotaRuntimeData) | Estado en tiempo de ejecución compartido por todas las flotas (jugador y PNJ). |
| [FlotaManager](xref:MareImperium.FlotaManager) | Gestiona todas las flotas PNJ activas en el mundo. |
| [FlotaIconoMapamundi](xref:MareImperium.FlotaIconoMapamundi) | Icono visual de una flota en el tilemap; gestiona movimiento interpolado. |

---

## Módulo 8: Comportamiento de PNJs

Máquinas de estado para comerciantes y piratas. Pathfinding con ruido para variedad.

| Clase | Descripción |
|---|---|
| [ComerciantePNJController](xref:MareImperium.ComerciantePNJController) | Controlador del PNJ comerciante: viaja entre ciudades comprando barato y vendiendo caro. |
| [PirataPNJController](xref:MareImperium.PirataPNJController) | Controlador del PNJ pirata: patrulla y persigue flotas enemigas. |
| [PirataBrain](xref:MareImperium.PirataBrain) | Cerebro asíncrono del pirata: calcula rutas en hilo secundario vía ConcurrentQueue. |
| [PirataBrainBootstrapper](xref:MareImperium.PirataBrainBootstrapper) | Singleton que construye el grafo de navegación e inicializa los brains de los piratas. |
| [EstadoFlotaPNJ](xref:MareImperium.EstadoFlotaPNJ) | Enum de estados de la máquina de estados de flotas PNJ. |

---

## Módulo 9: Tiempo y Simulación

Velocidades de juego, tick diario y desbloques de tecnología por fecha.

| Clase | Descripción |
|---|---|
| [SimulacionTiempo](xref:MareImperium.SimulacionTiempo) | Gestiona el reloj de juego, velocidades y emite eventos de nuevo día/hora. |

---

## Módulo 10: Interfaz de Usuario

Panels de HUD, menús y pantallas transversales.

| Clase | Descripción |
|---|---|
| [HUDTiempo](xref:MareImperium.HUDTiempo) | HUD persistente que muestra la fecha y la velocidad de simulación. |
| [HUDDinero](xref:MareImperium.HUDDinero) | HUD que muestra el dinero del jugador con formato localizado. |
| [MenuPrincipalUI](xref:MareImperium.MenuPrincipalUI) | Pantalla del menú principal: nueva partida, cargar, salir. |
| [MenuPausa](xref:MareImperium.MenuPausa) | Panel de pausa: continuar, menú principal, escritorio. |
| [PopUpEntradaCiudad](xref:MareImperium.PopUpEntradaCiudad) | Pop-up de confirmación al llegar a una ciudad. |
| [SeleccionCiudadUI](xref:MareImperium.SeleccionCiudadUI) | Fila de selección de ciudad en la pantalla de nueva partida. |
| [PanelFlotaUI](xref:MareImperium.PanelFlotaUI) | Panel que muestra los barcos de la flota del jugador con sus estadísticas. |
| [PanelInspeccionFlota](xref:MareImperium.PanelInspeccionFlota) | Panel de inspección de una flota (jugador o PNJ) con lista de barcos y Modo Pirata. |

---

## Módulo 11: Guardado y Carga

Persistencia SQLite con 5 slots. Un fichero .db por partida.

| Clase | Descripción |
|---|---|
| [DatabaseManager](xref:MareImperium.DatabaseManager) | Abre, cierra y expone la conexión SQLite de la partida activa. |
| [SaveManager](xref:MareImperium.SaveManager) | Serializa el estado completo del mundo a SQLite. |
| [LoadManager](xref:MareImperium.LoadManager) | Deserializa y restaura el estado del mundo desde SQLite. |
| [PantallaSlotsUI](xref:MareImperium.PantallaSlotsUI) | Pantalla de slots de guardado/carga con modos Guardar y Cargar. |
| [SlotUI](xref:MareImperium.SlotUI) | Fila individual de slot en la pantalla de slots. |
| [SlotData](xref:MareImperium.SlotData) | DTO con los metadatos de un slot (nombre, fecha, ruta de fichero). |
| [EstadoPartida](xref:MareImperium.EstadoPartida) | Estado completo de la partida en memoria (fecha, mercados, flotas, edificios). |

**DAOs:**

| Clase | Tabla SQLite |
|---|---|
| [AlmacenJugadorDAO](xref:MareImperium.AlmacenJugadorDAO) | AlmacenJugador |
| [AlmacenCiudadDAO](xref:MareImperium.AlmacenCiudadDAO) | AlmacenCiudad |
| [BarcoDAO](xref:MareImperium.BarcoDAO) | Barco |
| [BienDAO](xref:MareImperium.BienDAO) | Bien |
| [CargaBarcoDAO](xref:MareImperium.CargaBarcoDAO) | CargaBarco |
| [CapitanDAO](xref:MareImperium.CapitanDAO) | Capitan |
| [CiudadDAO](xref:MareImperium.CiudadDAO) | Ciudad |
| [EdificiosCiudadDAO](xref:MareImperium.EdificiosCiudadDAO) | EdificiosCiudad |
| [EstadoJuegoDAO](xref:MareImperium.EstadoJuegoDAO) | estadoJuego |
| [EstadoMercadoCiudadDAO](xref:MareImperium.EstadoMercadoCiudadDAO) | EstadoMercadoCiudad |
| [FlotaDAO](xref:MareImperium.FlotaDAO) | Flota |
| [MemoriaComercialPNJDAO](xref:MareImperium.MemoriaComercialPNJDAO) | MemoriaComercialPNJ |
| [ModuloBarcoDAO](xref:MareImperium.ModuloBarcoDAO) | ModuloBarco |

---

## Módulo 12: Audio y Feedback Visual

*(Módulo pendiente de implementación completa)*

---

## Módulo Transversal: Core

Clases de infraestructura que no pertenecen a un módulo vertical concreto.

| Clase | Descripción |
|---|---|
| [GameManager](xref:MareImperium.GameManager) | Singleton central: dinero, almacén, ciudades disponibles, flota del jugador. |
| [SceneController](xref:MareImperium.SceneController) | Fachada estática para navegar entre escenas. |
| [CamaraFija](xref:MareImperium.CamaraFija) | Fija la posición de la cámara a los valores configurados en el Inspector. |

---

## Taberna y Convoyes

| Clase | Descripción |
|---|---|
| [TabernaManager](xref:MareImperium.TabernaManager) | Gestiona las misiones de convoy disponibles en la taberna. |
| [TabernaUI](xref:MareImperium.TabernaUI) | Panel UI de la taberna: lista contratos y gestiona la contratación. |
| [ConvoyManager](xref:MareImperium.ConvoyManager) | Gestiona convoyes activos del jugador. |
| [ConvoyData](xref:MareImperium.ConvoyData) | Datos de un contrato de convoy: ruta, carga y recompensa. |
