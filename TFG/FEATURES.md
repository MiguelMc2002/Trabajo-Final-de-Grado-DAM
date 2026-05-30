# FEATURES.md — TFG

Documentación de la API pública organizada según la estructura de módulos oficial (`ModulosProyecto.md`).  
Actualizar este fichero cada vez que se añada o modifique un miembro público relevante.

---

## MÓDULOS VERTICALES

---

## Módulo 1: Económico
**Estado:** ✅ Implementado  
**Dependencias:** Módulo de Ciudades, Módulo de PNJs, Módulo de Guardado y Carga

Simula el mercado en múltiples ciudades con stock, precios reactivos y compra/venta por jugador y PNJs.  
Fórmula de precio: `precio_actual = precio_base × (stock_max / max(stock_actual, 1))`

### BienData *(ScriptableObject)*
`Assets/Scripts/Economico/BienData.cs`

| Campo / Propiedad | Tipo | Descripción |
|---|---|---|
| `nombre` | `string` | Nombre del bien (ej: "Grano"). |
| `categoria` | `CategoriaBien` | Primario / Intermedio / Avanzado. |
| `precioBase` | `float` | Precio base para la fórmula de mercado. |
| `stockMaximo` | `int` | Stock máximo de referencia para calcular precios. |

**Enum asociado:** `CategoriaBien { Primario, Intermedio, Avanzado }`

---

### EntradaMercado
`Assets/Scripts/Economico/EntradaMercado.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Bien` | `BienData` | Referencia al bien que representa esta entrada. |
| `StockActual` | `int` | Stock disponible en el mercado de la ciudad. |
| `StockMax` | `int` | Stock máximo de referencia. |
| `ProduccionDiaria` | `int` | Unidades producidas por día. |
| `ConsumoDiario` | `int` | Unidades consumidas por día. |
| `PrecioActual` | `float` | Precio calculado con la fórmula reactiva. |

---

### MarketManager *(MonoBehaviour)*
`Assets/Scripts/Economico/MarketManager.cs`

| Método | Retorno | Descripción |
|---|---|---|
| `InicializarConCiudad(CiudadData)` | `void` | Vincula el mercado a una ciudad. Llamar desde `CiudadController.Start()`. |
| `GetEntradas()` | `IReadOnlyList<EntradaMercado>` | Lista completa de entradas del mercado. |
| `GetNombreCiudad()` | `string` | Nombre de la ciudad actual. |
| `GetStockActual(BienData)` | `int` | Stock actual del bien en esta ciudad. |
| `GetPrecioActual(BienData)` | `float` | Precio actual calculado. |
| `Comprar(BienData, int)` | `bool` | Reduce stock y cobra al jugador. |
| `Vender(BienData, int)` | `bool` | Aumenta stock y paga al jugador. |

**Evento:** `OnMercadoActualizado` — disparado tras cualquier transacción.

---

### OficinaComercial
`Assets/Scripts/Economico/OficinaComercial.cs`

Intermediario que abstrae el origen/destino de mercancías (mercado, almacén ciudad, bodega barco).

| Miembro | Tipo | Descripción |
|---|---|---|
| `UltimoMensaje` | `string` | Texto del último resultado de operación. |
| `Inicializar(MarketManager)` | `void` | Vincula al mercado de la ciudad actual. |
| `Comprar(BienData, int)` | `bool` | Compra desde el mercado al almacén del jugador. |
| `Vender(BienData, int)` | `bool` | Vende desde el almacén del jugador al mercado. |
| `Transferir(BienData, int, OrigenDestino, OrigenDestino)` | `bool` | Mueve cantidad entre dos ubicaciones. |

**Enum:** `OrigenDestino { Mercado, AlmacenCiudad, BodegaBarco }`

---

### GameManager — sección económica *(MonoBehaviour singleton)*
`Assets/Scripts/Core/GameManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Dinero` | `long` | Oro actual del jugador. |
| `SetDinero(long)` | `void` | Asigna el dinero directamente (carga de partida). |
| `ModificarDinero(long)` | `bool` | Suma o resta; devuelve `false` si resulta negativo. |
| `GetCantidadBien(BienData)` | `int` | Unidades del bien en el almacén del jugador. |
| `ModificarCantidadBien(BienData, int)` | `bool` | Modifica cantidad; devuelve `false` si resulta negativa. |
| `GetTotalUnidadesAlmacen()` | `int` | Total de unidades en el almacén del jugador. |
| `GetAlmacen()` | `IReadOnlyDictionary<BienData, int>` | Snapshot del almacén del jugador. |
| `GetCantidadAlmacenCiudad(int, int)` | `int` | Cantidad de un bien en el almacén de una ciudad. |
| `SetCantidadAlmacenCiudad(int, int, int)` | `void` | Asigna cantidad (carga de partida). |
| `ModificarAlmacenCiudad(int, int, int)` | `bool` | Modifica inventario de almacén de ciudad. |
| `GetAlmacenCiudad(int)` | `Dictionary<int, int>` | Almacén completo de una ciudad (id_ciudad → clave). |
| `MercadosPorCiudad` | `IReadOnlyDictionary<int, List<EntradaMercado>>` | Estado de todos los mercados en memoria. |
| `TieneMercado(int)` | `bool` | Comprueba si una ciudad tiene mercado cargado. |
| `GetEntradasMercado(int)` | `IReadOnlyList<EntradaMercado>` | Entradas de mercado de una ciudad concreta. |
| `RegistrarMercadoCiudad(int, List<EntradaMercado>)` | `void` | Registra o actualiza el mercado de una ciudad. |
| `LimpiarMercados()` | `void` | Vacía todos los mercados (antes de cargar partida). |
| `NotificarMercadoActualizado(int, BienData)` | `void` | Dispara `OnMercadoCiudadActualizado`. |

