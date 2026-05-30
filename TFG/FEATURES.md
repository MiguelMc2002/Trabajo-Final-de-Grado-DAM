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

### MercadoUI *(MonoBehaviour)*
`Assets/Scripts/UI/MercadoUI.cs`

Panel modal del mercado en la escena Ciudad. Instancia una fila `MarketRowUI` por bien al activarse, refresca la cabecera con el nombre de ciudad y el uso de bodega, y destruye las filas al desactivarse para evitar acumulación. Se suscribe a `MarketManager.OnMercadoActualizado`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `BotonCerrar` | `Button` | Botón que cierra el panel delegando en `CiudadController.CerrarTodosPaneles()`. |
| `Cerrar()` | `void` | Cierra el panel del mercado. Registrado como listener de `BotonCerrar` en `Awake`. |

---

### MarketRowUI *(MonoBehaviour)*
`Assets/Scripts/UI/MarketRowUI.cs`

Fila de la pantalla de mercado. Muestra nombre del bien, precio, stocks de dos columnas ciclables (Mercado ciudad / Almacén ciudad / Bodega barco) e indicador de color reactivo al precio. Se suscribe a `MarketManager.OnMercadoActualizado` para actualizarse sin polling. Indicador: verde = precio ≤ base (vender aquí), amarillo = normal, rojo = precio > 2× base (comprar aquí).

| Miembro | Tipo | Descripción |
|---|---|---|
| `Inicializar(BienData bien, MarketManager marketManager, OficinaComercial oficina)` | `void` | Enlaza la fila con su bien y gestores, registra los listeners de los 6 botones (+1/+10/+100 comprar y vender) y pinta datos iniciales. Idempotente: limpia listeners previos antes de añadir nuevos. |

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

Panel de astillero con cinco subpaneles: Menú, Construir, Modificar, Reparar, Vender. Solo un subpanel es visible a la vez. El panel Construir muestra stats dinámicas (base del casco + deltas de módulos seleccionados en tiempo real).

| Miembro | Tipo | Descripción |
|---|---|---|
| `AbrirAstillero()` | `void` | Abre el panel raíz y muestra el subpanel Menú. Inicializa el selector de cascos en el índice 0. |
| `CerrarAstillero()` | `void` | Cierra el panel y reactiva el botón de mapa llamando a `CiudadController.ReactivarBotonMapa()`. |
| `MostrarPanel(int indice)` | `void` | Activa el subpanel indicado y oculta el resto. `0`=Menú · `1`=Construir · `2`=Modificar · `3`=Reparar · `4`=Vender. Refresca la UI del subpanel activado. |

---

### TipoCascoData *(ScriptableObject)*
`Assets/Scripts/Astillero/TipoCascoData.cs`

Implementación concreta de `IBarco` como ScriptableObject editable desde el Inspector. Es el ConcreteComponent del patrón Decorator: las subclases `CascoDecorador` lo envuelven, aunque en la práctica los decoradores concretos sobreescriben todos los valores con stats hardcoded.

| Campo | Tipo | Descripción |
|---|---|---|
| `idTipoCasco` | `int` | Identificador único del tipo de casco. |
| `nombreCasco` | `string` | Nombre visible (ej: "Cog"). |
| `vidaBase` / `velocidadBase` / `maniobrabilidadBase` / `capacidadCargaBase` | `int` | Stats base del casco. |
| `capacidadModulos` / `capacidadTripulacion` | `int` | Slots de módulos y tripulantes máximos. |
| `costeMadera` / `costeHierro` / `costeHerramientas` / `costeOro` | `int` | Coste de construcción por recurso. |
| `iconoCasco` | `Sprite` | Icono del casco en la interfaz del astillero. |

---

### CascoDecorador *(ScriptableObject abstracto)*
`Assets/Scripts/Astillero/CascoDecorador.cs`

Decorator abstracto que envuelve un `TipoCascoData` e implementa `IBarco` delegando en él. Las subclases concretas (`CascoCog`, `CascoHulk`, `CascoCarraca`, `CascoGalera`) sobreescriben todas las propiedades con stats hardcoded en lugar de modificar los del `_cascoBase`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `_cascoBase` | `TipoCascoData` | Casco envuelto. `protected`, asignable desde el Inspector de la subclase. |
| Propiedades `IBarco` | `int` | Todas delegadas en `_cascoBase` como implementación base; las subclases las sobreescriben. |