**Eventos:** `OnDineroActualizado (static event Action<long>)` · `OnMercadoCiudadActualizado (event Action<int, BienData>)`

---

## Módulo 2: Producción y Cadenas
**Estado:** ⚠️ Parcial  
**Dependencias:** Módulo Económico, Módulo de Ciudades

Tablas `RecetaProduccion` y `EdificiosCiudad` definidas en el schema SQLite. Los DAOs `CiudadDAO.MigrarColumnasCasilla()` mantienen la estructura de ciudad. La lógica de producción diaria (consumo de materias primas → generación de productos) **no tiene gestor activo implementado** en la versión actual.

> **TO-DO:** Implementar `ProduccionCiudadManager` que procese recetas por ciudad en cada tick de `SimulacionTiempo.OnNuevoDia`.

---

## Módulo 3: Combate Naval
**Estado:** ⚠️ Parcial — resolución asíncrona automática implementada; tablero grid manual pendiente  
**Dependencias:** Módulo de Flotas, Módulo de Mundo y Navegación, Módulo de PNJs

### CombateNavalResolver *(clase estática)*
`Assets/Scripts/Combate/CombateNavalResolver.cs`

| Método | Retorno | Descripción |
|---|---|---|
| `Resolver(FlotaRuntimeData, FlotaRuntimeData, bool, bool)` | `ResultadoCombate` | Resolución instantánea (sin animación). Usado para combates PNJ vs PNJ en segundo plano. |

---

### CombateEnCurso *(clase pura C#)*
`Assets/Scripts/Combate/CombateEnCurso.cs`

Combate asíncrono por turnos. Un turno = una hora de juego. Timeout de 5 días (120 turnos) = evasión del defensor.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Atacante` | `FlotaRuntimeData` | Flota que inició el encuentro. |
| `Defensor` | `FlotaRuntimeData` | Flota que recibió el ataque. |
| `TurnoActual` | `int` | Turnos completados. |
| `EsDelJugador` | `bool` | `true` si alguna flota tiene `Id == -1`. |
| `TerminoPorTimeout` | `bool` | `true` tras 120 turnos sin vencedor. |
| `FlotaEnemiga` | `FlotaRuntimeData` | Flota enemiga del jugador. |
| `JugadorGano` | `bool` | La flota del jugador sobrevive y la enemiga fue destruida. |
| `Ganador` | `FlotaRuntimeData` | Flota superviviente; `null` si ambas destruidas. |
| `NumBarcosInicialAtacante` | `int` | Barcos al inicio en la flota atacante. |
| `NumBarcosInicialDefensor` | `int` | Barcos al inicio en la flota defensora. |

| Método | Retorno | Descripción |
|---|---|---|
| `CombateEnCurso(atacante, defensor, esDelJugador)` | — | Constructor. Guarda stats iniciales barco a barco. |
| `ResolverTurno()` | `bool` | Avanza un turno con varianza ±30 %. Devuelve `true` al terminar. |

**Constantes:** `MaxTurnos = 120` · `FuerzaBaseXBarco = 5f` · `MultiplicadorDanio = 0.15f`

---

### GestorCombatesActivos *(MonoBehaviour singleton)*
`Assets/Scripts/Combate/GestorCombatesActivos.cs`

Gestiona todos los combates activos en el mapamundi. Se suscribe a `SimulacionTiempo.OnNuevaHora`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `GestorCombatesActivos` | Punto de acceso global. |
| `IniciarCombate(atacante, defensor, esDelJugador)` | `void` | Registra el combate; rechaza si alguna flota ya está en uno. Pausa iconos. |
| `OnCombateJugadorTerminado` | `static event Action<CombateEnCurso>` | Disparado al terminar un combate con el jugador. |

---

### CombateEventos *(clase estática)*
`Assets/Scripts/Combate/CombateEventos.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `CombateJugadorEnCurso` | `static bool` | `true` entre `DispararCombate` y `DispararFinCombate`. |
| `DispararCombate(atacante, defensor)` | `static void` | Pone flag en `true` e invoca `OnCombateIniciado`. |
| `DispararFinCombate()` | `static void` | Pone flag en `false` e invoca `OnCombateTerminado`. |
| `OnCombateIniciado` | `static event Action` | Escena Mapamundi lo escucha para pausar navegación. |
| `OnCombateTerminado` | `static event Action` | Escena Mapamundi lo escucha para reanudar. |

---

### ResultadoCombateUI *(MonoBehaviour)*
`Assets/Scripts/Combate/ResultadoCombateUI.cs`

Panel modal post-combate. Se suscribe a `GestorCombatesActivos.OnCombateJugadorTerminado`.  
Pausa `Time.timeScale = 0f` mientras está visible.

| Método | Descripción |
|---|---|
| `OcultarResultado()` | Cierra panel, reanuda tiempo, dispara `CombateEventos.DispararFinCombate`. |

**Acciones post-victoria:** Destruir · Saquear (40 % de la carga enemiga + oro) · Capturar

**Sistema de captura (constantes):**