**Subclases concretas:** `CascoCog` (id=1, vida=100, vel=3) · `CascoHulk` (id=2, vida=150, vel=2) · `CascoCarraca` (id=3, vida=200, vel=2) · `CascoGalera` (id=4, vida=80, vel=5)

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

### EdificioClickable *(MonoBehaviour)*
`Assets/Scripts/Ciudades/EdificioClickable.cs`

Botón de edificio en la pantalla de ciudad. Al pulsarlo notifica a `CiudadController.AbrirEdificio(Tipo)` para abrir el panel correspondiente.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Tipo` | `TipoEdificio` | Tipo de edificio que representa este botón (Mercado, Astillero, Taberna, Puerto). |
| `OnClick()` | `void` | Abre el panel del edificio. Asignar al evento OnClick del Button en el Inspector. |

---

### PanelAstilleroUI *(MonoBehaviour — stub beta)*
`Assets/Scripts/Ciudades/PanelAstilleroUI.cs`

Panel de astillero de la escena Ciudad (stub de la beta). En la versión release quedó sustituido por `AstilleroUI`. Actualmente solo expone el botón de cierre.

| Miembro | Tipo | Descripción |
|---|---|---|
| `BotonCerrar` | `Button` | Botón que cierra el panel delegando en `CiudadController.CerrarTodosPaneles()`. |
| `Cerrar()` | `void` | Cierra el panel. Registrado como listener de `BotonCerrar` en `Awake`. |

---

### PanelTabernaUI *(MonoBehaviour — stub beta)*
`Assets/Scripts/Ciudades/PanelTabernaUI.cs`

Panel de taberna de la escena Ciudad (stub de la beta). En la versión release quedó sustituido por `TabernaUI`. Actualmente solo expone el botón de cierre.

| Miembro | Tipo | Descripción |
|---|---|---|
| `BotonCerrar` | `Button` | Botón que cierra el panel delegando en `CiudadController.CerrarTodosPaneles()`. |
| `Cerrar()` | `void` | Cierra el panel. Registrado como listener de `BotonCerrar` en `Awake`. |

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

### MapamundiCamara *(MonoBehaviour)*
`Assets/Scripts/Mapamundi/MapamundiCamara.cs`

Controla la cámara de la escena Mapamundi: zoom con rueda del ratón, desplazamiento por bordes de pantalla y WASD, y arrastre con clic. Gestiona click derecho (navegar o iniciar persecución en modo pirata) y click izquierdo sobre flotas (abrir panel de inspección). Adjuntar a la Main Camera de la escena Mapamundi.

| Miembro | Tipo | Descripción |
|---|---|---|
| `MinZoom` / `MaxZoom` | `float` | Límites del tamaño ortográfico de la cámara (3–15 por defecto). |
| `VelocidadZoom` | `float` | Factor de zoom por unidad de rueda del ratón. |
| `ZonaBorde` | `float` | Píxeles de margen de pantalla que activan el scroll automático. |
| `VelocidadScroll` | `float` | Velocidad de desplazamiento por bordes. |
| `VelocidadWASD` | `float` | Velocidad de desplazamiento con teclado. |
| `LimiteIzquierdo` / `LimiteDerecho` / `LimiteInferior` / `LimiteSuperior` | `float` | Bordes del mapa que impiden que la cámara salga del área jugable. |

---

### MarcadorCiudad *(MonoBehaviour)*
`Assets/Scripts/Navegacion/MarcadorCiudad.cs`

Sprite interactuable adjunto a cada ciudad en el tilemap. Escala al 120 % al hacer hover. Al hacer clic delega en `NavegacionJugadorController.SolicitarEntradaCiudad`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `DatosCiudad` | `CiudadData` | Ciudad representada por este marcador. Asignable desde el Inspector. |
| `TextoNombre` | `TextMeshPro` | Etiqueta opcional con el nombre de la ciudad sobre el marcador. |
| `Inicializar(MapamundiController controlador)` | `void` | Enlaza el marcador con el controlador y escribe el nombre de ciudad en la etiqueta. Llamado por `MapamundiController.Start()`. |

---

### NavegacionJugadorController *(MonoBehaviour singleton)*
`Assets/Scripts/Jugador/NavegacionJugadorController.cs`

Gestiona toda la navegación del jugador en el mapamundi. Click derecho calcula ruta A* y mueve la flota; un nuevo click cancela la anterior (redirección). Al cruzar una casilla de ciudad detiene la flota y muestra `PopUpEntradaCiudad`. En modo pirata gestiona persecución activa con recálculo de ruta cada 0,5 s via coroutine.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `NavegacionJugadorController` | Punto de acceso global. Sin `DontDestroyOnLoad`: vive solo en la escena Mapamundi. |
| `CancelarPersecucionYNavegar(Vector2 posicionPantalla)` | `void` | Cancela persecución activa e inicia navegación hacia la casilla clickada. Llamado por `MapamundiCamara` al recibir click derecho. |
| `SolicitarEntradaCiudad(CiudadData ciudad)` | `void` | Si la flota ya está en la casilla de la ciudad muestra el pop-up; si no, navega hasta allí primero. |
| `MarcarSalidaDeCiudad()` | `void` | Indica que el jugador acaba de rechazar entrar a una ciudad. Evita que el pop-up se re-dispare en la misma casilla. |
| `IniciarPersecucion(FlotaRuntimeData objetivo)` | `void` | Inicia coroutine de persecución en modo pirata. Cancela cualquier persecución previa y dispara combate al alcanzar al objetivo. |

---

### CamaraFija *(MonoBehaviour)*
`Assets/Scripts/Core/CamaraFija.cs`

Fija el tamaño ortográfico de la cámara para impedir zoom accidental. Reaplica el valor cada frame. Sin API pública relevante: el tamaño se asigna desde el Inspector vía el campo serializado `_size`.

| Miembro | Tipo | Descripción |
|---|---|---|
| *(sin miembros públicos)* | — | Configuración exclusiva por Inspector (`_size: float`, defecto `0.71f`). |

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

Panel lateral en la escena Ciudad que muestra los datos del barco seleccionado. Navegación circular entre barcos con flechas. Tecla **F** como toggle (ignorada si el subpanel de bodega está abierto). Incluye subpanel de bodega con inventario del jugador generado dinámicamente.

| Miembro | Tipo | Descripción |
|---|---|---|
| `AbrirPanel()` | `void` | Cierra los demás paneles de ciudad, auto-selecciona el primer barco si ninguno estaba seleccionado y muestra el panel. |
| `MostrarBarco(BarcoJugador barco)` | `void` | Abre el panel con el barco indicado ya seleccionado. |
| `OcultarPanel()` | `void` | Desactiva el panel y restaura el botón de mapa. |
| `RefrescarPanel()` | `void` | Refresca todos los datos tras una operación del astillero. Si el barco fue vendido, cambia al primer barco disponible. |
| `RefrescarUI()` | `void` | Actualiza todos los textos con los datos del barco seleccionado. Si la flota está vacía, muestra el estado vacío y desactiva los botones. |

---

### ConvoyData *(clase pura C#)*
`Assets/Scripts/Taberna/ConvoyData.cs`

Agrupa hasta 5 barcos del jugador en una formación navegable. El barco líder no puede abandonar la formación. Calcula stats agregados (velocidad mínima, carga máxima, fuerza de combate) y puede exportarse como `FlotaRuntimeData` para el sistema de combate.

| Miembro | Tipo | Descripción |
|---|---|---|
| `NombreConvoy` | `string` | Nombre del convoy, igual al nombre del barco líder. |
| `BarcoLider` | `BarcoJugador` | Barco que encabeza la formación. No puede eliminarse. |
| `Miembros` | `IReadOnlyList<BarcoJugador>` | Barcos del convoy, incluyendo al líder. |
| `ModoPirata` | `bool` | Si `true`, el convoy actúa como pirata en el mapa. |
| `TripulacionTotal` | `int` | Suma de tripulantes de todos los barcos. |
| `VelocidadConvoy` | `float` | Velocidad del barco más lento (cuello de botella de la formación). |
| `FuerzaCombateTotal` | `int` | Suma de fuerzas de combate de todos los barcos. |
| `CargaMaximaTotal` | `int` | Suma de capacidades de carga máxima. |
| `ConvoyData(BarcoJugador barcoLider)` | constructor | Crea el convoy con el barco indicado como líder. Lo añade automáticamente a `Miembros`. |
| `AñadirMiembro(BarcoJugador barco)` | `bool` | Incorpora un barco si no supera los 5 miembros y no es duplicado. |
| `EliminarMiembro(BarcoJugador barco)` | `bool` | Elimina un barco; devuelve `false` si era el líder o no pertenecía. |
| `ComoFlotaRuntime()` | `FlotaRuntimeData` | Snapshot del convoy para el sistema de combate naval. |

---

### ConvoyManager *(MonoBehaviour singleton)*
`Assets/Scripts/Taberna/ConvoyManager.cs`

Gestiona todos los convoyes activos del jugador. Persiste entre escenas con `DontDestroyOnLoad`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `ConvoyManager` | Punto de acceso global. |
| `ConvoysActivos` | `IReadOnlyList<ConvoyData>` | Convoyes activos en este momento. |
| `CrearConvoy(BarcoJugador barcoLider)` | `ConvoyData` | Crea convoy para el barco dado. Si ya lidera uno, devuelve el existente. |
| `UnirseAConvoy(ConvoyData convoy, BarcoJugador barco)` | `bool` | Añade el barco al convoy. Falla si ya es miembro o el convoy está lleno (5). |
| `AbandonarConvoy(ConvoyData convoy, BarcoJugador barco)` | `bool` | Saca el barco. Si el convoy queda vacío lo disuelve automáticamente. |
| `GetConvoyDeBarco(BarcoJugador barco)` | `ConvoyData` | Devuelve el convoy al que pertenece el barco; `null` si ninguno. |
| `DisolverConvoy(ConvoyData convoy)` | `void` | Elimina el convoy de la lista de activos. |

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

Máquina de estados del comerciante PNJ. Ciclo: `EnPuerto → Viajando → Comerciando → EnPuerto`.
Instanciada por `FlotaManager` para cada flota comerciante. Avanza un paso por día de juego vía `Tick()`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `ComerciantePNJController(FlotaRuntimeData, FlotaManager)` | constructor | Inicializa el controlador vinculando la flota y el gestor de flotas. |
| `Tick()` | `void` | Avanza la máquina de estados un día de juego. Delega en el método privado del estado actual. |
| `IniciarViaje(int ciudadDestinoId, Dictionary<int,double> preciosCompra, int diasViaje)` | `void` | Fija el destino, registra los precios de compra y transiciona a `Viajando`. |

---

### PirataBrain *(clase pura C#)*
`Assets/Scripts/PNJ/PirataBrain.cs`

Cerebro asíncrono de una flota pirata. Ejecuta dos `Task` en background: `BucleDeteccion` (detecta comerciantes en radio y decide interceptar o patrullar) y `BucleNavegacion` (calcula rutas A* hexagonales sin bloquear el hilo principal). El hilo principal envía snapshots de posición y consume `ColaSalida` en `Update`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `PirataBrain(int flotaId, float radioDeteccion, Dictionary<Vector3Int, List<Vector3Int>> grafo, HashSet<Vector3Int> casillasCiudad)` | constructor | Crea el brain. No inicia los Tasks hasta llamar a `IniciarTasks()`. |
| `ColaSalida` | `ConcurrentQueue<ComandoPirata>` | Cola de comandos (`NuevaRuta`, `InterceptarObjetivo`, `Patrullar`) consumida en `FlotaIconoMapamundi.Update()`, máx. 1 por frame. |
| `IniciarTasks()` | `void` | Lanza `BucleDeteccion` y `BucleNavegacion` con un `CancellationToken` compartido. Llamar desde el hilo principal tras construir el brain. |
| `Detener()` | `void` | Cancela los dos Tasks. Llamar desde `OnDestroy` de `PirataBrainBootstrapper`. |
| `EnviarSnapshot(FlotaSnapshot[])` | `void` | Encola un array de posiciones de todas las flotas para que `BucleDeteccion` lo procese. Descarta entradas antiguas si la cola supera 3 elementos. |
| `ActualizarPosPropia(Vector2 posicion, Vector3Int casilla)` | `void` | Actualiza la posición propia del pirata en el brain. Llamar desde `Update` en el hilo principal. |

---

### PirataPNJController *(clase pura C#)*
`Assets/Scripts/PNJ/PirataPNJController.cs`

Máquina de estados del pirata PNJ. Gestiona únicamente el estado `Huyendo` post-combate (cooldown de 2 días). La detección de presas y la navegación las delega en `PirataBrain`. Instanciada por `FlotaManager` al registrar una flota con `IsPirata = true`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `PirataPNJController(FlotaRuntimeData flota, FlotaManager manager, RutaCalculadorTilemap rutaCalculador)` | constructor | Crea el controlador vinculando la flota, el gestor y el calculador A*. `rutaCalculador` puede ser `null` si la escena Mapamundi no está cargada. |
| `Tick()` | `void` | Avanza un día de juego. Solo actúa en estado `Huyendo`: calcula ruta al waypoint de huida más cercano, decrementa contador y transiciona a `Patrullando` tras 2 días. |
| `AsignarRutaCalculador(RutaCalculadorTilemap rutaCalculador)` | `void` | Reasigna el calculador A*. Llamar desde `MapamundiController.Start()` tras cargar la escena. |

---

### PirataBrainBootstrapper *(MonoBehaviour singleton)*
`Assets/Scripts/PNJ/PirataBrainBootstrapper.cs`

Construye el grafo de navegación en el hilo principal (donde `Tilemap.GetSprite` es seguro) y arranca un `PirataBrain` por cada flota pirata registrada. Espera un frame tras `Start` para que `FlotaManager` haya registrado todas las flotas.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `PirataBrainBootstrapper` | Punto de acceso global. |
| `CrearBrainParaPirata(FlotaRuntimeData flota)` | `void` | Crea y registra un `PirataBrain` para una flota pirata spawneada en runtime, reutilizando el grafo ya construido. No hace nada si el grafo aún no está disponible. |

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

### EstadoPartida *(clase serializable C#)*
`Assets/Scripts/Core/EstadoPartida.cs`

Contenedor serializable del estado completo de una partida en curso. `GameManager` es su único propietario y lo expone a través de métodos controlados. Agrupa mercados, flotas, barcos, edificios y memoria comercial en colecciones indexadas por ID.

| Miembro | Tipo | Descripción |
|---|---|---|
| `DiaJuego` | `int` | Día actual de la simulación. Incrementado en cada tick diario. |
| `MercadosPorCiudad` | `Dictionary<int, List<EntradaMercado>>` | Estado de los mercados de todas las ciudades, indexado por `IdCiudad`. |
| `FlotasPorId` | `Dictionary<int, FlotaRuntimeData>` | Estado de cada flota PNJ y del jugador, indexado por `id_flota`. |
| `BarcosPorId` | `Dictionary<int, object>` | Estado de cada barco por `id_barco`. *(tipo concreto pendiente de definir en release)* |
| `EdificiosPorCiudad` | `Dictionary<int, List<object>>` | Edificios activos por ciudad. *(tipo pendiente)* |
| `MemoriaComercialPorFlota` | `Dictionary<int, object>` | Memoria comercial de cada flota PNJ. *(tipo pendiente)* |

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

Muestra el dinero del jugador en el HUD. Se actualiza reactivamente mediante `GameManager.OnDineroActualizado`; nunca hace polling en `Update`. Se desuscribe automáticamente en `OnDisable` y `OnDestroy`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `FormatearDinero(long cantidad)` | `static string` | Formatea una cantidad con separador de miles (es-ES) y símbolo ƒ. Ejemplo: `999999` → `"999.999 ƒ"`. Pública para reutilizarse en otros paneles. |

---

### HUDTiempo *(MonoBehaviour singleton)*
`Assets/Scripts/UI/HUDTiempo.cs`

HUD persistente (`DontDestroyOnLoad`) que muestra la fecha y la velocidad de simulación. Se auto-destruye si detecta un duplicado al cargar una escena. Se oculta automáticamente en escenas no jugables mediante `SceneManager.sceneLoaded`. Los botones +/- son acceso alternativo al ratón; el teclado lo gestiona `SimulacionTiempo`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `HUDTiempo` | Punto de acceso global al HUD de tiempo persistente. |
| `ActualizarUI()` | `void` | Refresca los textos de fecha y velocidad, y el estado `interactable` de los botones +/-. Se llama al inicio y cada vez que cambia la fecha o la velocidad. |

---

### PanelInspeccionFlota *(MonoBehaviour)*
`Assets/Scripts/UI/PanelInspeccionFlota.cs`

Panel informativo de flota en el mapamundi. Genera una fila por barco con nombre, casco, vida, velocidad, maniobrabilidad, carga y fuerza de combate. Para flotas PNJ sin barcos individuales muestra stats agregados. Para el jugador activa el botón Modo Pirata.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Mostrar(FlotaRuntimeData flota, bool esJugador)` | `void` | Muestra el panel con los datos de la flota. Si `esJugador == true`, el título indica "Tu flota" y se activa el botón Modo Pirata. |
| `Ocultar()` | `void` | Desactiva el `GameObject` del panel. |

---

### EncuentroNavalUI *(MonoBehaviour)*
`Assets/Scripts/Combate/EncuentroNavalUI.cs`

Panel de decisión al interceptar una flota enemiga. Opciones: Luchar / Huir. Componente puramente reactivo: no expone métodos públicos; se activa mediante `CombateEventos.OnCombateIniciado`.

| Miembro | Tipo | Descripción |
|---|---|---|
| *(suscripción a `CombateEventos.OnCombateIniciado`)* | `event` | Activa el panel cuando el jugador entra en un encuentro naval. |
| Botón **Luchar** | `Button` | Delega en `GestorCombatesActivos.IniciarCombate(esDelJugador: true)` y oculta el panel. |
| Botón **Huir** | `Button` | Compara velocidad del jugador con la del pirata; si es mayor, escapa sin combate; si no, inicia combate igualmente. |

---

### TabernaUI *(MonoBehaviour)*
`Assets/Scripts/Taberna/TabernaUI.cs`

Tres subpaneles: Menú · Contratar Marineros · Contratar Capitán.  
Botones +/- con aceleración en tres fases al mantener pulsado (1→5→10 unidades/paso).

| Miembro | Tipo | Descripción |
|---|---|---|
| `AbrirTaberna()` | `void` | Abre el panel y muestra el subpanel de menú principal. |
| `CerrarTaberna()` | `void` | Cierra el panel y reactiva el botón de regreso al mapa. |
| `MostrarPanel(int indice)` | `void` | Activa el subpanel indicado: 0=Menú · 1=Marineros · 2=Capitán. |
| `RefrescarUI()` | `void` | Actualiza todos los textos, costes y estados de botones según el estado actual del juego. |