| Constante | Valor | Descripción |
|---|---|---|
| `ProbabilidadCapturaBase` | `0.30f` | Prob. base por barco (tripulación mínima). |
| `ProbabilidadCapturaMax` | `0.50f` | Prob. máxima (tripulación al límite). |
| `TripulacionMaxReferencia` | `50` | Referencia de interpolación. |

Cada barco enemigo hace una tirada independiente. Los capturados quedan con **1 de vida**. El botón Capturar se oculta si la flota está al límite (5 barcos) o si todos los enemigos fueron destruidos.

---

### ResultadoCombate *(clase pura C#, inmutable)*
`Assets/Scripts/Combate/ResultadoCombate.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Atacante` / `Defensor` | `FlotaRuntimeData` | Flotas participantes. |
| `JugadorGana` | `bool` | Resultado final. |
| `BarcosPerdidosAtacante` / `BarcosPerdidosDefensor` | `int` | Bajas de cada flota. |
| `DanioRecibidoAtacante` / `DanioRecibidoDefensor` | `float` | Daño total recibido. |
| `BotinOro` | `long` | Oro obtenido al vencer. |
| `BotinMercancia` | `Dictionary<int, int>` | Mercancía obtenida. |
| `TextoNarrativo` | `string` | Descripción generada del resultado. |
| `JugadorHuyo` | `bool` | `true` si el jugador se retiró. |

> **TO-DO:** Combate manual en tablero grid (movimiento por casillas, elección de sección a atacar: timón / velas / armamento / flotación, abordaje con unidades de tripulación).

---

## Módulo 4: Construcción y Personalización de Navíos
**Estado:** ✅ Implementado  
**Dependencias:** Módulo Económico (coste en oro), Módulo de Flotas, Módulo de Ciudades

Patrón Decorator sobre `IBarco`. Cuatro cascos base + módulos de armamento, velas y bodega.

### IBarco *(interfaz)*
`Assets/Scripts/Astillero/IBarco.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdTipoCasco` | `int` | 1=Cog · 2=Hulk · 3=Carraca · 4=Galera |
| `NombreCasco` | `string` | Nombre del tipo de casco. |
| `VidaBase` | `int` | Puntos de vida base. |
| `VelocidadBase` | `int` | Velocidad base. |
| `ManiobrabilidadBase` | `int` | Maniobrabilidad base. |
| `CapacidadCargaBase` | `int` | Capacidad de bodega base. |
| `CapacidadModulos` | `int` | Slots de módulos disponibles. |
| `CapacidadTripulacion` | `int` | Máximo de tripulantes. |
| `CosteOro` | `int` | Precio de construcción. |

**Implementaciones:** `TipoCascoData` (ScriptableObject) · `CascoDecorador` (base Decorator) · `CascoCog` · `CascoHulk` · `CascoCarraca` · `CascoGalera`

---

### ModuloBarcoData *(ScriptableObject)*
`Assets/Scripts/Astillero/ModuloBarcoData.cs`

| Campo | Tipo | Descripción |
|---|---|---|
| `nombreModulo` | `string` | Nombre visible del módulo. |
| `tipoModulo` | `TipoModulo` | Armamento / Velas / Bodega. |
| `slotsCosto` | `int` | Slots que ocupa en el casco. |
| `costeOro` | `int` | Precio de instalación. |
| `deltaVida` | `int` | Modificador de vida. |
| `deltaVelocidad` | `int` | Modificador de velocidad. |
| `deltaManiobrabilidad` | `int` | Modificador de maniobrabilidad. |
| `deltaCargaMaxima` | `int` | Modificador de bodega. |
| `deltaFuerzaCombate` | `int` | Modificador de fuerza de combate. |
| `requierePolvora` | `bool` | Desbloqueable solo a partir del año 1380. |
| `AnioDesbloqueoPolvoraJuego` | `const int = 1380` | Año de desbloqueo de armas de pólvora. |

**Enum:** `TipoModulo { Armamento, Velas, Bodega }`

---

### BarcoJugador *(clase pura C#)*
`Assets/Scripts/Astillero/BarcoJugador.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdBarco` | `int` | Identificador único. |
| `Nombre` | `string` | Nombre del barco. |
| `CascoBase` | `IBarco` | Casco base (con decoradores aplicados). |
| `ModulosInstalados` | `IReadOnlyList<ModuloBarcoData>` | Módulos activos. |
| `EsBarcosCombate` | `bool` | Barco de combate o de carga. |
| `VidaActual` | `int` | Vida actual. |
| `Tripulacion` | `int` | Tripulación actual (0 por defecto). |
| `TripulacionMaxima` | `int` | `CascoBase.CapacidadTripulacion`. |
| `VidaTotal` | `int` | Base + deltas de módulos. |
| `VelocidadTotal` | `int` | Base + deltas de módulos. |
| `ManiobrabilidadTotal` | `int` | Base + deltas de módulos. |
| `CargaMaximaTotal` | `int` | Base + deltas de módulos. |
| `FuerzaCombateTotal` | `int` | Suma de `deltaFuerzaCombate` de módulos. |
| `SlotsUsados` | `int` | Total de slots consumidos. |
| `SlotsDisponibles` | `int` | `CapacidadModulos - SlotsUsados`. |