---

### MenuPrincipalUI *(MonoBehaviour)*
`Assets/Scripts/UI/MenuPrincipalUI.cs`

Controla los paneles del menú principal (menú raíz / pantalla de slots). Gestiona la visibilidad excluyente entre el menú de botones y la pantalla de slots y delega en `PantallaSlotsUI` para cargar partidas. Tecla Escape cierra el panel de selección de ciudad si está abierto.

| Miembro | Tipo | Descripción |
|---|---|---|
| `panelSeleccionCiudad` | `GameObject` | Panel con los botones de ciudad para iniciar una nueva partida. |
| `IniciarNuevaPartida()` | `void` | Muestra el panel de selección de ciudad. Asignar al botón "Nueva Partida". |
| `CerrarPanelSeleccion()` | `void` | Oculta el panel de selección y vuelve al menú raíz. Activado también por Escape. |
| `CargarPartida()` | `void` | Abre la pantalla de slots en modo Cargar. Asignar al botón "Cargar Partida". |
| `MostrarMenuPrincipal()` | `void` | Reactiva el menú raíz. Llamado por `PantallaSlotsUI` al cerrarse. |
| `Salir()` | `void` | Cierra la aplicación (`Application.Quit()`). |

---

### MenuPausa *(MonoBehaviour)*
`Assets/Scripts/UI/MenuPausa.cs`

Menú de pausa in-game. La tecla Escape alterna visibilidad del panel. Delega en `SimulacionTiempo.PausarPorMenu` / `ReanudarDesdMenu`; en escenas sin simulación activa no toca el tiempo. Accede a `PantallaSlotsUI` para guardar y cargar partidas desde pausa.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Continuar()` | `void` | Cierra el panel y reanuda la simulación. Asignar al botón "Continuar". |
| `IrAMenuPrincipal()` | `void` | Reanuda la simulación y carga la escena MenuPrincipal, abandonando la partida. |
| `SalirAlEscritorio()` | `void` | Cierra la aplicación (detiene el Play en editor). |

---

### PopUpEntradaCiudad *(MonoBehaviour)*
`Assets/Scripts/UI/PopUpEntradaCiudad.cs`

Modal que aparece cuando la flota del jugador llega a una casilla de ciudad. Pausa la simulación mientras el jugador decide. Opción Entrar establece la ciudad en `GameManager` y carga la escena Ciudad. Opción Continuar cierra el panel y llama a `NavegacionJugadorController.MarcarSalidaDeCiudad` para evitar re-disparo inmediato.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Mostrar(CiudadData ciudad)` | `void` | Activa el panel con el nombre de la ciudad y pausa la simulación. Llamado por `NavegacionJugadorController`. |
| `Ocultar()` | `void` | Desactiva el panel y reanuda la simulación. |