| Método | Retorno | Descripción |
|---|---|---|
| `InstalarModulo(ModuloBarcoData)` | `bool` | Instala si hay slots y no hay módulo del mismo tipo. |
| `DesinstalarModulo(ModuloBarcoData)` | `bool` | Desinstala y libera slots. |
| `PuedeInstalar(ModuloBarcoData)` | `bool` | Comprueba slots y unicidad de tipo. |
| `ContratarMarineros(int)` | `int` | Incrementa hasta el hueco. Devuelve los realmente contratados. |
| `LicenciarMarineros(int)` | `int` | Reduce sin bajar de 0. |
| `ObtenerModuloPorTipo(TipoModulo)` | `ModuloBarcoData` | Módulo instalado de ese tipo; `null` si ninguno. |

---

### AstilleroManager *(MonoBehaviour singleton)*
`Assets/Scripts/Astillero/AstilleroManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `AstilleroManager` | Punto de acceso global. |
| `CascosDisponibles` | `IReadOnlyList<IBarco>` | Cascos base + decoradores disponibles para construcción. |
| `ModulosDisponibles` | `List<ModuloBarcoData>` | Todos los módulos configurados en el Inspector. |
| `ComprarBarco(IBarco, string)` | `ResultadoOperacion` | Crea `BarcoJugador`, cobra oro y añade a `FlotaJugador`. |
| `InstalarModulo(BarcoJugador, ModuloBarcoData)` | `ResultadoOperacion` | Cobra diferencia de precio e instala. |
| `RepararBarco(BarcoJugador)` | `ResultadoOperacion` | Cobra `daño × 10` oro y restaura vida. |
| `VenderBarco(BarcoJugador)` | `ResultadoOperacion` | Recibe 50 % del coste total, elimina de la flota. |

---

### AstilleroUI *(MonoBehaviour)*
`Assets/Scripts/Astillero/AstilleroUI.cs`

Panel de astillero con cinco subpaneles: Menú, Construir, Modificar, Reparar, Vender.

| Método | Descripción |
|---|---|
| `AbrirAstillero()` | Abre el panel mostrando el menú principal. |
| `CerrarAstillero()` | Cierra el panel y reactiva el botón de mapa. |
| `MostrarPanel(int)` | 0=Menú · 1=Construir · 2=Modificar · 3=Reparar · 4=Vender. |

Stats en panel Construir: dinámicas (base casco + deltas de módulos seleccionados en tiempo real).

---

## Módulo 5: Ciudades
**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos verticales

### CiudadData *(ScriptableObject)*
`Assets/Scripts/Ciudades/CiudadData.cs`

| Campo | Tipo | Descripción |
|---|---|---|
| `IdCiudad` | `int` | Identificador único (coincide con `id_ciudad` en BD). |
| `NombreCiudad` | `string` | Nombre visible. |
| `CasillaMapamundi` | `Vector3Int` | Coordenada offset en el Tilemap del mapa. |
| `Mercado` | `List<EntradaMercado>` | Estado inicial del mercado. |

**Ciudades configuradas:** Venecia · Génova · Barcelona · Ruan · Lübeck · Brujas

---

### CiudadController *(MonoBehaviour singleton)*
`Assets/Scripts/Ciudades/CiudadController.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `CiudadController` | Punto de acceso global. |
| `DatosCiudad` | `CiudadData` | Ciudad activa en esta sesión. |
| `PanelMercado` / `PanelAstillero` / `PanelTaberna` / `PanelFlota` | `GameObject` | Paneles de edificios. |
| `BotonMapa` | `GameObject` | Botón de regreso al mapamundi. |
| `ReactivarBotonMapa()` | `void` | Reactiva el botón; llamado por `TabernaUI` y `AstilleroUI` al cerrar. |
| `AbrirMercado()` / `AbrirAstillero()` / `AbrirTaberna()` / `AbrirPuerto()` | `void` | Abre el subpanel correspondiente. |
| `CerrarTodosPaneles()` | `void` | Oculta todos los paneles y reactiva botón mapa. |
| `IrAMapamundi()` | `void` | Bloqueado si la flota no tiene barcos. Tecla M. |
| `AbrirEdificio(TipoEdificio)` | `void` | Dispatcher: cierra todo y activa el panel del edificio. |

**Enum:** `TipoEdificio { Mercado, Astillero, Taberna, Puerto }`

---

## MÓDULOS TRANSVERSALES

---

## Módulo 6: Mundo y Navegación
**Estado:** ✅ Implementado  
**Dependencias:** Módulo de PNJs, Módulo de Combate, Módulo de Flotas