---

### SeleccionCiudadUI *(MonoBehaviour)*
`Assets/Scripts/UI/SeleccionCiudadUI.cs`

Botón de ciudad en el panel de nueva partida del menú principal. Al pulsarlo inicializa el slot 0 de la base de datos, carga todos los mercados desde assets y navega a la escena Ciudad.

| Miembro | Tipo | Descripción |
|---|---|---|
| `datosCiudad` | `CiudadData` | Ciudad asociada a este botón. Asignable desde el Inspector. |
| `SeleccionarCiudad()` | `void` | Inicializa el slot 0, carga mercados desde assets y navega a la escena Ciudad. Asignar al evento OnClick del Button. |

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

### LoadManager *(MonoBehaviour singleton)*
`Assets/Scripts/Database/LoadManager.cs`

Restaura el estado completo en 9 pasos ordenados (tiempo → dinero → almacenes → mercados → flotas PNJ → flota jugador → capitanes). Usa fallback por `TipoModulo` si el nombre del módulo no coincide con el asset guardado.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `LoadManager` | Punto de acceso global. Persiste entre escenas con `DontDestroyOnLoad`. |
| `CargarPartida(int slotIndex)` | `void` | Carga el estado completo desde el slot indicado (1–5). Abre el `.db`, instancia los DAOs y restaura cada subsistema en orden topológico. |

---

### PantallaSlotsUI *(MonoBehaviour)*
`Assets/Scripts/Database/PantallaSlotsUI.cs`

Panel completo de selección de slots de guardado y carga. Al abrirse escanea los cinco archivos `slot_N.db` en disco mediante una conexión SQLite temporal de solo lectura y rellena cada `SlotUI`. Incluye panel de confirmación modal para sobrescritura y borrado.

**Enum:** `SlotModo { Guardar, Cargar }` — determina qué acción y qué botones muestra cada fila.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Abrir(SlotModo modo)` | `void` | Abre el panel en el modo indicado y escanea los archivos de partida en disco. |
| `OnGuardar(SlotUI slotUI)` | `void` | Guarda en el slot. Si está ocupado muestra confirmación de sobrescritura antes de proceder. |
| `OnCargar(SlotUI slotUI)` | `void` | Carga la partida del slot y navega al mapamundi. |
| `OnBorrar(SlotUI slotUI)` | `void` | Elimina el archivo `slot_N.db` tras confirmación. |
| `CerrarPanel()` | `void` | Cierra el panel y notifica a `MenuPrincipalUI.MostrarMenuPrincipal()` si procede. |

---

### SlotData *(clase pura C#)*
`Assets/Scripts/Database/SlotData.cs`

Metadatos de un slot de guardado. Instanciado por `PantallaSlotsUI` al escanear los archivos de partida. Contiene solo lo que se muestra en la fila de la interfaz.

| Miembro | Tipo | Descripción |
|---|---|---|
| `NumeroSlot` | `int` | Número de slot (1–5). Determina el nombre del archivo: `slot_N.db`. |
| `EstaOcupado` | `bool` | `true` si el archivo `.db` existe y contiene una partida guardada. |
| `NombrePartida` | `string` | Nombre visible del slot (p. ej. `"Partida 2"`). |
| `FechaGuardado` | `string` | Fecha y hora del último guardado (`"dd/MM/yyyy HH:mm"`). Vacío si no ocupado. |
| `DiasJugados` | `int` | Días de juego transcurridos desde el inicio, leídos de `estadoJuego`. Cero si no ocupado. |

---

### SlotUI *(MonoBehaviour)*
`Assets/Scripts/Database/SlotUI.cs`

Prefab de fila de slot. Rellena textos, aplica colores y activa o desactiva los botones según el modo del panel (Guardar → solo botón Guardar; Cargar → botones Cargar y Borrar si está ocupado).

| Miembro | Tipo | Descripción |
|---|---|---|
| `Datos` | `SlotData` | Metadatos del slot que representa esta fila. Accesible desde `PantallaSlotsUI` para identificar sobre qué slot actuar. |
| `Inicializar(SlotData datos, SlotModo modo, PantallaSlotsUI pantalla)` | `void` | Rellena textos, colores y botones, y registra listeners que delegan en `pantalla`. Limpia listeners previos para evitar duplicados al reutilizar el prefab. |

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