### MapamundiController *(MonoBehaviour singleton)*
`Assets/Scripts/Navegacion/MapamundiController.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `MapamundiController` | Punto de acceso global. |
| `Ciudades` | `MarcadorCiudad[]` | Marcadores de ciudad en el mapa. |
| `IconoFlotaJugador` | `FlotaIconoMapamundi` | Icono de la flota del jugador. |
| `ObtenerCiudadEnCasilla(Vector3Int)` | `CiudadData` | Ciudad cuya casilla coincide, o `null`. |
| `AbrirPanelInspeccion(FlotaRuntimeData)` | `void` | Abre `PanelInspeccionFlota` para una flota PNJ. |
| `AbrirPanelJugador()` | `void` | Abre el panel de inspección con los datos del jugador. |
| `ViajarACiudad(CiudadData)` | `void` | Ordena al icono del jugador navegar a esa ciudad. |
| `DesactivarIconoFlota(int)` | `void` | Desactiva el icono de la flota y lo elimina del diccionario. |
| `SpawnIconoFlotaPNJ(FlotaRuntimeData)` | `void` | Instancia icono en runtime para flota recién spawnada. |
| `PausarIcono(int)` / `ReanudarIcono(int)` | `void` | Colorea en naranja / restaura color. `-1` para el jugador. |
| `RestaurarColorIcono(int)` | `void` | Restaura color sin tocar `EnCombate`. |
| `HuirAlPuerto(int)` | `void` | Ordena al icono navegar al puerto más cercano. |
| `IniciarCombateJugadorAtaca(FlotaRuntimeData)` | `void` | Bloquea iconos y dispara `CombateEventos.DispararCombate`. |
| `ComprobarProximidadCombate(FlotaRuntimeData)` | `void` | Comprueba si la flota enemiga está en radio de combate del jugador. |
| `ObtenerPosicionMarAleatoria()` | `Vector2` | Devuelve coordenada world-space sobre casilla marítima válida. |

---

### FlotaIconoMapamundi *(MonoBehaviour)*
`Assets/Scripts/Mapamundi/FlotaIconoMapamundi.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `EnCombate` | `bool` | Detiene el movimiento cuando `true`. |
| `ColorearEnCombate()` | `void` | Colorea el sprite en naranja. |
| `RestaurarColor()` | `void` | Restaura el color original. |
| `HuirAlPuertoMasCercano()` | `void` | Calcula ruta A* a la ciudad más cercana. |

---

### RutaCalculadorTilemap *(MonoBehaviour)*
`Assets/Scripts/Navegacion/RutaCalculadorTilemap.cs`

| Método | Retorno | Descripción |
|---|---|---|
| `CalcularRuta(Vector3Int, Vector3Int)` | `List<Vector3Int>` | A* sobre el Tilemap. |
| `CalcularRutaConRuido(Vector3Int, Vector3Int)` | `List<Vector3Int>` | A* con variación aleatoria de coste para rutas más naturales. |

---

### TileNavegable *(ScriptableObject)*
`Assets/Scripts/Navegacion/TileNavegable.cs`

| Campo | Tipo | Descripción |
|---|---|---|
| `costeMovimiento` | `float` | Coste A* de esta casilla. |
| `esTransitable` | `bool` | Si `false`, la casilla bloquea el paso. |

---

## Módulo 7: Flotas y Gestión de Tripulación
**Estado:** ✅ Implementado  
**Dependencias:** Módulo de Construcción de Navíos, Módulo de Ciudades, Módulo de Guardado y Carga

### FlotaJugador *(clase pura C#)*
`Assets/Scripts/Astillero/FlotaJugador.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `MaxBarcos` | `const int = 5` | Máximo de barcos combatientes en la flota. |
| `Barcos` | `IReadOnlyList<BarcoJugador>` | Lista de barcos activos. |
| `ModoPirata` | `bool` | El jugador opera como pirata. Persiste en `estadoJuego.modo_pirata`. |
| `VidaTotalFlota` | `int` | Suma de `VidaActual` de todos los barcos. |
| `VelocidadFlota` | `float` | Velocidad mínima entre todos los barcos. |
| `ManiobrabilidadMedia` | `float` | Media de maniobrabilidades. |
| `CargaMaximaTotal` | `int` | Suma de `CargaMaximaTotal` de todos los barcos. |
| `FuerzaCombateTotal` | `int` | Suma de fuerzas de combate. |
| `TripulacionTotal` | `int` | Suma de tripulaciones. |
| `AñadirBarco(BarcoJugador)` | `bool` | Añade si no supera `MaxBarcos`. |
| `EliminarBarco(int)` | `bool` | Elimina por `IdBarco`. |
| `LimpiarTodos()` | `void` | Vacía la lista (llamar antes de cargar partida). |
| `AplicarDanioCombate(int, float)` | `void` | Distribuye daño entre los barcos de la flota. |
| `ObtenerBarco(int)` | `BarcoJugador` | Busca por `IdBarco`. |
| `ComoFlotaRuntime()` | `FlotaRuntimeData` | Snapshot con `BarcosFlota` relleno para el sistema de combate. |

---

### TabernaManager *(MonoBehaviour singleton)*
`Assets/Scripts/Taberna/TabernaManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `TabernaManager` | Punto de acceso global. |
| `PrecioMarinero` | `const int = 50` | Oro por marinero contratado. |
| `ContratarMarineros(BarcoJugador, int)` | `ResultadoOperacion` | Oferta ilimitada en ciudad. Falla si hueco ≤ 0 u oro insuficiente. |
| `GetCapitanesDisponibles(int)` | `List<CapitanData>` | Capitanes disponibles en la ciudad indicada. |
| `ContratarCapitan(BarcoJugador, CapitanData)` | `ResultadoOperacion` | Asigna capitán al barco. Cobra 500 oro. |
| `GetCapitanDeBarco(int)` | `CapitanData` | Capitán asignado al barco (por `IdBarco`). |
| `ObtenerBarcosDeLaFlota()` | `IReadOnlyList<BarcoJugador>` | Delega en `GameManager.FlotaJugador.Barcos`. |
| `LimpiarCapitanesContratados()` | `void` | Vacía el diccionario (antes de cargar partida). |
| `RestaurarCapitanContratado(CapitanData)` | `void` | Añade capitán sin flujo de contratación (solo desde `LoadManager`). |

---

### CapitanData *(ScriptableObject)*
`Assets/Scripts/Taberna/CapitanData.cs`

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Identificador único. |
| `Nombre` | `string` | Nombre del capitán. |
| `IdBarcoAsignado` | `int` | `-1` si no está asignado. |
| `HabilidadNavegacion` | `int` | Influye en velocidad de ruta. |
| `HabilidadCombate` | `int` | Influye en resultado de combate. |

---

### PanelFlotaUI *(MonoBehaviour)*
`Assets/Scripts/UI/PanelFlotaUI.cs`

Panel de gestión de flota en escena Ciudad. Navegación circular entre barcos con flechas.  
Tecla **F** como toggle (si el subpanel de bodega no está abierto).

| Método | Descripción |
|---|---|
| `RefrescarPanel()` | Refresca todos los datos. Llamado externamente por `AstilleroUI`. |

---

## Módulo 8: Comportamiento de PNJs
**Estado:** ✅ Implementado (comerciantes y piratas con ciclo completo)  
**Dependencias:** Módulo Económico, Módulo de Mundo y Navegación, Módulo de Combate

### FlotaManager *(MonoBehaviour singleton)*
`Assets/Scripts/PNJ/FlotaManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `FlotaManager` | Punto de acceso global. |
| `MaxComerciantesActivos` | `const int = 20` | Límite de flotas comerciantes simultáneas. |
| `MaxPiratasActivos` | `const int = 3` | Límite de flotas piratas simultáneas. |
| `RegistrarFlota(FlotaRuntimeData)` | `void` | Registra en el diccionario activo. |
| `ObtenerFlota(int)` | `FlotaRuntimeData` | Busca por ID. |
| `ObtenerTodasLasFlotas()` | `IReadOnlyCollection<FlotaRuntimeData>` | Todas las flotas activas. |
| `LimpiarTodasLasFlotas()` | `void` | Vacía el registro (antes de cargar partida). |
| `EliminarFlota(int)` | `void` | Elimina flota, desactiva icono, borra de BD, activa respawn. |
| `SpawnFlotasPNJIniciales(IReadOnlyList<CiudadData>)` | `void` | Crea comerciantes y piratas al iniciar partida nueva. |
| `SpawnComercianteAleatorio()` | `void` | Crea comerciante con ID 1001–1999 y nombre único del pool. |
| `SpawnPirataAleatorio()` | `void` | Crea pirata con ID 2001–2999 en casilla marítima aleatoria. |
| `TickTodosLosControladores()` | `void` | Avanza todos los estados PNJ un tick. Llamado por `SimulacionTiempo.OnNuevoDia`. |
| `ContarFlotasEnRutaHacia(int, int)` | `int` | Flotas activas que van a una ciudad con un bien concreto. |
| `CambiarEstado(int, EstadoFlotaPNJ)` | `void` | Cambia el estado de una flota por ID. |
| `RegistrarPirataBrain(int, PirataBrain)` | `void` | Asocia un `PirataBrain` a una flota pirata. |
| `AsignarRutaCalculadorAPiratas(RutaCalculadorTilemap)` | `void` | Inyecta el calculador de rutas A* a los cerebros pirata activos. |

---

### FlotaRuntimeData *(clase pura C#)*
`Assets/Scripts/PNJ/FlotaRuntimeData.cs`

Estado completo de una flota PNJ en memoria. Gestionado por `FlotaManager`.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Coincide con `id_flota` en BD. |
| `NombrePropietario` | `string` | Nombre del propietario. |
| `IsPirata` | `bool` | `true` para flotas piratas. |
| `EstadoActual` | `EstadoFlotaPNJ` | Estado de la máquina de estados. |
| `CiudadOrigenId` / `CiudadDestinoId` | `int` | Ciudades de la ruta. `-1` si sin destino. |
| `RutaActual` | `List<int>` | Waypoints por ID de ciudad. |
| `Carga` | `Dictionary<int, int>` | Inventario (`id_bien → cantidad`). |
| `PosicionActual` | `Vector2` | Posición en coordenadas de mundo. |
| `BarcosFlota` | `List<BarcoJugador>` | Barcos individuales (relleno por `FlotaManager`). |
| `VidaMax` / `VidaActual` | `float` | Puntos de vida agregados de la flota. |
| `FuerzaCanhones` | `float` | Fuerza de combate total. |
| `VelocidadFlota` / `ManiobrabilidadFlota` | `float` | Stats de combate. |
| `NumBarcos` | `int` | Barcos operativos. |
| `Tripulacion` | `int` | Tripulantes activos totales. |
| `CargaMaximaTotal` | `int` | Suma de `CargaMaximaTotal` de `BarcosFlota`; estimación si vacío. |
| `AplicarDanio(float)` | `bool` | Reduce vida; devuelve `true` si la flota queda destruida. |
| `EstaDestruida()` | `bool` | `NumBarcos <= 0`. |
| `TieneCarga()` | `bool` | Algún bien con cantidad > 0. |
| `ResetearParaReabastecimiento()` | `void` | Restaura vida/tripulación/barcos. Llamado cada 7 días en piratas. |

**Enum:** `EstadoFlotaPNJ { EnPuerto, Viajando, Comerciando, Huyendo, Patrullando, Interceptando, HuyendoAPuerto, EsperandoEnPuerto }`

---

### ComerciantePNJController *(clase pura C#)*
`Assets/Scripts/PNJ/ComerciantePNJController.cs`

Máquina de estados del comerciante PNJ. Ciclo: EnPuerto → Viajando → Comerciando → EnPuerto.

- **EnPuerto:** Evalúa márgenes de ganancia en todas las ciudades disponibles (con precios desactualizados 7 días). Compra hasta llenar la bodega (`CargaMaximaTotal - cargaActual`). Inicia viaje A*.  
- **Viajando:** Descuenta `_diasRestantesViaje` cada tick. Al llegar transiciona a Comerciando.  
- **Comerciando:** Vende toda la carga al precio de destino.

---

### PirataBrain *(MonoBehaviour)*
`Assets/Scripts/PNJ/PirataBrain.cs`

Comportamiento asíncrono de pirata. Patrulla zonas marítimas, detecta flotas en radio de visión e inicia combate con comerciantes cercanos.

---

## Módulo 9: Tiempo y Simulación
**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos

### SimulacionTiempo *(MonoBehaviour singleton)*
`Assets/Scripts/Core/SimulacionTiempo.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `SimulacionTiempo` | Punto de acceso global. |
| `DiaActual` / `MesActual` / `AñoActual` | `int` | Fecha actual del juego. |
| `VelocidadActual` | `float` | Velocidad de simulación. |
| `EstaPausado` | `bool` | Indica si el tiempo está pausado. |
| `GetFechaFormateada()` | `string` | Fecha en formato "DD/MM/AAAA". |
| `GetVelocidadFormateada()` | `string` | Velocidad como texto ("0.25x", "1x"…). |
| `SubirVelocidad()` / `BajarVelocidad()` | `void` | Cicla entre 0.25x · 1x · 2x · 10x. |
| `TogglePausa()` | `void` | Alterna pausa. |
| `PausarPorMenu()` / `ReanudarDesdMenu()` | `void` | Pausa/reanuda desde menú de pausa. |
| `SetEstado(int, int, int, float)` | `void` | Restaura día/mes/año/velocidad (carga de partida). |

**Velocidades soportadas:** 0.25x · 1x · 2x · 10x + pausa  
**Eventos:**

| Evento | Tipo | Descripción |
|---|---|---|
| `OnNuevoDia` | `static event Action` | Dispara cada día de juego. |
| `OnNuevoMes` | `static event Action` | Dispara cada mes de juego. |
| `OnNuevaHora` | `static event Action` | 24 veces por día. Usado por `GestorCombatesActivos`. |
| `OnVelocidadCambiada` | `static event Action<float>` | Al cambiar velocidad o pausa. |

---

## Módulo 10: Interfaz de Usuario
**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos

### SceneController *(clase estática)*
`Assets/Scripts/Core/SceneController.cs`

| Método | Descripción |
|---|---|
| `IrAMenuPrincipal()` | Carga escena MenuPrincipal. |
| `IrAMapamundi()` | Carga escena Mapamundi. |
| `IrACiudad(string)` | Carga escena Ciudad con nombre. |
| `IrACiudad()` | Carga escena Ciudad de la ciudad actual en `GameManager`. |
| `IrAMercado()` | Carga escena Mercado. |
| `RecargarEscenaActual()` | Recarga la escena activa. |
| `SetPausa(bool)` | Activa/desactiva `Time.timeScale`. |
| `TogglePausa()` | Alterna pausa. |

---

### HUDDinero *(MonoBehaviour)*
`Assets/Scripts/UI/HUDDinero.cs`

Componente HUD presente en escenas Ciudad y Mapamundi. Se suscribe a `GameManager.OnDineroActualizado` en `OnEnable` y desuscribe en `OnDisable`/`OnDestroy`. Formato con separador de miles en español.

---

### HUDTiempo *(MonoBehaviour)*
`Assets/Scripts/UI/HUDTiempo.cs`

| Miembro | Descripción |
|---|---|
| `Instance` | Singleton ligero para la escena activa. |
| `ActualizarUI()` | Refresca texto de fecha y velocidad. |

---

### PanelInspeccionFlota *(MonoBehaviour)*
`Assets/Scripts/UI/PanelInspeccionFlota.cs`

Panel informativo de flota en el mapamundi.

| Método | Descripción |
|---|---|
| `Mostrar(FlotaRuntimeData, bool esJugador)` | Muestra panel. Si `esJugador`, activa botón Modo Pirata y título diferenciado. |
| `Ocultar()` | Desactiva el panel. |

---

### EncuentroNavalUI *(MonoBehaviour)*
`Assets/Scripts/Combate/EncuentroNavalUI.cs`

Panel de decisión al interceptar una flota enemiga. Opciones: Luchar / Huir.  
Al pulsar Luchar delega en `GestorCombatesActivos.IniciarCombate(esDelJugador: true)`.

---

### TabernaUI *(MonoBehaviour)*
`Assets/Scripts/Taberna/TabernaUI.cs`

Tres subpaneles: Menú · Contratar Marineros · Contratar Capitán.  
Botones +/- con aceleración en tres fases al mantener pulsado (1→5→10 unidades/paso).

| Método | Descripción |
|---|---|
| `AbrirTaberna()` | Abre panel y muestra menú principal. |
| `CerrarTaberna()` | Cierra panel y reactiva botón mapa. |
| `MostrarPanel(int)` | 0=Menú · 1=Marineros · 2=Capitán. |
| `RefrescarUI()` | Actualiza todos los textos y estados. |

---

## Módulo 11: Guardado y Carga
**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos

Persistencia completa en SQLite. Un fichero `.db` por slot (máx. 5). La tabla `estadoJuego` es independiente sin FKs.

### DatabaseManager *(MonoBehaviour singleton)*
`Assets/Scripts/Database/DatabaseManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `DatabaseManager` | Punto de acceso global. |
| `Conexion` | `SqliteConnection` | Conexión activa compartida por todos los DAOs. |
| `InicializarSlot(int)` | `void` | Abre (o crea) el fichero `.db`, ejecuta DDL y migraciones. |

---

### SaveManager *(MonoBehaviour)*
`Assets/Scripts/Database/SaveManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `SaveManager` | Punto de acceso global. |
| `GuardarPartida(int)` | `void` | Guarda estado completo en el slot indicado. |

Persistencia incluye: estado global · mercados · almacenes ciudad · flota jugador (barcos + módulos + tripulación) · capitanes · flotas PNJ (FlotaPNJ + CargaFlotaPNJ) · memoria comercial PNJ · modo pirata.

---

### LoadManager *(MonoBehaviour)*
`Assets/Scripts/Database/LoadManager.cs`

Restaura el estado completo desde el slot indicado, en orden topológico (tablas padre antes que hijo).  
Fallback por `TipoModulo` si el nombre del módulo no coincide (asset renombrado).

---

### DAOs activos

| DAO | Tabla(s) principal(es) | Descripción |
|---|---|---|
| `EstadoJuegoDAO` | `estadoJuego` | Estado global: fecha, velocidad, dinero, modo_pirata. |
| `CiudadDAO` | `Ciudad`, `EstadoMercadoCiudad`, `EdificiosCiudad` | Ciudades, mercados y edificios. |
| `BarcoDAO` | `Barco`, `TipoCasco`, `EstadoSeccionBarco`, `CargaBarco` | Barcos del jugador con módulos y secciones. |
| `FlotaDAO` | `Flota` | Flota del jugador y flotas PNJ en tabla `Flota`. |
| `FlotaPNJDAO` | `FlotaPNJ`, `CargaFlotaPNJ` | Flotas PNJ con bodega persistida. |
| `ModuloBarcoDAO` | `ModuloBarco` | Módulos instalados en cada barco. |
| `CapitanDAO` | `Capitan` | Capitanes y asignaciones. |
| `AlmacenCiudadDAO` | `AlmacenCiudadJugador` | Almacén del jugador por ciudad. |
| `MarketDAO` | `EstadoMercadoCiudad` | Lectura/escritura del mercado de cada ciudad. |
| `MemoriaComercialDAO` | `MemoriaComercialPNJ` | Precios conocidos por comerciantes PNJ. |

---

### Schema SQLite (tablas principales)

```
estadoJuego    — estado global de la partida (sin FK)
Ciudad         — ciudades del juego
Bien           — catálogo de bienes
AlmacenJugador — stock del jugador (global)
AlmacenCiudadJugador — stock del jugador por ciudad
EstadoMercadoCiudad  — stock, precio, producción, consumo por ciudad/bien
TipoEdificio / EdificiosCiudad — edificios de producción
Capitan        — capitanes disponibles y asignados
TipoCasco      — estadísticas base de cascos (4 tipos)
Flota          — flotas (jugador y PNJ referenciados por tipo_propietario)
Barco          — barcos con casco, vida, tripulación
ModuloBarco    — módulos instalados por barco
EstadoSeccionBarco — daños por sección (timón/velas/armamento/flotación)
CargaBarco     — mercancía en bodega de cada barco
FlotaPNJ       — flotas PNJ comerciantes/piratas
CargaFlotaPNJ  — bodega de flotas PNJ
MemoriaComercialPNJ — precios conocidos por los comerciantes
RecetaProduccion — recetas de cadenas de producción
```

---

## Módulo 12: Audio y Feedback Visual
**Estado:** ❌ Pendiente

> **TO-DO:**
> - Efectos sonoros contextuales (ciudad, combate, comercio, mapamundi)
> - Música ambiental diferenciada por escena
> - Indicadores visuales de daño en combate
> - Animaciones de impacto y destrucción de secciones
> - Marcadores de estado en mapamundi (encuentro, peligro, ruta activa)

---

## TO-DO — Funcionalidades pendientes por módulo

| Módulo | Pendiente | Prioridad |
|---|---|---|
| Producción y Cadenas | Gestor de producción diaria (`ProduccionCiudadManager`) — consume materias primas y genera intermedios/avanzados | Alta |
| Combate Naval | Tablero grid manual: movimiento por casillas, selección de sección a atacar (timón/velas/armamento/flotación) | Alta |
| Combate Naval | Fase de abordaje: combate de tripulación en cubierta, unidades por cada 5 marineros, armas estacionarias | Media |
| Construcción de Navíos | Desbloqueo progresivo de módulos con pólvora por fecha de calendario | Baja (parcial: `requierePolvora`) |
| Flotas y Tripulación | Disolución de flota si el barco del capitán es hundido; reorganización de barcos supervivientes | Media |
| Comportamiento PNJs | Estados `Interceptando` y `HuyendoAPuerto` completos para flotas comerciantes | Media |
| Interfaz de Usuario | Pantalla de creación de partida (selección de ciudad de inicio, fecha de inicio) | Media |
| Interfaz de Usuario | Menú de pausa completo (guardar, cargar, salir) desde todas las escenas | Alta |
| Audio y Feedback Visual | Todo (ver Módulo 12) | Baja |
