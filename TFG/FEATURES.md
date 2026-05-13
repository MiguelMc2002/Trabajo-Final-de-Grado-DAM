# FEATURES — Videojuego de estrategia marítima medieval

Documentación de la API pública del proyecto.  
Actualizar este fichero cada vez que se añada o modifique una clase con miembros públicos.

---

## GameManager

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Core/GameManager.cs` |
| **Tipo** | `MonoBehaviour` (singleton persistente) |
| **Módulo** | Core — Transversal |
| **Descripción** | Registro central de la partida. Conserva el estado del comerciante —tesoro, ciudad actual, última ciudad visitada y bodega— y desde el Día 12 es el dueño exclusivo del estado vivo de todos los mercados del mundo (via `EstadoPartida`). `MarketManager` ya no posee el estado: lo lee y escribe a través de `GameManager`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static GameManager` (get) | Punto de acceso global al estado de la partida activa. |
| `Dinero` | `long` (get) | Monedas de oro en el cofre del comerciante. Sube al vender y baja al comprar. |
| `CiudadActual` | `CiudadData` (get) | Puerto en el que está atracado el jugador. `null` mientras navega por el mapamundi. |
| `UltimaCiudad` | `CiudadData` (get) | Puerto visitado antes del destino actual. `null` si el jugador no ha viajado todavía. Útil para ofrecer volver al origen. |
| `CapacidadAlmacen` | `const int` | Capacidad de bodega en la beta: `int.MaxValue`. En la release se sustituirá por la capacidad real del barco. |
| `CiudadesDisponibles` | `IReadOnlyList<CiudadData>` (get) | Catálogo de todas las ciudades del juego registradas en `EstadoPartida`. Lo puebla `InicializarMercadosDesdeAssets`. |
| `MercadosPorCiudad` | `IReadOnlyDictionary<int, List<EntradaMercado>>` (get) | Estado vivo de los mercados de todas las ciudades, indexado por `IdCiudad`. Propiedad de `EstadoPartida`; `MarketManager` opera sobre él. |
| `EstablecerCiudadActual(CiudadData ciudad)` | `void` | Registra el puerto de destino. Guarda el valor anterior en `UltimaCiudad` antes de sobrescribir `CiudadActual`. Invocado desde `MapamundiController`. |
| `ModificarDinero(long cantidad)` | `bool` | Registra un movimiento de dinero. Positivo al cobrar una venta, negativo al pagar una compra. Devuelve `false` si el tesoro no cubre el gasto. |
| `GetCantidadBien(BienData bien)` | `int` | Devuelve las unidades del bien indicado en bodega. Retorna 0 si no está en el inventario. |
| `ModificarCantidadBien(BienData bien, int cantidad)` | `bool` | Modifica la cantidad de un bien en bodega. Devuelve `false` si el resultado sería negativo o superaría `CapacidadAlmacen`. |
| `GetTotalUnidadesAlmacen()` | `int` | Devuelve el total de unidades de todas las mercancías en bodega. |
| `GetAlmacen()` | `IReadOnlyDictionary<BienData, int>` | Expone el inventario completo de bodega en modo solo lectura. |
| `TieneMercado(int idCiudad)` | `bool` | Devuelve `true` si `MercadosPorCiudad` contiene una entrada para la ciudad indicada. |
| `GetEntradasMercado(int idCiudad)` | `List<EntradaMercado>` | Devuelve la lista de entradas del mercado de la ciudad. Lanza excepción si la ciudad no está registrada; usar `TieneMercado` antes. |
| `RegistrarMercadoCiudad(int idCiudad, List<EntradaMercado> entradas)` | `void` | Añade o sobrescribe el mercado de una ciudad en el diccionario. Invocado por `LoadManager` al restaurar partida y por `InicializarMercadosDesdeAssets` al arrancar. |
| `LimpiarMercados()` | `void` | Vacía `MercadosPorCiudad` completamente. Invocado por `LoadManager` antes de repoblar desde BD. |
| `InicializarMercadosDesdeAssets(IEnumerable<CiudadData> ciudades)` | `void` | Recorre el catálogo de ciudades, hace una copia profunda de cada `EntradaMercado` y la registra en el diccionario. Garantiza que todas las ciudades estén en memoria antes de entrar en partida. Invocado desde `SeleccionCiudadUI.SeleccionarCiudad()`. |
| `NotificarMercadoActualizado(int idCiudad, BienData bien = null)` | `void` | Dispara `OnMercadoCiudadActualizado` con la ciudad y el bien afectados. `MarketManager` lo llama tras cada compra, venta o tick diario. |
| `OnMercadoCiudadActualizado` | `event Action<int, BienData>` | Se lanza cuando cualquier mercado del mundo cambia. `MarketManager` se suscribe para refrescar su vista de la ciudad activa. |

### Dependencias

- `BienData` — clave del diccionario de bodega.
- `CiudadData` — tipo de `CiudadActual`, `UltimaCiudad` y catálogo de ciudades.
- `EstadoPartida` — POCO que agrupa todos los diccionarios del estado vivo del mundo.
- `EntradaMercado` — estado dinámico de cada bien en cada ciudad.

---

## SceneController

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Core/SceneController.cs` |
| **Tipo** | `MonoBehaviour` (métodos estáticos) |
| **Módulo** | Core — Interfaz de usuario / Navegación |
| **Descripción** | Gestiona todos los cambios de pantalla del juego. Centraliza los literales de nombre de escena para evitar typos. Flujo beta: Menú Principal → Mapamundi → Ciudad → Mercado → (vuelta al mapa). |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `IrAMenuPrincipal()` | `static void` | Lleva al jugador al Menú Principal, abandonando la partida en curso. |
| `IrAMapamundi()` | `static void` | Muestra el mapamundi para que el jugador elija su próximo destino. |
| `IrACiudad(string nombreCiudad)` | `static void` | Abre la pantalla de ciudad registrando el nombre en el log. La ciudad se establece previamente en `GameManager` desde `MapamundiController`. |
| `IrACiudad()` | `static void` | Carga la pantalla de ciudad sin modificar `GameManager.CiudadActual`. Sobrecarga usada cuando la ciudad ya está registrada. |
| `IrAMercado()` | `static void` | Abre el mercado del puerto actual. Solo válido si el jugador está dentro de una ciudad. |
| `RecargarEscenaActual()` | `static void` | Reinicia la pantalla actual a su estado inicial sin abandonar la partida. |
| `SetPausa(bool pausado)` | `static void` | Detiene o reanuda el tiempo del juego. |
| `TogglePausa()` | `static void` | Alterna entre pausa y juego activo con una sola llamada. |

---

## BienData

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Economico/BienData.cs` |
| **Tipo** | `ScriptableObject` |
| **Módulo** | Económico — Bienes y mercado |
| **Descripción** | Define las propiedades estáticas de un bien comerciable en los mercados de la Liga Hanseática. Actúa como plantilla inmutable; el estado dinámico —stock y precio actual— vive en `MarketManager`. Crear instancias desde el menú: Assets → Create → TFG → Bien Comercial. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `nombre` | `string` | Nombre visible del bien en la interfaz del mercado y del almacén (p. ej. "Grano", "Madera", "Pescado"). |
| `categoria` | `CategoriaBien` | Clasificación del bien en la cadena productiva: `Primario`, `Intermedio` o `Avanzado`. |
| `precioBase` | `float` | Precio de referencia cuando el stock está al máximo. Base de la fórmula dinámica de precios. |
| `stockMaximo` | `int` | Unidades máximas que puede almacenar una ciudad de este bien. Determina el rango dinámico del precio. |

### Dependencias

- `CategoriaBien` (enum definido en el mismo fichero).

---

## CategoriaBien

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Economico/BienData.cs` |
| **Tipo** | `enum` |
| **Módulo** | Económico — Bienes y mercado |
| **Descripción** | Clasificación de un bien según su posición en la cadena de producción. |

### Valores

| Valor | Descripción |
|---|---|
| `Primario` | Materia prima directamente extraída (grano, madera, pescado…). |
| `Intermedio` | Bien semielaborado fabricado a partir de materias primas (harina, tela…). |
| `Avanzado` | Bien manufacturado de alto valor que requiere varias transformaciones (pan, ropa…). |

---

## CiudadData

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/CiudadData.cs` |
| **Tipo** | `ScriptableObject` |
| **Módulo** | Ciudades — Configuración de mercado |
| **Descripción** | Describe la configuración estática de una ciudad de la Liga Hanseática: su nombre visible y la lista de bienes que comercia su mercado con los valores de stock inicial, stock máximo, y cadencias de producción y consumo diarios. Actúa como asset de datos; el estado dinámico en partida vive en `MarketManager`. Crear instancias desde el menú: Assets → Create → TFG → Ciudad. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `IdCiudad` | `int` | Identificador numérico único de la ciudad. Debe coincidir con el `id_ciudad` de la tabla Ciudad en SQLite (1=Lübeck, 2=Barcelona, 3=Génova, 4=Venecia, 5=Ruan, 6=Brujas). Asignar manualmente en el Inspector de cada asset. |
| `NombreCiudad` | `string` | Nombre de la ciudad que se mostrará en la interfaz (p. ej. "Lübeck", "Brujas"). |
| `Mercado` | `List<EntradaMercado>` | Lista de bienes disponibles en el mercado de esta ciudad, con su stock inicial y cadencias diarias. |
| `CasillaMapamundi` | `Vector3Int` | Casilla hexagonal del tilemap donde está ubicada esta ciudad. Z siempre 0. Usado por el pathfinding A* del Día 17. |

### Dependencias

- `EntradaMercado` — clase serializable que describe cada bien del mercado (definida en el mismo fichero).

---

## EntradaMercado

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/CiudadData.cs` |
| **Tipo** | `[Serializable] class` |
| **Módulo** | Ciudades — Configuración de mercado |
| **Descripción** | Agrupa la configuración de un bien dentro del mercado de una ciudad concreta: referencia al bien, stock inicial, stock máximo, tasas diarias de producción y consumo, y precio calculado en tiempo de ejecución. Se serializa en el Inspector. `MarketManager` realiza una copia profunda de estas entradas al iniciar la partida para no mutar el asset. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Bien` | `BienData` | Referencia al `BienData` que define nombre, categoría y precio base del bien. |
| `StockActual` | `int` | Unidades del bien disponibles actualmente en el mercado. Se reduce al comprar y aumenta al vender. |
| `StockMax` | `int` | Capacidad máxima de este bien en la ciudad. Límite de acumulación; previsto para renombrarse a `UmbralFlush` en la release. |
| `ProduccionDiaria` | `int` | Unidades que la ciudad genera de este bien cada día de juego. |
| `ConsumoDiario` | `int` | Unidades que la ciudad consume de este bien cada día de juego. |
| `PrecioActual` | `float` | Precio calculado en tiempo de ejecución según la fórmula de oferta y demanda. Oculto en el Inspector; se inicializa en `MarketManager.Start`. |

### Dependencias

- `BienData` — datos estáticos del bien representado.

---

## MarketManager

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Economico/MarketManager.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Económico — Bienes y mercado |
| **Descripción** | **Vista** del mercado de la ciudad activa. Desde el Día 12 ya no posee el estado: lee y escribe las entradas del mercado directamente sobre `GameManager.MercadosPorCiudad`. En `Start`, si `DatosCiudad` está asignado, lee las entradas del diccionario de `GameManager` (que `SeleccionCiudadUI` ha inicializado de forma eager antes de cargar la escena). Calcula precios dinámicos con la fórmula `precio = precioBase × (stockMaximo / max(stock, 1))` y ejecuta las operaciones de compra y venta del jugador. La API pública no ha cambiado: `OficinaComercial`, `MercadoUI` y `MarketRowUI` no requieren modificaciones. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `DatosCiudad` | `CiudadData` | Asset con la configuración de la ciudad activa. `Start` lo usa para obtener `IdCiudad` y recuperar las entradas de `GameManager`. |
| `OnMercadoActualizado` | `event Action<BienData>` | Se lanza cada vez que el stock o el precio de cualquier bien cambia. La interfaz del mercado se suscribe para refrescar las filas afectadas. |
| `GetEntradas()` | `IReadOnlyList<EntradaMercado>` | Devuelve la lista de entradas del mercado de la ciudad activa, leída desde `GameManager.MercadosPorCiudad`. |
| `GetNombreCiudad()` | `string` | Devuelve el nombre de la ciudad leído desde `DatosCiudad`. Retorna cadena vacía si no hay asset asignado. |
| `GetStockActual(BienData bien)` | `int` | Devuelve el stock actual de un bien en este mercado. Retorna 0 si el bien no existe. |
| `GetPrecioActual(BienData bien)` | `float` | Devuelve el precio actual de un bien calculado con la fórmula de oferta y demanda. Retorna 0 si el bien no existe. |
| `Comprar(BienData bien, int cantidad)` | `bool` | Descuenta el coste del tesoro, reduce el stock de la ciudad y carga las unidades en bodega. Devuelve `false` si stock, dinero o espacio son insuficientes. |
| `Vender(BienData bien, int cantidad)` | `bool` | Ingresa el precio en el tesoro, aumenta el stock de la ciudad y retira las unidades de bodega. Devuelve `false` si el jugador no tiene suficiente cantidad. |
| `AplicarTickDiario()` | `private void` | Aplica producción y consumo diarios a cada bien del mercado activo. Se invoca automáticamente al recibir `SimulacionTiempo.OnNuevoDia`. Trunca el stock al `UmbralFlush` (1 000 u.) si lo supera. |

### Dependencias

- `CiudadData` — fuente del `IdCiudad` para recuperar entradas del diccionario global.
- `GameManager` — dueño del estado; `MarketManager` lee y escribe sobre `MercadosPorCiudad` y llama a `NotificarMercadoActualizado`.
- `EntradaMercado` — estado dinámico de cada bien en tiempo de partida.
- `BienData` — referencia a los datos estáticos de cada bien.
- `SimulacionTiempo` — suscripción al evento `OnNuevoDia` para el tick diario.

---

## MarketRowUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/MarketRowUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Mercado |
| **Descripción** | Controla una fila del panel de mercado. Muestra el nombre del bien, el stock de la ciudad, el stock en bodega del jugador, el precio actual con indicador de color, y los botones de compra/venta (+1, +10, +100). Reacciona automáticamente a los cambios del mercado suscribiéndose al evento `MarketManager.OnMercadoActualizado`. Las operaciones de compra/venta se delegan en `OficinaComercial`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Inicializar(BienData bien, MarketManager marketManager, OficinaComercial oficina)` | `void` | Inicializa la fila con el bien, el gestor de mercado y la oficina comercial. Limpia y registra los listeners de los 6 botones y se suscribe al evento de actualización. Puede llamarse varias veces de forma segura. |

### Dependencias

- `BienData` — datos del bien que representa la fila.
- `MarketManager` — fuente de datos de stock y precio.
- `OficinaComercial` — intermediario que valida y ejecuta las operaciones de compra/venta.
- `GameManager` — para consultar el stock de bodega del jugador.
- `TextMeshProUGUI` (TMPro) — etiquetas de texto.
- `UnityEngine.UI.Button` — botones de compra/venta.
- `UnityEngine.UI.Image` — indicador de color de precio.

---

## MercadoUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/MercadoUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Mercado |
| **Descripción** | Panel de UI del mercado que se muestra sobre la escena Ciudad como diálogo modal. Al activarse (`OnEnable`) inicializa la `OficinaComercial`, se suscribe al evento `OnMercadoActualizado` y construye las filas del mercado; al desactivarse (`OnDisable`) desuscribe el evento y destruye las filas para evitar acumulación. El botón de cierre se registra en `Awake` y llama a `CiudadController.CerrarTodosPaneles()`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `BotonCerrar` | `Button` | Botón que cierra el panel del mercado. Se asigna desde el Inspector; su listener se registra en `Awake`. |
| `Cerrar()` | `void` | Cierra el panel delegando en `CiudadController.CerrarTodosPaneles()`. Si el controlador no está disponible, desactiva el panel directamente como fallback. |

### Dependencias

- `MarketManager` — fuente de datos del mercado activo.
- `OficinaComercial` — intermediario de compra/venta; se inicializa en `OnEnable` y se inyecta en cada fila.
- `MarketRowUI` — prefab que se instancia por cada bien en `OnEnable` y se destruye en `OnDisable`.
- `CiudadController` — receptor de `Cerrar()`; se cachea en `Awake` con `FindAnyObjectByType`.
- `GameManager` — para mostrar la capacidad usada del almacén en la cabecera.
- `TextMeshProUGUI` (TMPro) — etiquetas de cabecera.
- `UnityEngine.UI.Button` — botón de cierre.

---

## OficinaComercial

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Economico/OficinaComercial.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Económico — Bienes y mercado |
| **Descripción** | Intermediario exclusivo entre la UI del mercado y los sistemas de economía. Centraliza la validación de operaciones de compra y venta, y expone el resultado de la última operación en `UltimoMensaje` para que la UI lo muestre al jugador. Si `GameManager.Instance` es `null` al operar (prueba directa de escena), cancela la operación con un aviso en el log sin lanzar excepción. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `UltimoMensaje` | `string` (get) | Descripción textual del resultado de la última operación. La UI lo lee tras cada compra o venta. |
| `Inicializar(MarketManager mercado)` | `void` | Vincula la oficina al mercado de la ciudad activa. Debe llamarse antes de cualquier compra o venta. |
| `Comprar(BienData bien, int cantidad)` | `bool` | Valida stock de ciudad y dinero del jugador, y delega la compra en `MarketManager`. Devuelve `false` si alguna validación falla. |
| `Vender(BienData bien, int cantidad)` | `bool` | Valida unidades en bodega del jugador y delega la venta en `MarketManager`. Devuelve `false` si alguna validación falla. |

### Dependencias

- `MarketManager` — ejecuta la operación tras la validación.
- `GameManager` — consulta dinero y bodega del jugador.
- `BienData` — identifica el bien objeto de la operación.

---

## CiudadController

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/CiudadController.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Ciudades — Coordinación de escena |
| **Descripción** | Coordinador central de la escena Ciudad. En `Start` sincroniza `DatosCiudad` con `GameManager.CiudadActual` si el jugador llegó desde el mapamundi, muestra el nombre del puerto y oculta todos los paneles. Recibe los clicks de los edificios del mapa visual a través de `AbrirEdificio`. La tecla M activa `IrAMapamundi()`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `DatosCiudad` | `CiudadData` | Asset con el nombre y el mercado de la ciudad actual. Se sincroniza en `Start` desde `GameManager.CiudadActual` si el jugador llegó desde el mapamundi. |
| `PanelMercado` | `GameObject` | Panel de UI del mercado. Se activa al pulsar el edificio Mercado. Asignar desde el Inspector. |
| `PanelAstillero` | `GameObject` | Panel de UI del astillero. Se activa al pulsar el edificio Astillero. Asignar desde el Inspector. |
| `PanelTaberna` | `GameObject` | Panel de UI de la taberna. Se activa al pulsar el edificio Taberna. Asignar desde el Inspector. |
| `CerrarTodosPaneles()` | `void` | Oculta los tres paneles de edificio. Se invoca al iniciar la escena y antes de abrir cualquier panel. |
| `AbrirEdificio(TipoEdificio tipo)` | `void` | Cierra todos los paneles y activa el que corresponde al edificio pulsado. Invocado por los componentes `EdificioClickable`. |
| `IrAMapamundi()` | `void` | Cierra los paneles abiertos y navega al mapamundi. También se activa con la tecla M. |

### Dependencias

- `CiudadData` — fuente del nombre de ciudad y configuración del mercado.
- `GameManager` — origen de `CiudadActual` al llegar desde el mapamundi.
- `SceneController` — gestiona la navegación a otras pantallas.
- `EdificioClickable` — componentes que invocan `AbrirEdificio` al ser pulsados.
- `TipoEdificio` — enum que identifica el edificio pulsado.

---

## TipoEdificio

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/CiudadController.cs` |
| **Tipo** | `enum` |
| **Módulo** | Ciudades — Coordinación de escena |
| **Descripción** | Identifica cada edificio clickable del mapa visual de la ciudad. |

### Valores

| Valor | Descripción |
|---|---|
| `Mercado` | Mercado de la ciudad: permite comprar y vender mercancías. |
| `Astillero` | Astillero: construcción y reparación de barcos. Disponible tras la beta. |
| `Taberna` | Taberna: contratación de capitanes y tripulación. Disponible tras la beta. |

---

## EdificioClickable

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/EdificioClickable.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Ciudades — Coordinación de escena |
| **Descripción** | Detecta el click del jugador sobre el sprite de un edificio y notifica a `CiudadController` para que abra el servicio correspondiente. La referencia a `CiudadController` se cachea en `Awake` con `FindAnyObjectByType`. Requiere un `Collider2D` en el mismo GameObject para que `OnMouseDown` funcione. |

### API pública

_No expone miembros públicos. La interacción se produce íntegramente a través de `OnMouseDown` y la referencia cacheada a `CiudadController`._

### Dependencias

- `CiudadController` — receptor del evento de click.
- `TipoEdificio` — identifica qué edificio representa este sprite.

---

## PanelAstilleroUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/PanelAstilleroUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Ciudades — Interfaz de usuario |
| **Descripción** | Panel de UI del astillero que se muestra sobre la escena Ciudad como diálogo modal. En la beta actúa como stub: al activarse loguea `[Astillero] Disponible en la versión release.` La referencia a `CiudadController` se cachea en `Awake` con `FindAnyObjectByType` y el listener del botón de cierre se registra en el mismo método. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `BotonCerrar` | `Button` | Botón que cierra el panel del astillero. Se asigna desde el Inspector; su listener se registra en `Awake`. |
| `Cerrar()` | `void` | Cierra el panel delegando en `CiudadController.CerrarTodosPaneles()`. Si el controlador no está disponible, desactiva el panel directamente como fallback. |

### Dependencias

- `CiudadController` — receptor de `Cerrar()`; se cachea en `Awake` con `FindAnyObjectByType`.
- `UnityEngine.UI.Button` — botón de cierre.

---

## PanelTabernaUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Ciudades/PanelTabernaUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Ciudades — Interfaz de usuario |
| **Descripción** | Panel de UI de la taberna que se muestra sobre la escena Ciudad como diálogo modal. En la beta actúa como stub: al activarse loguea `[Taberna] Disponible en la versión release.` La referencia a `CiudadController` se cachea en `Awake` con `FindAnyObjectByType` y el listener del botón de cierre se registra en el mismo método. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `BotonCerrar` | `Button` | Botón que cierra el panel de la taberna. Se asigna desde el Inspector; su listener se registra en `Awake`. |
| `Cerrar()` | `void` | Cierra el panel delegando en `CiudadController.CerrarTodosPaneles()`. Si el controlador no está disponible, desactiva el panel directamente como fallback. |

### Dependencias

- `CiudadController` — receptor de `Cerrar()`; se cachea en `Awake` con `FindAnyObjectByType`.
- `UnityEngine.UI.Button` — botón de cierre.

---

## MapamundiController

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Navegacion/MapamundiController.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Mundo y navegación |
| **Descripción** | Controla el mapamundi: inicializa los marcadores de ciudad visibles en el mapa y gestiona la navegación del jugador hacia un puerto o al menú principal. En `Start` llama a `Inicializar(this)` en cada `MarcadorCiudad` del array. En la beta el viaje es inmediato; en la release se animará la flota sobre el mapa. La tecla M viaja directamente a la última ciudad visitada si existe. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Ciudades` | `MarcadorCiudad[]` | Marcadores de las ciudades navegables del mapa. Asignar desde el Inspector. |
| `ViajarACiudad(CiudadData ciudadDestino)` | `void` | Registra la ciudad de destino en `GameManager` y carga la pantalla de ciudad. Incluye guard contra `null`. |
| `IrAMenuPrincipal()` | `void` | Abandona la partida y regresa al menú principal. |

### Dependencias

- `MarcadorCiudad` — componentes que notifican el click del jugador sobre una ciudad.
- `CiudadData` — datos del puerto de destino.
- `GameManager` — registra la ciudad actual y expone `UltimaCiudad` para el atajo de teclado.
- `SceneController` — gestiona la carga de escenas.

---

## MarcadorCiudad

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Navegacion/MarcadorCiudad.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Mundo y navegación |
| **Descripción** | Componente que se adjunta al sprite de cada ciudad en el mapamundi. Detecta el click del jugador y delega el viaje en `MapamundiController`. Proporciona feedback visual escalando el sprite al 120 % al pasar el cursor. Requiere un `Collider2D` en el mismo GameObject para que los eventos `OnMouse*` funcionen. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `DatosCiudad` | `CiudadData` | ScriptableObject del puerto representado por este marcador. Asignar desde el Inspector. |
| `TextoNombre` | `TextMeshPro` | Etiqueta opcional que muestra el nombre de la ciudad sobre el marcador. Si es `null` no se muestra texto. |
| `Inicializar(MapamundiController controlador)` | `void` | Enlaza el marcador con el controlador del mapa y escribe el nombre de ciudad en `TextoNombre` si está asignado. Llamado por `MapamundiController.Start`. |

### Dependencias

- `MapamundiController` — receptor del evento de click; se recibe en `Inicializar`.
- `CiudadData` — datos del puerto que representa el marcador.
- `TextMeshPro` (TMPro) — etiqueta de nombre sobre el marcador.

---

## CiudadesEditorSetup

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Editor/CiudadesEditorSetup.cs` |
| **Tipo** | `static class` (editor only, `#if UNITY_EDITOR`) |
| **Módulo** | Utilidades de editor — Ciudades |
| **Descripción** | Genera los assets `CiudadData` de las ciudades de la beta con su mercado inicial preconfigurado. La lógica de creación está centralizada en el método genérico `CrearCiudad`; para añadir una ciudad nueva basta con añadir una llamada más en `CrearAssetsCiudades`. Actualmente genera Lübeck y Barcelona. Los `BienData` se cargan con `AssetDatabase.LoadAssetAtPath`; si alguno falta emite un aviso indicando que hay que ejecutar primero `TFG/Crear Bienes Primarios`. Solo se compila en el editor; no afecta a las builds. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `CrearAssetsCiudades()` | `static void` | Genera o actualiza los assets de todas las ciudades de la beta en `Assets/ScriptableObjects/Ciudades/`. Acceder desde el menú Unity: **TFG → Crear Assets de Ciudades**. |

### Dependencias

- `CiudadData` — tipo del asset que genera.
- `EntradaMercado` — instancias que rellena dentro del asset.
- `BienData` — assets cargados desde `Assets/ScriptableObjects/Bienes/`.
- `UnityEditor.AssetDatabase` — creación, carga y guardado de assets.
- `UnityEditor.EditorUtility` — marcado de assets como modificados.

---

## Decisiones de diseño pendientes

Decisiones de diseño cerradas cuya implementación queda diferida a la release o a módulos posteriores a la beta.

---

### Flush diario de excedente (pendiente — post-beta)

Al cambio de día, cada `EntradaMercado` descarta las unidades que superen un umbral configurable (1 000 por defecto). El mercado opera libre durante el día sin tope de compraventa; el flush es el único regulador de acumulación para evitar el colapso de precios.

**Impacto en el modelo de datos**
- `StockMax` en `EntradaMercado` y `CiudadData` se renombrará a `UmbralFlush` para reflejar su semántica real.
- `MarketManager` recibirá una lógica de tick diario que invoca el flush sobre cada entrada.

**Lógica narrativa:** el grano se estropea, la madera se pudre en el puerto; el umbral es el techo de lo que una ciudad puede almacenar útilmente de un día para otro.

**Fórmula de flush:**
```
if (entrada.StockActual > UmbralFlush)
    entrada.StockActual = UmbralFlush;
```

---

### Generación de soldados en abordaje (pendiente — módulo de combate)

Cada 5 tripulantes de un barco generan 1 soldado completo al iniciar un combate de abordaje. Si los tripulantes restantes no son múltiplo exacto de 5, se genera 1 soldado adicional con vida reducida proporcionalmente.

**Fórmula de generación:**
```
soldadosCompletos = tripulacion / 5          // división entera
resto             = tripulacion % 5
si resto != 0 → soldadoExtra.vida = (resto / 5.0) * vidaBase
```

**Ejemplo:** 13 tripulantes → 2 soldados completos + 1 soldado con 60 % de vida (3/5).

**Sistema de bajas por tramos de 20 %**

Cada tramo de vida representa 1 marinero del grupo de 5. Dentro del tramo, si el daño acumulado en ese tramo es ≥ 10 % el marinero muere; si es < 10 % sobrevive.

| Rango de vida | Bajas acumuladas |
|---|---|
| 100 – 81 % | 0 bajas |
| 80 – 61 % | 1 baja |
| 60 – 41 % | 2 bajas |
| 40 – 21 % | 3 bajas |
| 20 – 1 % | 4 bajas |
| 0 % | soldado muerto |

**Ejemplos:**
- Soldado al 85 % → 15 % de daño en el tramo 100–81 % → **1 baja**
- Soldado al 90 % → 10 % de daño en el tramo 100–81 % → **0 bajas**

Los detalles exactos se pulirán en la release; la lógica general queda fijada aquí.

---

### Vista isométrica y assets (pendiente — Día 6)

- Cambiar proyección de cámara a isométrica en todas las escenas.
- Descargar e integrar assets isométricos de Kenney: Isometric Blocks + Isometric City Pack + Isometric Roads.
- Opcionalmente mezclar con assets de OpenGameArt "Medieval Isometric" para reducir aspecto genérico.
- Todos los assets deben usar el mismo tamaño de tile (64×64 o 128×64) para que encajen en la cuadrícula.
- No mezclar Kenney con pixel art o assets hiperrealistas.
- Ajustar sorting order de sprites para profundidad isométrica correcta.

---

### Documentación técnica de referencia (pendiente — post-beta)

- Generar documentación técnica formal a partir de los XMLDoc existentes en el código.
- Herramienta recomendada: DocFX (estándar Microsoft para C#) — equivalente a Javadoc en Java; genera un sitio HTML navegable con parámetros, tipos, valores de retorno y sobrecargas.
- Requisito previo: XMLDoc completo en todas las clases públicas (ya en curso).
- Output: carpeta `docs/` en la raíz del repositorio, publicable en GitHub Pages.

---

### Revisión de arquitectura (pendiente — Día 6)

- Análisis estático de dependencias entre clases para detectar acoplamiento excesivo.
- Verificar que ninguna clase UI accede directamente a `GameManager` o `MarketManager` sin pasar por `OficinaComercial`.
- Verificar separación correcta entre datos (`ScriptableObjects`) y lógica (Managers).
- Verificar que no hay referencias circulares entre módulos.

---

### Integración del flujo completo (pendiente — Día 6)

- Flujo jugable completo: Menú Principal → seleccionar ciudad → Ciudad → Mapa funcional.
- Eliminar `GameManager` temporal de la escena Mapamundi una vez montado el flujo completo (usar el singleton persistente del Menú Principal).
- Sprites definitivos y mapa de fondo en la escena Mapamundi.
- Eliminar `IrAMenuPrincipal()` de `MapamundiController`; esta acción pasará a gestionarse exclusivamente desde el menú de pausa.
- Menú de pausa (tecla Escape) con opciones: Continuar / Menú Principal / Salir.

---

## EstadoPartida

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Core/EstadoPartida.cs` |
| **Tipo** | `[Serializable] class` (no MonoBehaviour) |
| **Módulo** | Core — Estado de partida |
| **Descripción** | POCO contenedor con todos los diccionarios del estado vivo del mundo. Es propiedad exclusiva de `GameManager`; ningún otro sistema instancia ni modifica esta clase directamente. Centraliza en un único objeto todo el estado que debe persistirse en SQLite al guardar y restaurarse al cargar. |

### API pública (campos)

| Campo | Tipo | Estado | Descripción |
|---|---|---|---|
| `MercadosPorCiudad` | `Dictionary<int, List<EntradaMercado>>` | ✅ Activo | Estado vivo de los mercados de todas las ciudades, indexado por `IdCiudad`. Poblado en Día 12. |
| `FlotasPorId` | `Dictionary<int, object>` | ⏳ Semana 3 | Estado de las flotas PNJ activas. Se rellena al implementar el módulo de PNJs comerciantes. |
| `BarcosPorId` | `Dictionary<int, object>` | ⏳ Semana 3 | Estado de los barcos activos por flota. Se rellena junto con `FlotasPorId`. |
| `EdificiosPorCiudad` | `Dictionary<int, object>` | ⏳ Semana 4 | Edificios activos por ciudad. Se rellena al implementar el módulo de producción y cadenas. |
| `MemoriaComercialPorFlota` | `Dictionary<int, object>` | ⏳ Semana 5 | Memoria de precios de los PNJs. Se rellena al implementar el comportamiento de PNJs. |

### Dependencias

- `EntradaMercado` — tipo de los valores en `MercadosPorCiudad`.

---

## DÍA 6 — Integración completa del PMV y pulido visual

### Flujo completo implementado

- MenuPrincipal con fondo de atardecer marítimo, botones medievales y tipografía Cinzel.
- Panel de selección de ciudad inicial con fondo mar/cielo.
- Flujo completo: MenuPrincipal → Selección ciudad → Ciudad → Mercado → Mapamundi → Ciudad.
- Fix race condition: mercado de Barcelona ya no muestra datos de Lübeck.
- Fix click fantasma al entrar a ciudad desde mapamundi (delay 0.5 s en `CiudadController`).
- Fix botón cerrar mercado que navegaba al mapamundi en lugar de cerrar el panel.
- Botón Mapa se oculta al abrir paneles de edificios.

### Mercado

- UI con columnas: Producto / Stock Ciudad / Comprar / Indicador / Precio / Vender / Stock Almacén.
- Compra/venta inteligente: si no hay stock suficiente, opera hasta el máximo disponible sin fallar.
- Precios reactivos con límite mínimo (0.5× precio base) y máximo (5× precio base).
- Indicador de color basado en precio vs precio base:
  - Verde: precio ≤ 1.0× base (stock alto, buen momento para vender).
  - Amarillo: precio entre 1.0× y 2.0× base (precio normal).
  - Rojo: precio > 2.0× base (stock bajo, buen momento para comprar).
- Panel mercado con fondo pergamino, marco decorativo medieval y tipografía Cinzel.

### Menú de pausa

- Hotkey Escape activa/desactiva el panel de pausa en Ciudad y Mapamundi.
- `Time.timeScale = 0` al pausar, `1` al reanudar.
- Botones: Continuar / Menú Principal / Salir al escritorio.
- `MenuPausaEditorSetup.cs` genera el menú de pausa con un click desde **TFG → Generar Menú de Pausa**. Compatible con escenas sin Canvas (crea Canvas automáticamente).

### Visual y UI

- Canvas Scaler 1920×1080 configurado en todas las escenas.
- Fondo MenuPrincipal: atardecer marítimo con barco.
- Fondo PanelSeleccionCiudad: cielo/mar con gradiente.
- Fondo Ciudad: ilustración medieval isométrica (placeholder).
- Fondo Mapamundi: mapa de Europa medieval estilo pergamino (crédito: senior_lavash, Reddit).
- Marcadores de ciudad reposicionados geográficamente sobre el mapa real.
- Edificios clickables implementados como botones UI dentro del Canvas.
- `UIGradiente.cs`: aplica gradiente vertical a componentes `Image`.

### Scripts de editor creados en el Día 6

| Menú Unity | Script | Función |
|---|---|---|
| TFG → Generar Menú de Pausa | `MenuPausaEditorSetup.cs` | Genera el panel de pausa con botones en la escena activa. |
| TFG → Regenerar Prefab MarketRow | `MarketRowEditorSetup.cs` | Regenera el prefab de fila de mercado. |

---

## MenuPrincipalUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/MenuPrincipalUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Menú Principal |
| **Descripción** | Controla la lógica del Menú Principal: mostrar el panel de selección de ciudad al iniciar nueva partida, cerrarlo con Escape o con el botón Atrás, abrir el panel de slots en modo Cargar, y salir de la aplicación. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `panelSeleccionCiudad` | `GameObject` | Panel con los botones de ciudad para comenzar una nueva partida. Se asigna desde el Inspector. |
| `IniciarNuevaPartida()` | `void` | Activa el panel de selección de ciudad. Llamado por el botón "Nueva Partida". |
| `CerrarPanelSeleccion()` | `void` | Oculta el panel de selección. Llamado por el botón "Atrás" y por la tecla Escape. |
| `CargarPartida()` | `void` | Abre `PantallaSlotsUI` en modo `Cargar`. Requiere `_pantallaSlotsUI` asignado en el Inspector. |
| `MostrarMenuPrincipal()` | `void` | Desactiva el panel de slots y reactiva el panel raíz del menú. Llamado por `PantallaSlotsUI.CerrarPanel()` al volver. |
| `Salir()` | `void` | Cierra la aplicación con `Application.Quit()`. |

### Campos SerializeField (Día 11)

| Campo | Tipo | Descripción |
|---|---|---|
| `_pantallaSlotsUI` | `PantallaSlotsUI` | Referencia al panel de slots. Asignar en el Inspector. |
| `_panelMenuPrincipal` | `GameObject` | Panel raíz con los botones del menú. Se reactiva al cerrar el panel de slots. |

### Dependencias

- `PantallaSlotsUI` — panel de guardado/carga que se abre en modo Cargar.
- `SceneController` — navegación entre pantallas (uso indirecto vía `SeleccionCiudadUI`).

---

## SeleccionCiudadUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/SeleccionCiudadUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Menú Principal |
| **Descripción** | Componente adjunto a cada botón de ciudad en el panel de selección. Al pulsarlo, inicializa los mercados de todas las ciudades del catálogo en `GameManager` (inicialización eager), registra la ciudad elegida como ciudad actual y carga la escena de ciudad. La inicialización eager garantiza que `GameManager.MercadosPorCiudad` contiene todas las ciudades antes de que ningún `MarketManager` arranque. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `datosCiudad` | `CiudadData` | Ciudad asociada a este botón. Asignar desde el Inspector. |
| `SeleccionarCiudad()` | `void` | Llama a `GameManager.InicializarMercadosDesdeAssets(todasLasCiudades)`, establece `GameManager.CiudadActual` y llama a `SceneController.IrACiudad()`. Asignar al evento `OnClick` del botón. |

### Dependencias

- `CiudadData` — datos del puerto que representa el botón y fuente del catálogo de ciudades.
- `GameManager` — inicializa mercados de todas las ciudades y registra la ciudad seleccionada.
- `SceneController` — carga la escena Ciudad.

---

## MenuPausa

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/MenuPausa.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Pausa |
| **Descripción** | Gestiona el menú de pausa en las escenas jugables (Ciudad, Mapamundi). La tecla Escape alterna la visibilidad del panel. La gestión del tiempo se delega en `SimulacionTiempo` (si existe en la escena). En escenas sin simulación (Menú Principal) el panel abre y cierra sin tocar el tiempo. Desde el Día 11 incluye botones Guardar y Cargar que abren `PantallaSlotsUI` como popup sin cerrar el menú ni reanudar el tiempo. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Continuar()` | `void` | Oculta el panel y llama a `SimulacionTiempo.ReanudarDesdMenu()`. Asignar al botón "Continuar". |
| `IrAMenuPrincipal()` | `void` | Llama a `ReanudarDesdMenu()` y carga el Menú Principal abandonando la partida. Asignar al botón "Menú Principal". |
| `SalirAlEscritorio()` | `void` | Llama a `ReanudarDesdMenu()` y cierra la aplicación. En el editor detiene el modo Play. Asignar al botón "Salir". |

### Campos SerializeField (Día 11)

| Campo | Tipo | Descripción |
|---|---|---|
| `_botonGuardar` | `Button` | Botón "Guardar" del panel de pausa. Su listener se registra en `Start`. |
| `_botonCargar` | `Button` | Botón "Cargar" del panel de pausa. Su listener se registra en `Start`. |
| `_pantallaSlotsUI` | `PantallaSlotsUI` | Panel de slots. Se abre como popup sin cerrar el menú ni reanudar el tiempo. Dejar `_menuPrincipalUI` vacío en este contexto. |

### Dependencias

- `PantallaSlotsUI` — popup de guardado/carga; se abre sin cerrar el menú de pausa.
- `SceneController` — carga el Menú Principal en `IrAMenuPrincipal()`.
- `SimulacionTiempo` — delega la pausa/reanudación del tiempo de juego (null-safe).

---

---

## SimulacionTiempo

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Core/SimulacionTiempo.cs` |
| **Tipo** | `MonoBehaviour` (singleton ligero) |
| **Módulo** | Tiempo y simulación |
| **Descripción** | Gestiona el tiempo interno del juego: fecha, velocidad de simulación y avance diario. Se añade manualmente al GameObject GameManager. No modifica `Time.timeScale`; la velocidad se aplica multiplicando por `VelocidadActual` en el acumulador interno, lo que permite que el menú de pausa y las animaciones de UI sigan activos aunque el tiempo de juego esté pausado. El input de teclado (Espacio, +, −) vive aquí para centralizar toda la lógica de tiempo en un único componente. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static SimulacionTiempo` (get) | Punto de acceso global. Puede ser `null` en escenas sin simulación. |
| `DiaActual` | `int` (get) | Día actual del calendario de juego (1-30). |
| `MesActual` | `int` (get) | Mes actual del calendario de juego (1-12). |
| `AñoActual` | `int` (get) | Año actual del calendario de juego. |
| `VelocidadActual` | `float` (get) | Velocidad de simulación activa. 0 si está pausado. |
| `EstaPausado` | `bool` (get) | `true` si `VelocidadActual == 0`. |
| `OnNuevoDia` | `static event Action` | Se dispara cada vez que avanza un día de juego. |
| `OnNuevoMes` | `static event Action` | Se dispara cada vez que avanza un mes de juego. |
| `OnVelocidadCambiada` | `static event Action` | Se dispara cada vez que cambia la velocidad o el estado de pausa. |
| `SubirVelocidad()` | `void` | Incrementa la velocidad al siguiente nivel. Salta el índice 0 (pausa). Dispara `OnVelocidadCambiada`. |
| `BajarVelocidad()` | `void` | Reduce la velocidad al nivel anterior. Nunca llega a pausa. Dispara `OnVelocidadCambiada`. |
| `TogglePausa()` | `void` | Alterna entre pausa y la última velocidad activa guardada. Dispara `OnVelocidadCambiada`. |
| `PausarPorMenu()` | `void` | Pone velocidad a 0 sin modificar el estado guardado. Llamado por `MenuPausa` al abrir el panel. |
| `ReanudarDesdMenu()` | `void` | Restaura la velocidad previa al abrir el menú. Llamado por `MenuPausa` al cerrar el panel. |
| `GetFechaFormateada()` | `string` | Devuelve la fecha en formato "1 de Marzo de 1350". |
| `GetVelocidadFormateada()` | `string` | Devuelve "\|\|", "0.25x", "1x", "2x" o "10x" según la velocidad actual. |
| `SetEstado(int dia, int mes, int anio, float velocidad)` | `void` | Sobrescribe la fecha y la velocidad con los valores leídos desde SQLite al cargar una partida. Busca el índice correcto en `_velocidadesValidas` con `Mathf.Approximately`; si no hay coincidencia, usa 1x como fallback. Dispara `OnVelocidadCambiada` para refrescar el HUD. Añadido en Día 10 para uso de `LoadManager`. |
| `OnDestroy()` | `private void` | Limpia `Instance` si este objeto era la instancia activa, evitando referencias colgantes al destruir la escena. |

### Dependencias

- `MenuPausa` — llama a `PausarPorMenu` y `ReanudarDesdMenu`.
- `MarketManager` — se suscribe a `OnNuevoDia` para el tick diario.
- `HUDTiempo` — se suscribe a `OnNuevoDia` y `OnVelocidadCambiada`.

### Decisión de diseño

SimulacionTiempo usa eventos estáticos para que `MarketManager` se suscriba sin acoplamiento directo. El input de teclado vive en `SimulacionTiempo` para centralizar toda la lógica de tiempo en un único componente.

---

## HUDTiempo

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/HUDTiempo.cs` |
| **Tipo** | `MonoBehaviour` (singleton persistente) |
| **Módulo** | Interfaz de usuario — HUD |
| **Descripción** | Panel de HUD que muestra la fecha y la velocidad de simulación actuales. Persiste entre escenas mediante `DontDestroyOnLoad`; si se detecta un duplicado al cargar una escena, la instancia nueva se auto-destruye. Se oculta automáticamente en escenas no jugables suscribiéndose a `SceneManager.sceneLoaded`; las escenas visibles se configuran en `_escenasVisibles` desde el Inspector (por defecto: "Ciudad", "Mapamundi"). Se suscribe a `SimulacionTiempo.OnNuevoDia` y `OnVelocidadCambiada` para refrescar la interfaz solo cuando hay cambios. Los botones son un acceso alternativo vía ratón; el input de teclado lo gestiona `SimulacionTiempo`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static HUDTiempo` (get) | Punto de acceso global al HUD persistente. |
| `ActualizarUI()` | `void` | Actualiza textos y estado interactable de botones. Se llama en `Start` y en cada evento de `SimulacionTiempo`. Incluye guard nulo contra `_simulacion`. Los listeners de los botones también tienen guard nulo: si `_simulacion` es `null` se limpian pero no se registran. |

### Campos SerializeField

| Campo | Tipo | Descripción |
|---|---|---|
| `_escenasVisibles` | `string[]` | Nombres exactos de las escenas donde el HUD es visible. Por defecto: `{ "Ciudad", "Mapamundi" }`. En el resto de escenas `_panelVisible` se desactiva automáticamente al cargar. |
| `_panelVisible` | `GameObject` | GameObject hijo que contiene todo el contenido visual del HUD. Solo este hijo se activa/desactiva según la escena; el GameObject raíz permanece siempre activo para que la suscripción a `SceneManager.sceneLoaded` no se pierda. |

### Dependencias

- `SimulacionTiempo` — fuente de datos de fecha y velocidad.
- `TextMeshProUGUI` (TMPro) — etiquetas de fecha y velocidad.
- `UnityEngine.UI.Button` — botones de subir/bajar velocidad.
- `UnityEngine.SceneManagement.SceneManager` — suscripción a `sceneLoaded` para control de visibilidad.

### Setup

El GameObject que contiene `HUDTiempo` debe crearse en **una sola escena** (recomendado: la primera escena jugable que cargue). El componente se marca `DontDestroyOnLoad` y se auto-destruye si detecta un duplicado al cambiar de escena. **No añadir `HUDTiempo` en el Canvas de cada escena individual.** La visibilidad en cada escena se controla exclusivamente mediante `_escenasVisibles`.

---

---

## Módulo de Guardado y Carga (SQLite)

Conjunto de clases que persisten y restauran el estado completo de una partida en un fichero `.db` por slot. Todos los DAOs reciben `DatabaseManager` por constructor y reutilizan su `Conexion` sin abrir conexiones propias.

### Estado de implementación

| Estado | Clase | Descripción |
|---|---|---|
| ✅ | `DatabaseManager` | Singleton `MonoBehaviour` con `DontDestroyOnLoad`. Abre o crea el fichero `slot_N.db` en `Application.persistentDataPath` y garantiza que las 15 tablas existen con `CREATE TABLE IF NOT EXISTS` antes de que cualquier DAO opere. |
| ✅ | `EstadoJuegoDAO` | Guarda y carga la fila única de `estadoJuego` (día, mes, año, velocidad del tiempo, fecha de guardado UTC). Usa `INSERT OR REPLACE` con `id_estado = 1`. |
| ✅ | `CiudadDAO` | Inserta las 6 ciudades iniciales con IDs fijos (`InsertarCiudadesIniciales`) y lee todas las ciudades. Expone `CiudadDto` para no colisionar con el `ScriptableObject` `CiudadData` del Inspector. |
| ✅ | `BienDAO` | Inserta los 19 bienes con precios base (`InsertarBienesIniciales`), lee por id y lista completa. Expone `BienDto` para no colisionar con el `ScriptableObject` `BienData`. |
| ✅ | `EstadoMercadoCiudadDAO` | Guarda y carga el estado completo del mercado de cada ciudad: stock, precio, producción y consumo por bien. Desde el Día 12, `GuardarTodoElMercado` recibe `(int idCiudad, IReadOnlyList<EntradaMercado> entradas)` en lugar de `(CiudadData, MarketManager)`. Nuevo método `ObtenerIdsCiudadesConMercado()` devuelve `List<int>` con los IDs de ciudades persistidas, usado por `LoadManager` para repoblar todas las ciudades. |
| ✅ | `EdificiosCiudadDAO` | Inserta los tipos de edificio en `TipoEdificio` e inserta los 6 edificios base por ciudad en `EdificiosCiudad`. Lee los edificios de una ciudad con JOIN a `TipoEdificio`. |
| ✅ | `FlotaDAO` | Inserta, actualiza posición y estado, obtiene por tipo de propietario y elimina flotas. Los campos `id_ciudad_actual`, `id_capitan` e `id_ciudad_destino` son nullable; se mapean con el patrón `(object)valor ?? DBNull.Value` y se leen con `reader.IsDBNull`. |
| ✅ | `BarcoDAO` | Inserta los 3 tipos de casco base (Cog, Hulk, Carraca) en `TipoCasco`. Gestiona barcos por flota: inserta, actualiza vida/tripulación/flota y elimina. Booleano `es_barco_combate` almacenado como `1`/`0`. |
| ✅ | `CargaBarcoDAO` | Guarda y carga la mercancía en bodega de cada barco. `GuardarCargaCompleta` limpia la carga anterior con `EliminarCargaDeBarco` antes de reinsertar, garantizando atomicidad. `ObtenerCargaDeBarco` usa JOIN a `Bien` para traer el nombre sin segunda consulta. |
| ✅ | `MemoriaComercialPNJDAO` | Guarda y carga el conocimiento de precios de los PNJs comerciantes con caducidad de 7 días. `ObtenerPrecioConocido` devuelve `null` si el dato supera ese umbral. |
| ✅ | `AlmacenJugadorDAO` | DAO que gestiona la tabla `AlmacenJugador`. Persiste y restaura el inventario personal del jugador (bienes en bodega) entre sesiones. Sigue el patrón atómico borrar-e-insertar de `CargaBarcoDAO`. Añadido en Día 13. |
| ✅ | `SaveManager` | Singleton `MonoBehaviour` que orquesta el guardado completo respetando integridad referencial: estadoJuego (con `dinero_jugador`) → catálogos (Ciudad, Bien, TipoEdificio, TipoCasco) → **AlmacenJugador** → EstadoMercadoCiudad → EdificiosCiudad. Desde el Día 13 también persiste `GameManager.Dinero` y el inventario completo del jugador. ✅ Bug persistencia multi-ciudad resuelto (Día 12). ✅ Bug persistencia dinero y almacén resuelto (Día 13). |
| ✅ | `LoadManager` | Singleton `MonoBehaviour` que orquesta la carga completa: abre slot → restaura `SimulacionTiempo` → **restaura dinero del jugador** → limpia almacén → **restaura AlmacenJugador** → limpia `GameManager.MercadosPorCiudad` → repuebla con **todas las ciudades** guardadas en BD. Desde el Día 13 usa `GameManager.GetBienPorNombre` (catálogo canónico) en lugar de `Resources.FindObjectsOfTypeAll`. ✅ Bug persistencia multi-ciudad resuelto (Día 12). ✅ Bug persistencia dinero y almacén resuelto (Día 13). |
| ✅ | Pantalla de slots | UI con 5 slots implementada en `SlotData` + `SlotUI` + `PantallaSlotsUI`. Muestra nombre de partida, fecha de guardado y días jugados. Soporta modos Guardar y Cargar con confirmación antes de sobrescribir o borrar. |

### DTOs del módulo

| DTO | Archivo | Campos |
|---|---|---|
| `EstadoJuegoData` | `EstadoJuegoDAO.cs` | `DiaJuego`, `MesJuego`, `AñoJuego`, `VelocidadTiempo`, `FechaGuardado`, **`DineroJugador`** (añadido Día 13) |
| `CiudadDto` | `CiudadDAO.cs` | `IdCiudad`, `Nombre`, `CasillaX`, `CasillaY` |
| `BienDto` | `BienDAO.cs` | `IdBien`, `Nombre`, `Categoria`, `PrecioBase` |
| `EstadoMercadoDto` | `EstadoMercadoCiudadDAO.cs` | `IdCiudad`, `IdBien`, `Stock`, `Produccion`, `Consumo`, `PrecioActual` |
| `EdificiosCiudadDto` | `EdificiosCiudadDAO.cs` | `IdCiudad`, `IdTipoEdificio`, `NombreTipoEdificio`, `Cantidad` |
| `FlotaDto` | `FlotaDAO.cs` | `IdFlota`, `TipoPropietario`, `IdCiudadActual`(?), `PosicionX`, `PosicionY`, `IdCapitan`(?), `EstadoActual`, `IdCiudadDestino`(?) |
| `BarcoDto` | `BarcoDAO.cs` | `IdBarco`, `IdTipoCasco`, `NombreBarco`, `EsBarcosCombate`, `VidaActual`, `TripulacionActual`, `CapacidadTripulacion`, `IdFlota`(?) |
| `CargaBarcoDto` | `CargaBarcoDAO.cs` | `IdBarco`, `IdBien`, `NombreBien`, `Cantidad` |
| `MemoriaComercialPNJDto` | `MemoriaComercialPNJDAO.cs` | `IdFlota`, `IdBien`, `PrecioConocido`, `DiaJuegoConocido` |
| `SlotData` | `SlotData.cs` | `NumeroSlot`, `EstaOcupado`, `NombrePartida`, `FechaGuardado`, `DiasJugados` |

---

## TO-DO POST-BETA

### Alta prioridad

#### Refactorizar selección de ciudad como escena separada

- **Módulo:** UI
- **Descripción:** Actualmente es un panel dialog sobre MenuPrincipal. Convertir en escena separada `SeleccionCiudad.unity` siguiendo el patrón de Patrician III/IV.
- **Impacto:** Crear escena, mover contenido del panel, actualizar `SceneController`.

#### MapamundiCamara — zoom y scroll por bordes

- **Módulo:** Navegación
- **Descripción:** Script de cámara para el mapamundi cuando el mapa sea más grande que la pantalla. Zoom con rueda del ratón (`orthographicSize` min: 3, max: 15) y movimiento llevando el ratón a los bordes.
- **Prioridad:** Alta para release.

#### Mapa de Europa medieval propio

- **Módulo:** Visual
- **Descripción:** Crear o encargar un mapa 2D estilizado de Europa medieval específico para el juego. El placeholder actual es de senior_lavash (Reddit).

#### Fondo de ciudad específico por ciudad

- **Módulo:** Visual
- **Descripción:** Actualmente Lübeck y Barcelona usan el mismo placeholder. Crear ilustraciones específicas para cada una de las 6 ciudades.

### Media prioridad

#### Flush diario de excedente de stock

- **Módulo:** Económico
- **Descripción:** Al cambio de día, cada `EntradaMercado` descarta unidades que superen el umbral configurable. `StockMax` se renombrará a `UmbralFlush` en la release. Ver sección "Decisiones de diseño pendientes" para la fórmula completa.

#### Icono de moneda en TextoPrecio del mercado

- **Módulo:** UI / Mercado
- **Descripción:** Reemplazar el texto `"g"` junto al precio por un sprite de moneda de oro. Requiere `Image` hijo en el prefab `MarketRow` con sprite de moneda asignado.

#### Indicador de color del mercado como círculo

- **Módulo:** UI / Mercado
- **Descripción:** El indicador actual es un rectángulo. Cambiar a círculo pequeño usando el sprite "Knob" de Unity.

#### Revisión de arquitectura

- **Módulo:** Core
- **Descripción:** Análisis estático de dependencias. Verificar que la UI no accede directamente a `GameManager` o `MarketManager` sin pasar por `OficinaComercial`. Verificar separación datos/lógica y ausencia de referencias circulares.

### Baja prioridad

#### Generación de soldados en abordaje

- **Módulo:** Combate
- **Descripción:** Cada 5 tripulantes → 1 soldado completo. Resto no múltiplo de 5 → 1 soldado extra con vida proporcional. Sistema de bajas por tramos de 20 %. Ver sección "Decisiones de diseño pendientes" para la fórmula completa.

#### Animaciones en pantalla de ciudad

- **Módulo:** Visual
- **Descripción:** Partículas Unity para simular movimiento sobre fondo estático: agua ondulante, humo de chimeneas, gaviotas.

---

### Día 7 — Pendientes identificados

#### Consumo diario selectivo por tipo de bien

- **Módulo:** Módulo económico
- **Descripción:** Actualmente `AplicarTickDiario()` reduce el stock de todos los bienes cada día. En release avanzada, solo los bienes clasificados como alimentos o materias primas de edificios activos de la ciudad deben consumir stock diariamente. Los bienes comerciales puros (no son alimento ni materia prima de ningún edificio activo en esa ciudad) solo pierden stock por compras del jugador o al superar `StockMax`.
- **Requisitos previos:** Campo de categoría en `BienData` (alimento, materia prima, bien de lujo) + módulo de edificios de ciudad implementado.

#### Integración de pausa con combate PVE manual

- **Módulo:** Módulo de combate naval
- **Descripción:** Al entrar en combate PVE manual llamar a `SimulacionTiempo.Instance.PausarPorMenu()` y ocultar `HUDTiempo.Instance` (`HUDTiempo.Instance.gameObject.SetActive(false)`). Al salir del combate, llamar a `ReanudarDesdMenu()` y reactivar el HUD. El combate automático no pausa el tiempo — se resuelve como popup sobre la escena activa.

#### Añadir ciudades nuevas al Mapamundi

- **Módulo:** Módulo de mundo y navegación
- **Descripción:** Los assets ScriptableObject de Génova, Venecia, Ruan y Brujas están creados pero los marcadores en la escena Mapamundi.unity hay que añadirlos manualmente con sus posiciones geográficas correctas, siguiendo el mismo patrón que Lübeck y Barcelona (SpriteRenderer + BoxCollider2D + MarcadorCiudad + TextoNombre).

---

## TO-DO Día 9

- [ ] Asignar `IdCiudad` en el Inspector de cada ScriptableObject de ciudad (1=Lübeck, 2=Barcelona, 3=Génova, 4=Venecia, 5=Ruan, 6=Brujas) — HECHO
- [ ] Verificar que `GuardarTodoElMercado` encuentra los bienes por nombre correctamente cuando se integre con `SaveManager` — HECHO (revisión de integración Día 10, bug corregido en `SaveManager.GuardarEstadoEconomico`)
- [ ] `MemoriaComercialPNJDAO` pendiente para cuando se implemente el módulo de PNJs (semana 3) — HECHO (Día 10)

---

## DÍA 10 — Módulo de guardado/carga completo + Tests + Corrección de plugins SQLite

### Clases implementadas

---

## MemoriaComercialPNJDAO

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/MemoriaComercialPNJDAO.cs` |
| **Tipo** | `class` (no MonoBehaviour) |
| **Módulo** | Guardado y carga — PNJs |
| **Descripción** | DAO que gestiona la tabla `MemoriaComercialPNJ`. Representa el conocimiento de mercado acumulado por los PNJs comerciantes: cada flota recuerda el último precio observado para cada bien, pero ese dato caduca a los 7 días de juego, obligando a los PNJs a visitar ciudades con regularidad para mantener información fiable. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `GuardarMemoria(int idFlota, int idBien, double precioConocido, int diaJuegoActual)` | `void` | Registra o actualiza el precio observado por una flota para un bien. `INSERT OR REPLACE`, idempotente. |
| `ObtenerMemoriaDeFlota(int idFlota)` | `List<MemoriaComercialPNJDto>` | Devuelve toda la memoria de una flota sin filtrar por antigüedad. Útil para serializar el estado de un PNJ. |
| `ObtenerPrecioConocido(int idFlota, int idBien, int diaActual)` | `double?` | Devuelve el precio memorizado si `diaActual - diaJuegoConocido < 7`; `null` si el dato ha caducado o no existe. |
| `EliminarMemoriaDeFlota(int idFlota)` | `void` | Borra toda la memoria de una flota. Se llama al disolver un PNJ para evitar filas huérfanas. |

### Dependencias

- `DatabaseManager` — proporciona la `SqliteConnection` activa.
- `MemoriaComercialPNJDto` — DTO interno definido en el mismo fichero.

---

## SaveManager

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/SaveManager.cs` |
| **Tipo** | `MonoBehaviour` (singleton persistente) |
| **Módulo** | Guardado y carga |
| **Descripción** | Orquesta el guardado completo de una partida en SQLite invocando los DAOs en el orden correcto para respetar la integridad referencial: estadoJuego (con `dinero_jugador`) → catálogos Ciudad, Bien, TipoEdificio, TipoCasco → **AlmacenJugador** → EstadoMercadoCiudad de **todas las ciudades** → EdificiosCiudad. Desde el Día 12 ya no busca `MarketManager` con `FindAnyObjectByType`. Desde el Día 13 también persiste `GameManager.Dinero` y el inventario completo del jugador. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static SaveManager` (get) | Punto de acceso global al gestor de guardado. |
| `GuardarPartida(int slotIndex)` | `void` | Ejecuta el guardado completo en el slot indicado (1 a 5). Abre o crea el fichero `slot_N.db`, instancia los DAOs y los invoca en orden. Loguea cada paso con `Debug.Log`. |

### Dependencias

- `DatabaseManager` — abre el slot antes del guardado.
- `SimulacionTiempo` — fuente de fecha y velocidad actual.
- `GameManager` — fuente de `MercadosPorCiudad`; se itera para guardar todas las ciudades.
- `EstadoJuegoDAO`, `CiudadDAO`, `BienDAO`, `EdificiosCiudadDAO`, `BarcoDAO`, `EstadoMercadoCiudadDAO` — ejecutan las escrituras.

---

## LoadManager

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/LoadManager.cs` |
| **Tipo** | `MonoBehaviour` (singleton persistente) |
| **Módulo** | Guardado y carga |
| **Descripción** | Orquesta la carga completa de una partida desde SQLite. Desde el Día 12 ya no busca `MarketManager` con `FindAnyObjectByType`. Limpia `GameManager.MercadosPorCiudad` y lo repuebla iterando todas las ciudades que devuelve `EstadoMercadoCiudadDAO.ObtenerIdsCiudadesConMercado()`. Los bienes se emparejan por nombre entre `BienData` y `BienDto` para evitar dependencias de IDs entre el Inspector y la BD. Cuando el jugador entra en una ciudad, su `MarketManager` lee el estado ya restaurado desde `GameManager`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static LoadManager` (get) | Punto de acceso global al gestor de carga. |
| `CargarPartida(int slotIndex)` | `void` | Carga la partida del slot indicado (1 a 5). Abre el fichero `slot_N.db`, restaura `SimulacionTiempo`, limpia el almacén del jugador y repuebla `GameManager.MercadosPorCiudad` con el mercado de todas las ciudades guardadas. |

### Dependencias

- `DatabaseManager` — abre el slot antes de la carga.
- `SimulacionTiempo` — receptor de la fecha y velocidad restauradas vía `SetEstado`.
- `GameManager` — limpia y restaura el almacén del jugador; receptor del diccionario de mercados restaurado.
- `EstadoJuegoDAO`, `BienDAO`, `EstadoMercadoCiudadDAO` — ejecutan las lecturas.

---

## SlotData

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/SlotData.cs` |
| **Tipo** | `class` (no MonoBehaviour) |
| **Módulo** | Guardado y carga — UI |
| **Descripción** | POCO con los metadatos de un slot de guardado. Instanciado por `PantallaSlotsUI` al escanear los archivos en disco; pasado a `SlotUI.Inicializar` para rellenar la fila visual. No tiene lógica propia. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `NumeroSlot` | `int` | Número de slot (1 a 5). Determina el nombre del archivo `slot_N.db`. |
| `EstaOcupado` | `bool` | `true` si el archivo `slot_N.db` existe y contiene una partida guardada. |
| `NombrePartida` | `string` | Nombre visible del slot. Por defecto `"Partida N"`. |
| `FechaGuardado` | `string` | Fecha y hora del último guardado formateada como `"dd/MM/yyyy HH:mm"`. Vacío si el slot no está ocupado. |
| `DiasJugados` | `int` | Días de juego transcurridos leídos desde `estadoJuego`. Cero si el slot está vacío. |

---

## SlotUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/SlotUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Guardado y carga — UI |
| **Descripción** | Componente adjunto a cada fila de slot. Recibe los metadatos y el modo del panel con `Inicializar`, rellena los textos con colores medievales y muestra solo los botones pertinentes según el modo. Activa explícitamente su propio `gameObject` y el padre `BotonesSlot` al inicializarse para garantizar visibilidad aunque estuvieran desactivados en escena. Llama siempre a `RemoveAllListeners` antes de registrar listeners para evitar duplicados. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Datos` | `SlotData` (get) | Metadatos del slot que esta fila representa. Accesible desde `PantallaSlotsUI`. |
| `Inicializar(SlotData datos, SlotModo modo, PantallaSlotsUI pantalla)` | `void` | Rellena textos, aplica colores y configura visibilidad de botones según modo: `Guardar` muestra solo BotonGuardar; `Cargar` muestra BotonCargar y BotonBorrar (solo si el slot está ocupado). Slot vacío muestra "— Vacío —" en gris. |

### Colores de texto (Día 11)

| Elemento | Color | Condición |
|---|---|---|
| `TextoNombre` | Dorado `#C8A84B` | Slot ocupado |
| `TextoNombre` | Gris `#888888` | Slot vacío |
| `TextoFecha` | Gris `#AAAAAA` | Siempre |
| `TextoDias` | Gris `#AAAAAA` | Siempre |

### Dependencias

- `SlotData` — metadatos que rellena la fila.
- `SlotModo` — controla qué botones son visibles.
- `PantallaSlotsUI` — receptor de las acciones de los botones.
- `TextMeshProUGUI` (TMPro) — etiquetas de nombre, fecha y días.
- `UnityEngine.UI.Button` — botones Guardar, Cargar y Borrar.

---

## PantallaSlotsUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/PantallaSlotsUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Guardado y carga — UI |
| **Descripción** | Panel completo de selección de slots. Al abrirse escanea los cinco archivos `slot_N.db` con una conexión SQLite temporal de solo lectura, lee los metadatos de `estadoJuego` y rellena cada `SlotUI`. Delega el guardado en `SaveManager`, la carga en `LoadManager` y la navegación en `SceneController`. Incluye panel de confirmación reutilizable para sobrescribir o borrar. Funciona en dos contextos: pantalla completa en el Menú Principal (con `_menuPrincipalUI` asignado) o popup en el menú de pausa (con `_menuPrincipalUI` null). |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Abrir(SlotModo modo)` | `void` | Activa el panel, establece el modo (Guardar / Cargar) y refresca los cinco slots leyendo el disco. |
| `CerrarPanel()` | `void` | Cierra el panel y notifica a `_menuPrincipalUI` para que reactive los botones del menú. Si `_menuPrincipalUI` es null (popup de pausa), solo desactiva el panel. |
| `OnGuardar(SlotUI slotUI)` | `void` | Guarda en el slot. Si está ocupado, pide confirmación antes de sobrescribir. |
| `OnCargar(SlotUI slotUI)` | `void` | Carga la partida del slot y navega al mapamundi. Solo opera si el slot está ocupado. |
| `OnBorrar(SlotUI slotUI)` | `void` | Pide confirmación y elimina el archivo `.db`. Refresca la UI tras el borrado. |

### Campos SerializeField (Día 11)

| Campo | Tipo | Descripción |
|---|---|---|
| `_menuPrincipalUI` | `MenuPrincipalUI` | Opcional. Asignar solo en la escena Menú Principal. Si es null, el panel actúa como popup. |

### Comportamiento Escape (Día 11)

`Update()` detecta `KeyCode.Escape`: si el panel de confirmación está abierto lo cierra primero; si no, llama a `CerrarPanel()`.

### Dependencias

- `SlotUI` — filas visuales de cada slot; se inicializan en `Abrir`.
- `SlotData` — metadatos que se construyen al escanear el disco.
- `MenuPrincipalUI` — notificada al cerrar el panel (opcional; null en modo popup).
- `SaveManager` — ejecuta el guardado.
- `LoadManager` — ejecuta la carga.
- `SceneController` — navega al mapamundi tras cargar.
- `TextMeshProUGUI` (TMPro) — texto del panel de confirmación.
- `UnityEngine.UI.Button` — botón Cerrar y botones Sí/No del panel de confirmación.
- `Mono.Data.Sqlite` — conexión temporal de solo lectura para leer metadatos.
- `System.IO` — `File.Exists`, `File.Delete`, `Path.Combine`.

### Setup en Inspector

Asignar: array de 5 `SlotUI`, `_botonCerrar`, `_panelConfirmacion` (GameObject), `_textoConfirmacion` (TextMeshProUGUI), `_botonConfirmarSi`, `_botonConfirmarNo`. En el Menú Principal: asignar también `_menuPrincipalUI`.

---

## SlotModo

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/PantallaSlotsUI.cs` |
| **Tipo** | `enum` |
| **Módulo** | Guardado y carga — UI |
| **Descripción** | Determina si el panel de slots se abrió para guardar o para cargar una partida. |

### Valores

| Valor | Descripción |
|---|---|
| `Guardar` | El jugador quiere guardar la partida actual en un slot. |
| `Cargar` | El jugador quiere cargar una partida guardada anteriormente. |

---

### Tests creados

| Archivo | Modo | Tests | Estado |
|---|---|---|---|
| `Assets/Tests/EditMode/SlotDataTests.cs` | Edit Mode | 2 (SlotData vacío por defecto, nombre contiene número de slot) | ✅ pasan |
| `Assets/Tests/PlayMode/MemoriaComercialPNJDAOTests.cs` | Play Mode | 6 (inserción, sobrescritura, precio válido <7 días, precio caducado ≥7 días, registro inexistente, eliminación de flota) | ✅ pasan |

Todos los tests son autónomos: usan clases locales (`SlotDataLocal`, `TestableMemoriaDAO`) y una base de datos temporal en disco con `Path.GetTempPath()` + `Guid.NewGuid()`. No dependen de ningún `MonoBehaviour` ni del proyecto principal.

### Correcciones de infraestructura

- `sqlite3.dll` reemplazado: binario de mayo 2015 → **SQLite 3.53.0** (abril 2026, 3.2 MB).
- `Mono.Data.Sqlite.dll` `.meta` corregido: CPU `AnyCPU`, Editor + Win64 habilitados.
- `sqlite3.dll` `.meta` corregido: `isPreloaded: 1`, CPU `x86_64`, Editor + Win64 habilitados.
- `MemoriaComercialPNJDAO` refactorizado: almacena `SqliteConnection` directamente en lugar de `DatabaseManager`; constructor público secundario añadido para facilitar los tests sin dependencia de `MonoBehaviour`.

---

---

## DÍA 11 — Integración UI de guardado/carga en el flujo del juego

Sesión dedicada a conectar `PantallaSlotsUI` al flujo completo del juego: Menú Principal y menú de pausa de Ciudad/Mapamundi. Se han creado scripts de editor para automatizar el montaje del panel y reparar estados incorrectos en escena. Se ha identificado un bug crítico de scope (solo se persiste la ciudad activa en escena al guardar/cargar) que se difiere al Día 12.

### Cambios en scripts existentes

| Script | Cambios |
|---|---|
| `MenuPrincipalUI` | `CargarPartida()` ya no es stub: abre `PantallaSlotsUI` en modo Cargar. Nuevo método `MostrarMenuPrincipal()`. Nuevos campos `_pantallaSlotsUI` y `_panelMenuPrincipal`. |
| `MenuPausa` | Botones Guardar y Cargar abren el popup de slots sin cerrar el menú ni reanudar el tiempo. Nuevos campos `_botonGuardar`, `_botonCargar`, `_pantallaSlotsUI`. |
| `PantallaSlotsUI` | Nuevo método público `CerrarPanel()` que notifica a `MenuPrincipalUI` si está asignado. Nuevo campo `_menuPrincipalUI` (opcional). `Update()` cierra con Escape: primero el panel de confirmación si está abierto, luego el panel completo. |
| `SlotUI` | `Inicializar()` recibe nuevo parámetro `SlotModo modo`. Visibilidad de botones según modo. Colores aplicados con `Color32`. Texto "— Vacío —" en gris para slots vacíos. Activación explícita de `gameObject` y `BotonesSlot` al inicio. |

### Scripts de editor creados

| Menú Unity | Script | Función |
|---|---|---|
| TFG → Generar Panel de Slots (Pantalla Completa) | `PantallaSlotsEditorSetup.cs` | Genera la jerarquía completa del panel (700×520) en modo pantalla completa con overlay oscuro. Cablea todos los `[SerializeField]` de `PantallaSlotsUI` y `SlotUI` via reflexión. |
| TFG → Generar Panel de Slots (Popup) | `PantallaSlotsEditorSetup.cs` | Igual que el anterior pero con `PanelContenedor` de 600×460 para uso como popup en el menú de pausa. |
| TFG → Reparar → Activar BotonesSlot en escena activa | `ActivarBotonesSlot.cs` | Busca todos los GameObjects "BotonesSlot" en la escena activa y los activa junto con sus hijos directos. Ejecutar en cada escena afectada. |
| TFG → Reparar → Colorear todos los textos del PanelSlots | `RepararColoresSlots.cs` | Aplica colores medievales (dorado, gris, blanco) a todos los `TMP_Text` dentro de los paneles de slots según el nombre del GameObject. |

### Bug identificado en Día 11 — ✅ Resuelto en Día 12

`LoadManager.RestaurarMercados()` buscaba `MarketManager` en escena en el momento de la carga, antes de cambiar de escena. Si no había `MarketManager` activo, la restauración se omitía silenciosamente. Resuelto en el Día 12 mediante el refactor de persistencia multi-ciudad: `LoadManager` ya no depende de `MarketManager`; restaura `GameManager.MercadosPorCiudad` directamente.

---

## DÍA 12 — Refactor de persistencia multi-ciudad (bug crítico resuelto)

Bug identificado al final del Día 11: `SaveManager` y `LoadManager` solo persistían el mercado de la ciudad activa en escena. El estado del resto de ciudades se perdía al guardar o cargar.

### Cambios implementados

- **`EstadoPartida`** — nuevo POCO `[Serializable]` que agrupa todos los diccionarios del estado vivo del mundo. Propiedad exclusiva de `GameManager`.
- **`GameManager`** — nuevo dueño del estado de todos los mercados. Expone `MercadosPorCiudad`, `InicializarMercadosDesdeAssets`, `RegistrarMercadoCiudad`, `LimpiarMercados`, `NotificarMercadoActualizado` y el evento `OnMercadoCiudadActualizado`.
- **`MarketManager`** — refactorizado a "vista" de la ciudad activa. Ya no posee el estado; lee y escribe contra `GameManager.MercadosPorCiudad`. API pública conservada al 100 % (sin cambios en `OficinaComercial`, `MercadoUI` ni `MarketRowUI`).
- **`SeleccionCiudadUI`** — llama a `GameManager.InicializarMercadosDesdeAssets` antes de cargar la escena, garantizando inicialización eager de todas las ciudades.
- **`SaveManager`** — itera `GameManager.MercadosPorCiudad` y guarda todas las ciudades. Eliminada la dependencia de `FindAnyObjectByType<MarketManager>`.
- **`LoadManager`** — llama a `GameManager.LimpiarMercados()` y repuebla el diccionario con todas las ciudades de la BD. Eliminada la dependencia de `FindAnyObjectByType<MarketManager>`.
- **`EstadoMercadoCiudadDAO`** — firma de `GuardarTodoElMercado` actualizada a `(int idCiudad, IReadOnlyList<EntradaMercado> entradas)`. Nuevo método `ObtenerIdsCiudadesConMercado()` → `List<int>`.
- **Fix del tick diario** — el refresco en directo de la UI del mercado ahora funciona correctamente al operar contra el diccionario global.

---

## TO-DO Día 13

### Tests automatizados de persistencia multi-ciudad

- [ ] `Assets/Tests/PlayMode/SaveLoadMultiCiudadTests.cs` — registrar 2 mercados en `GameManager`, modificar stocks, guardar en slot temporal, vaciar el diccionario, cargar, comprobar que ambos mercados se restauran correctamente.
- [ ] Test del tick diario con dos ciudades en memoria: verificar que producción/consumo de cada ciudad opera sobre su propio diccionario sin contaminar a la otra.

### Mejoras menores de UI de slots (heredado del Día 11)

- [ ] Reemplazar el carácter `✕` del botón cerrar por un sprite o símbolo soportado por LiberationSans (warning persistente en Console).
- [ ] Corregir `PantallaSlotsEditorSetup` para que busque el Canvas en la escena activa, no en DontDestroyOnLoad.
- [ ] Renombrar `BotonGuardar`/`BotonCargar` de los slots como `BotonGuardarSlot`/`BotonCargarSlot` y los del menú de pausa como `BotonGuardarPausa`/`BotonCargarPausa` para evitar ambigüedad en la jerarquía.

### Refactor de MenuPrincipalUI

- [ ] Encapsular la lógica de mostrar/ocultar paneles en un único método `MostrarPanel(...)` para eliminar el patrón de `SetActive` distribuido.

### Revisión técnica del LoadManager

- [ ] Sustituir `Resources.FindObjectsOfTypeAll<BienData>()` por una vía determinista (`Resources.LoadAll<BienData>(...)` o catálogo serializado en `GameManager`). El método actual funciona pero puede devolver assets en estados raros del editor.

### Inicio Semana 3 — PNJs comerciantes

- [ ] Definir `FlotaPNJData` real (promover el DTO existente a clase de runtime).
- [ ] Rellenar `EstadoPartida.FlotasPorId` al cargar partida o al spawnear PNJs.
- [ ] Crear `FlotaManager` o equivalente como vista de las flotas activas en el mapamundi.

---

## DÍA 14 — Comportamiento de PNJs: infraestructura base

Inicio de la Semana 3. Se implanta toda la infraestructura necesaria para que las flotas PNJ comerciantes existan en el mundo y avancen su lógica día a día, aunque sus estados internos son aún esqueleto (sin lógica real de compra/venta).

### Scripts nuevos

#### `EstadoFlotaPNJ` — `Assets/Scripts/PNJ/EstadoFlotaPNJ.cs`

Enum que define los tres estados posibles de la máquina de estados de una flota PNJ comerciante.

| Valor | Significado |
|---|---|
| `EnPuerto` | La flota está atracada en una ciudad, lista para comerciar o elegir destino. |
| `Viajando` | La flota navega hacia una ciudad destino. |
| `Comerciando` | La flota está ejecutando una operación de compra o venta en puerto. |

---

#### `FlotaRuntimeData` — `Assets/Scripts/PNJ/FlotaRuntimeData.cs`

POCO de runtime que representa el estado vivo de una flota PNJ durante la simulación. No hereda de `MonoBehaviour`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Id` | `int` (get) | Identificador único de la flota. |
| `NombrePropietario` | `string` (get) | Nombre del PNJ dueño de la flota (ej. "Comerciante Hans"). |
| `CiudadOrigenId` | `int` | Ciudad de partida de la flota en la sesión actual. |
| `CiudadDestinoId` | `int` | Ciudad hacia la que viaja la flota. `-1` si no hay destino asignado. |
| `EstadoActual` | `EstadoFlotaPNJ` | Estado activo en la máquina de estados. Modificable por `FlotaManager.CambiarEstado`. |
| `RutaActual` | `List<int>` | Secuencia de `IdCiudad` que describe el itinerario planificado. |
| `Carga` | `Dictionary<int, int>` | Inventario de la bodega: clave `id_bien`, valor unidades. |
| `FlotaRuntimeData(int id, string nombrePropietario)` | Constructor | Inicializa la flota con id y nombre; estado inicial `EnPuerto`, sin destino ni carga. |
| `TieneCarga()` | `bool` | Devuelve `true` si la bodega contiene al menos un bien con cantidad > 0. |

---

#### `FlotaManager` — `Assets/Scripts/PNJ/FlotaManager.cs`

| Campo | Valor |
|---|---|
| **Tipo** | `MonoBehaviour` (singleton persistente) |
| **Módulo** | PNJs — Comportamiento |
| **Descripción** | Gestor singleton de flotas PNJ activas en el mundo. Es la única puerta de entrada para registrar, consultar y cambiar el estado de las flotas comerciantes. Se suscribe a `SimulacionTiempo.OnNuevoDia` para avanzar un tick de comportamiento en todos los controladores registrados. |

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static FlotaManager` (get) | Punto de acceso global al gestor. |
| `RegistrarFlota(FlotaRuntimeData flota)` | `void` | Añade una flota al estado de partida y crea su `ComerciantePNJController`. Si ya existe una flota con el mismo `Id`, la sobreescribe. |
| `ObtenerFlota(int id)` | `FlotaRuntimeData` | Devuelve los datos de runtime de la flota indicada, o `null` si no existe. |
| `ObtenerTodasLasFlotas()` | `IReadOnlyCollection<FlotaRuntimeData>` | Devuelve todas las flotas PNJ activas en el mundo. |
| `CambiarEstado(int flotaId, EstadoFlotaPNJ nuevoEstado)` | `void` | Realiza una transición de estado en la flota indicada. No actúa si la flota no existe. |
| `TickTodosLosControladores()` | `void` | Avanza un día de simulación en todos los controladores de comportamiento registrados. Suscrito a `SimulacionTiempo.OnNuevoDia`. |
| `SpawnFlotasPNJIniciales(IReadOnlyList<CiudadData> ciudades)` | `void` | Crea y registra exactamente 2 comerciantes de prueba (Hans id 1001, Klaus id 1002) usando las dos primeras ciudades del catálogo. |

**Dependencias:** `GameManager.EstadoPartida`, `SimulacionTiempo.OnNuevoDia`, `ComerciantePNJController`, `FlotaRuntimeData`.

---

#### `ComerciantePNJController` — `Assets/Scripts/PNJ/ComerciantePNJController.cs`

| Campo | Valor |
|---|---|
| **Tipo** | Clase pura C# (no `MonoBehaviour`) |
| **Módulo** | PNJs — Comportamiento |
| **Descripción** | Controlador de comportamiento de una flota PNJ comerciante. Instanciado por `FlotaManager` al registrar cada flota. Avanza un paso de la máquina de estados por cada llamada a `Tick`. |

| Miembro | Tipo | Descripción |
|---|---|---|
| `ComerciantePNJController(FlotaRuntimeData flota, FlotaManager manager)` | Constructor | Vincula el controlador a su flota y al gestor central. |
| `Tick()` | `void` | Avanza la lógica de comportamiento un día de juego delegando en el método privado del estado activo. Llamado por `FlotaManager.TickTodosLosControladores`. |

**Estados internos (esqueleto — lógica real pendiente Día 15):**

| Método privado | Estado | Comportamiento actual |
|---|---|---|
| `TickEnPuerto()` | `EnPuerto` | Log de traza. Día 15: consultar `MemoriaComercialPNJDAO` con retraso de 7 días, elegir bien rentable, comprar simulado y decidir ciudad destino → transición a `Viajando`. |
| `TickViajando()` | `Viajando` | Log de traza. Día 15: decrementar días de viaje; al llegar → transición a `Comerciando`. |
| `TickComerciando()` | `Comerciando` | Log de traza. Día 15: vender carga si es rentable → transición a `EnPuerto`. |
| `CambiarEstado(EstadoFlotaPNJ)` | — | Delega en `FlotaManager.CambiarEstado` para centralizar el log de transiciones. |

---

### Scripts modificados

#### `EstadoPartida` — `Assets/Scripts/Core/EstadoPartida.cs`

- Añadido `FlotasPorId` (`Dictionary<int, FlotaRuntimeData>`): diccionario de todas las flotas PNJ activas en el mundo, indexado por `Id`. `FlotaManager` opera sobre él a través de `GameManager.EstadoPartida`.

#### `GameManager` — `Assets/Scripts/Core/GameManager.cs`

- Añadida propiedad pública `EstadoPartida` (`EstadoPartida`, get): expone el estado de partida para que `FlotaManager` pueda leer y escribir `FlotasPorId` sin duplicar el diccionario.
- `InicializarMercadosDesdeAssets` llama a `FlotaManager.Instance.SpawnFlotasPNJIniciales(_ciudadesDisponibles)` al finalizar la inicialización de mercados, arrancando las flotas PNJ al inicio de cada partida nueva.

### Fixes de infraestructura

- Añadidas `Mono.Data.Sqlite.dll` y `System.Data.dll` a `Assets/Plugins/` para resolver errores de compilación tras formateo del equipo de desarrollo (las DLLs no estaban en el repositorio).

---

## TO-DO Día 15

- [ ] **`TickEnPuerto` real** — consultar `MemoriaComercialPNJDAO` con retraso de 7 días, elegir el bien más rentable, simular compra y asignar ciudad destino → transición `EnPuerto → Viajando`.
- [ ] **`TickComerciando` real** — vender carga si el precio actual supera el precio de compra conocido → transición `Comerciando → EnPuerto`.
- [ ] **`TickViajando` real** — decrementar contador de días de viaje; al llegar a destino → transición `Viajando → Comerciando`.
- [ ] **SpawnerInicial configurable** — decidir cuántas flotas PNJ se crean en función del número de ciudades disponibles (no hardcodeado a 2).
- [ ] **Persistencia de flotas PNJ en SQLite** — diferido a Día 19; hasta entonces las flotas se recrean desde `SpawnFlotasPNJIniciales` al iniciar partida.
- [ ] Implementar la máquina de estados de comerciantes (EnPuerto, Viajando, Comerciando, Huyendo) según `sesion_planificacion_release.md`.

---

## DÍA 15 — PNJs comerciantes: tick diario y memoria comercial global

Lógica completa de los tres estados de la máquina de estados de los PNJs comerciantes. Se implementa el snapshot global de precios (idFlota=0) que sirve como fuente de verdad con retraso de 7 días para las decisiones de compra.

### Scripts modificados

#### `ComerciantePNJController` — `Assets/Scripts/PNJ/ComerciantePNJController.cs`

| Miembro | Cambio |
|---|---|
| `TickEnPuerto()` | Lógica completa: consulta snapshot global, selecciona bien con mayor margen, compra simulada afectando `StockActual`, inicia viaje. Sin ruta rentable → viaje en vacío a ciudad aleatoria. |
| `TickViajando()` | Decrementa `_diasRestantesViaje` cada tick; al llegar actualiza `CiudadOrigenId` y transiciona a `Comerciando`. |
| `TickComerciando()` | Vende siempre al llegar (con o sin pérdidas) para liberar bodega. Modifica `StockActual` directamente y notifica via `NotificarMercadoActualizado`. |
| `IniciarViaje()` | Método público para arrancar viaje desde `TickEnPuerto`, registra precios de compra. |
| `ObtenerSnapshotGlobal()` | Helper privado — devuelve memoria global (idFlota=0). |
| `ObtenerEntrada()` | Helper privado — busca `EntradaMercado` por ciudad y bien sin pasar por MarketManager. |
| Constructor | Acepta `MemoriaComercialPNJDAO memoriaDAO` como tercer parámetro. |

#### `FlotaManager` — `Assets/Scripts/PNJ/FlotaManager.cs`

| Miembro | Cambio |
|---|---|
| `RefreshMemoriaGlobal()` | Nuevo método privado — recorre `MercadosPorCiudad` y guarda snapshot global con `idFlota=0` cada 7 días. |
| `ObtenerMemoriaDAO()` | Helper privado lazy — crea `MemoriaComercialPNJDAO` la primera vez que se necesita, garantizando conexión SQLite abierta. |
| `TickTodosLosControladores()` | Incrementa `_diasDesdeUltimoRefresh`; llama `RefreshMemoriaGlobal()` cada 7 días. |
| `SpawnFlotasPNJIniciales()` | Llama `RefreshMemoriaGlobal()` al finalizar para garantizar snapshot desde el día 1. |

#### `MemoriaComercialPNJDAO` — `Assets/Scripts/Database/MemoriaComercialPNJDAO.cs`

| Miembro | Cambio |
|---|---|
| `GuardarMemoria()` | Añadido parámetro `int idCiudad` — persiste la ciudad donde se observó el precio. |
| `ObtenerMemoriaDeFlota()` | SELECT incluye `id_ciudad`, se hidrata en `dto.IdCiudad`. |

#### `MemoriaComercialPNJDto`

- Añadida propiedad `public int IdCiudad { get; set; }`.

#### `DatabaseManager` — `Assets/Scripts/Database/DatabaseManager.cs`

- Tabla `MemoriaComercialPNJ` redefinida sin FK a `Flota` ni `Bien`, con `id_ciudad INTEGER NOT NULL DEFAULT 0` y PK `(id_flota, id_bien, id_ciudad)`.

#### `EstadoPartida` — `Assets/Scripts/Core/EstadoPartida.cs`

- Añadido `public int DiaJuego = 1` — día actual de simulación usado por el sistema PNJ.

#### `SeleccionCiudadUI` — `Assets/Scripts/UI/SeleccionCiudadUI.cs`

- Llama a `DatabaseManager.Instance.InicializarSlot(0)` antes de `InicializarMercadosDesdeAssets` para garantizar conexión SQLite en partida nueva. Slot 0 = partida temporal en curso.

---

## TO-DO Día 16

- [x] Tilemap hexagonal del mapamundi — crear grid hex navegable con tiles tipados (mar, tierra, peligro).
- [x] Marcadores visuales de ciudades sobre el tilemap. (PARCIAL — GameObjects creados, sin sprite visual todavía)
- [x] Cámara del mapamundi con zoom y desplazamiento por bordes.

---

## TO-DO Día 17

- [x] A* hexagonal (`RutaCalculadorTilemap.cs`) con coordenadas cube, heurística hex y cola de prioridad (MinHeap propio — PriorityQueue no disponible en .NET Standard 2.1)
- [x] Tests de conectividad entre las 15 pares de ciudades — todas PASS
- [x] `FlotaRuntimeData` ampliado con `PosicionActual`, `CasillaDestino`, `RutaActualTilemap`, `IndiceWaypointActual`
- [x] Migración BD flotas con columnas `posicion_actual_x`, `posicion_actual_y`, `casilla_destino_x`, `casilla_destino_y`
- [x] `FlotaIconoMapamundi.cs` — movimiento continuo con `Vector3.MoveTowards`, flip de sprite, respeta pausa y `VelocidadActual`
- [x] Coordenadas reales de las 6 ciudades corregidas en `CiudadDAO` y `CiudadesEditorSetup`
- [x] `SpawnIconosFlotas()` en `MapamundiController` — crea iconos de flota al cargar mapamundi
- [x] Ciclo completo PNJ funcional: comprar → viajar por mar → llegar → vender → repetir

---

## TO-DO Día 18

- [x] Almacén ciudad del jugador — tabla BD, DAO, integración GameManager/LoadManager/SaveManager
- [x] Transferencias internas Mercado ↔ Almacén Ciudad ↔ Bodega Barco en MarketRowUI
- [x] A* activado con heurística real + `CalcularRutaConRuido` para PNJs
- [x] Refactor completo ComerciantePNJController: eliminar snapshot/MemoriaComercialPNJDAO, nuevo `EstimarPrecioMercado` con `InteligenciaComercial`, historial últimas 2 ciudades, límite 2 flotas por ruta
- [x] FlotaManager ampliado a 18 comerciantes, eliminado `RefreshMemoriaGlobal`
- [x] `FlotaRuntimeData.InteligenciaComercial` añadido
- [x] BienesEditorSetup ampliado a 12 bienes

---

## DÍA 18 — Almacén ciudad, refactor PNJ comercial e IA por inteligencia

### Resumen
Día dedicado a tres bloques: (1) implementación del almacén de ciudad del jugador como capa de persistencia independiente de la bodega del barco; (2) mejora de la UI del mercado para soportar transferencias internas entre tres orígenes; (3) refactorización profunda del sistema de PNJs comerciantes para eliminar la dependencia del snapshot SQLite y reemplazarlo por lectura directa del mercado en memoria con ruido proporcional a la inteligencia individual de cada comerciante.

### Clases nuevas

#### AlmacenCiudadDAO
| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Database/AlmacenCiudadDAO.cs` |
| **Tipo** | `class` (DAO puro) |
| **Módulo** | Guardado y carga |
| **Descripción** | Gestiona la tabla `AlmacenCiudadJugador` en SQLite. Opera con `@param` nombrados para evitar inyección. |

| Miembro | Tipo | Descripción |
|---|---|---|
| `GetCantidad(int idCiudad, int idBien)` | `int` | Devuelve las unidades almacenadas de un bien en una ciudad. |
| `SetCantidad(int idCiudad, int idBien, int cantidad)` | `void` | Establece (INSERT OR REPLACE) la cantidad exacta. |
| `Incrementar(int idCiudad, int idBien, int delta)` | `void` | Suma `delta` al stock. Lanza `InvalidOperationException` si el resultado sería negativo. |
| `GetTodosPorCiudad(int idCiudad)` | `Dictionary<int,int>` | Devuelve todos los bienes almacenados en una ciudad como mapa idBien→cantidad. |
| `LimpiarCiudad(int idCiudad)` | `void` | Elimina todas las filas de la ciudad indicada. |

### Clases modificadas

#### GameManager (Día 18)
Añadidos para el almacén de ciudad del jugador:

| Miembro | Tipo | Descripción |
|---|---|---|
| `InyectarAlmacenCiudadDAO(AlmacenCiudadDAO dao)` | `void` | Inyecta el DAO desde `LoadManager` durante la inicialización. |
| `GetCantidadAlmacenCiudad(int idCiudad, int idBien)` | `int` | Lee del diccionario en memoria. Devuelve 0 si no existe entrada. |
| `SetCantidadAlmacenCiudad(int idCiudad, int idBien, int cantidad)` | `void` | Escribe en el diccionario en memoria sin persistir. |
| `ModificarAlmacenCiudad(int idCiudad, int idBien, int delta)` | `void` | Valida que el resultado no sea negativo y persiste inmediatamente via DAO. |
| `GetAlmacenCiudad(int idCiudad)` | `Dictionary<int,int>` | Devuelve el mapa idBien→cantidad del almacén de esa ciudad. |
| `LimpiarAlmacenCiudades()` | `void` | Vacía el diccionario en memoria. Llamado por `LoadManager` antes de cargar. |
| `CargarAlmacenCiudadesDesdeDAO()` | `void` | Repuebla el diccionario en memoria desde `AlmacenCiudadDAO`. |
| `InicializarMercadosCiudades(IEnumerable<CiudadData>)` | `void` | Método extraído de `InicializarMercadosDesdeAssets` para inicializar mercados sin disparar el spawn de PNJs (evita recursión). |

#### OficinaComercial (Día 18)
| Miembro | Tipo | Descripción |
|---|---|---|
| `OrigenDestino` | `enum` | `Mercado`, `AlmacenCiudad`, `BodegaBarco`. |
| `Transferir(BienData bien, int cantidad, OrigenDestino origen, OrigenDestino destino)` | `void` | Mueve unidades entre almacén ciudad y bodega sin transacción de dinero. `Mercado` no es origen ni destino válido. |

#### MarketRowUI (Día 18)
Reescritura completa para soportar columnas ciclables:

| Miembro | Tipo | Descripción |
|---|---|---|
| `_textoEtiquetaIzq / Der` | `TMP_Text` | Etiquetas de cabecera de cada columna. |
| `_btnCiclarIzq / Der` | `Button` | Botones de flecha para ciclar el origen/destino de cada columna. |
| `CiclarColumnaIzq()` | `void` | Cicla la columna izquierda entre los tres orígenes. |
| `CiclarColumnaDer()` | `void` | Cicla la columna derecha entre los tres orígenes. |
| `Refrescar()` | `void` | Actualiza stocks y estado `interactable` de botones según columnas activas. |

#### MercadoUI (Día 18)
`RefrescarCabecera()` actualizado: muestra `"Bodega: X / Almacén: Y"` en lugar de un único valor.

#### RutaCalculadorTilemap (Día 18)
| Miembro | Tipo | Descripción |
|---|---|---|
| `CalcularRuta(Vector3Int origen, Vector3Int destino)` | `List<Vector3Int>` | A* determinista. Heurística real activada (multiplicador `1f`). |
| `CalcularRutaConRuido(Vector3Int origen, Vector3Int destino)` | `List<Vector3Int>` | A* con factor aleatorio ±15% en la heurística para que las rutas PNJ no sean idénticas. |

#### ComerciantePNJController (Día 18)
Refactorización profunda — eliminados `_memoriaDAO`, `ObtenerSnapshotGlobal`, `EstimarPrecio(MemoriaComercialPNJDto)`. Nuevo comportamiento:

| Miembro | Tipo | Descripción |
|---|---|---|
| `_historialCiudades` | `Queue<int>` | Últimas 2 ciudades visitadas. Evita ciclos A→B→A→B. |
| `EstimarPrecioMercado(int idCiudad, int idBien)` | `float` (privado) | Lee el precio real del mercado en memoria y aplica ruido con `Lerp(0.40, 0.05, InteligenciaComercial)`. |
| PASO 1 | — | Itera directamente sobre el catálogo (`for idBien = 1..N`) y todas las ciudades. Filtra: ciudad ya visitada (historial), ≥ 2 flotas en ruta. Selecciona el destino con mayor margen. |

#### FlotaManager (Día 18)
| Miembro | Tipo | Descripción |
|---|---|---|
| `ContarFlotasEnRutaHacia(int idCiudad, int idBien)` | `int` | Cuenta flotas en estado `Viajando` con ese destino y ese bien cargado. |
| `SpawnFlotasPNJIniciales` | — | Ampliado a 18 comerciantes (IDs 1001–1018), 3 por ciudad. Elimina la llamada a `RefreshMemoriaGlobal`. |
| Eliminados | — | `RefreshMemoriaGlobal`, `_diasDesdeUltimoRefresh`, `_memoriaDAO`, `ObtenerMemoriaDAO`. |

#### FlotaRuntimeData (Día 18)
| Miembro | Tipo | Descripción |
|---|---|---|
| `InteligenciaComercial` | `float` (get) | Nivel de habilidad comercial entre 0.1 y 1.0, asignado aleatoriamente en el constructor. Controla la precisión de `EstimarPrecioMercado`. |

#### BienesEditorSetup (Día 18)
Ampliado de 5 a 12 bienes: añadidos Sal, Cera (primarios), Tela, Herramientas, Cerveza (intermedios), Especias, Seda (avanzados). Informa en consola cuántos se crearon vs. ya existían.

### TO-DOs abiertos tras el Día 18
- Persistencia de flotas PNJ entre guardado y carga (diferida al Día 19).
- `MarcadorCiudad`: detección de llegada de flota del jugador + cambio de sprite (diferido a cuando existan flotas del jugador).
- Sistema de producción del jugador: edificios que consumen materias primas y producen manufacturados (Días 23-24).
- Barcos reales con capacidad de carga y velocidad variable conectados a flotas PNJ.
- `MemoriaComercialPNJDAO` queda en el proyecto pero ya no la usa `ComerciantePNJController` — evaluar si mantener para estadísticas o eliminar.
- `FindFirstObjectByType` deprecado en Unity 6 — reemplazar por `FindAnyObjectByType` en `ComerciantePNJController` (3 llamadas) y `DebugCasillaHex`.

---

## TO-DO Días 23-24 (fin de semana 16-17 mayo)

- [ ] Día 23 — Vista de ciudad: tilemap 2D con pack de assets hexagonal (mismo que mapamundi) + edificios en capa superior (puerto, taberna, astillero, mercado como GameObjects clicables). Una plantilla base completa para una ciudad.
- [ ] Día 24 — Adaptar plantilla a las otras 5 ciudades cambiando tiles según geografía (Lübeck más verde/agua, Barcelona más árido/mediterráneo, etc.)

---

## RutaCalculadorTilemap

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Mapamundi/RutaCalculadorTilemap.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Mundo y navegación |
| **Descripción** | Calcula rutas A* hexagonales sobre el tilemap Pointy Top del mapamundi. Adjuntar al GameObject `RutaCalculador` en la escena Mapamundi y asignar el Tilemap en el Inspector. Usa coordenadas cube internamente con conversión offset↔cube para Pointy Top odd-r. Sprites transitables: `loonapix_17783290501031121577` (mar abierto, coste 1.0), `Costa` (aguas costeras, coste 1.2), `medieval_openCastle_0` (ciudad, coste 1.0). Cualquier otro sprite es intransitable. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `CalcularRuta(Vector3Int origen, Vector3Int destino)` | `List<Vector3Int>` | A* determinista. Devuelve la lista de casillas offset desde origen hasta destino (ambos incluidos). Lista vacía si no hay ruta; lista con un elemento si origen == destino. |
| `CalcularRutaConRuido(Vector3Int origen, Vector3Int destino)` | `List<Vector3Int>` | A* con factor aleatorio ±15% en la heurística. Usar desde `ComerciantePNJController` para que las rutas PNJ no sean idénticas entre sí. |
| `GetVecinosDebug(Vector3Int pos)` | `List<Vector3Int>` | **Temporal de debug** — devuelve los vecinos transitables de una casilla y loguea sprite y transitable por cada uno de los 6 vecinos. Eliminar antes del freeze (Día 32). |

---

## FlotaIconoMapamundi

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Mapamundi/FlotaIconoMapamundi.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Mundo y navegación |
| **Descripción** | Representa visualmente una flota PNJ en el mapamundi. Se adjunta a un GameObject creado por código en `SpawnIconosFlotas()`. Mueve el sprite con `Vector3.MoveTowards` siguiendo los waypoints de `FlotaRuntimeData.RutaActualTilemap`. Respeta la pausa y `VelocidadActual` de `SimulacionTiempo`. Aplica `flipX` al `SpriteRenderer` según la dirección X del movimiento. Al llegar al destino fuerza la transición a `Comerciando` en `FlotaRuntimeData`. Si la flota está en estado `Viajando` pero sin ruta, la recalcula desde la posición actual. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Flota` | `FlotaRuntimeData` | La flota que representa este icono. Asignar antes de llamar a `InicializarIcono`. |
| `Inicializar(Tilemap t, RutaCalculadorTilemap r)` | `void` | Asigna dependencias de tilemap y calculador cuando el icono se crea por código en lugar de desde el Inspector. |
| `InicializarIcono()` | `void` | Inicializa el `SpriteRenderer` y resetea el índice de waypoint. Llamar desde `MapamundiController` tras asignar ruta y posición. |
| `CasillaOrigenDesdeFlota()` | `Vector3Int` | Devuelve la casilla offset de la ciudad origen de la flota consultando `GameManager.CiudadesDisponibles`. Devuelve `Vector3Int.zero` si no se encuentra. |

---

## MapamundiCamara

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Mapamundi/MapamundiCamara.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Mundo y navegación |
| **Descripción** | Controla la cámara del mapamundi. Zoom con rueda del ratón con límite dinámico basado en orthographicSize para no mostrar área fuera del mapa. Desplazamiento con WASD/flechas, arrastre con clic izquierdo (respeta colisiones de ciudades y flotas) o clic medio, y scroll por bordes de pantalla. ClampPosicion() centralizado aplica límites a todos los métodos de movimiento. Adjuntar a la Main Camera de la escena Mapamundi. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `MinZoom` | `float` | Tamaño ortográfico mínimo (más zoom). Default: 3. |
| `MaxZoom` | `float` | Tamaño ortográfico máximo (menos zoom). Default: 15. |
| `VelocidadZoom` | `float` | Velocidad de zoom al girar la rueda. Default: 2. |
| `ZonaBorde` | `float` | Píxeles desde el borde que activan el scroll. Default: 50. |
| `VelocidadScroll` | `float` | Velocidad de desplazamiento por bordes. Default: 5. |
| `VelocidadWASD` | `float` | Velocidad de desplazamiento con teclado. Default: 8. |
| `LimiteIzquierdo` | `float` | Límite izquierdo del mapa en world space. |
| `LimiteDerecho` | `float` | Límite derecho del mapa en world space. |
| `LimiteInferior` | `float` | Límite inferior del mapa en world space. |
| `LimiteSuperior` | `float` | Límite superior del mapa en world space. |

---

## Deuda técnica registrada (Día 15)

- [ ] **`BienData` sin ID numérico propio** — el índice de array se usa como `idBien` en `MemoriaComercialPNJ`. Es frágil si se reordenan assets en el Inspector. Solución: añadir campo `int idBien` serializado a `BienData` y actualizar DAOs. Pendiente antes del freeze (Día 32).
- [ ] **Persistencia de flotas PNJ entre guardado y carga** — diferida al Día 19. Hasta entonces las flotas se recrean desde `SpawnFlotasPNJIniciales` al iniciar partida nueva; los guardados anteriores al Día 15 no tienen flotas PNJ.
- [ ] **Viaje en vacío a ciudad aleatoria** — cuando no hay ruta rentable el comerciante elige destino al azar. En post-TFG mejorar a selección por cercanía geográfica usando pathfinding A* del Día 17.
- [ ] **`TileNavegable.cs` sin uso efectivo** — el pathfinding usa `GetSprite()` por nombre en lugar del campo `costeMovimiento`. Evaluar si se elimina o adapta antes del freeze (Día 32).
- [ ] **Scripts de debug de editor** (`DebugCasillaHex.cs`, `DebugTilemapBounds.cs`) — eliminar antes del freeze (Día 32).
- [ ] **`FindFirstObjectByType` deprecado en Unity 6** — reemplazar por `FindAnyObjectByType` en `ComerciantePNJController` (3 llamadas) y `DebugCasillaHex`. Registrado en Día 18.
- [ ] **`MemoriaComercialPNJDAO` sin uso** — `ComerciantePNJController` ya no la usa tras el refactor del Día 18. Evaluar si mantener para estadísticas o eliminar antes del freeze (Día 32).
- [ ] **Marcadores visuales de ciudad** — GameObjects creados pero sin sprite ni feedback visual. Diferido al Día 17.

---

## DÍA 19 — Persistencia de flotas PNJ + tests de persistencia

### Resumen
Día dedicado a dos bloques: (1) persistencia real del estado de las 18 flotas PNJ comerciantes entre sesiones de juego, guardando y cargando desde SQLite las tablas FlotaPNJ y CargaFlotaPNJ; (2) tests automatizados de Play Mode para el almacén ciudad del jugador y para la persistencia de flotas PNJ.

### TO-DO Día 19

- [x] Persistencia de flotas PNJ entre sesiones (GuardarFlotasPNJ + CargarFlotasPNJ)
- [x] Guard en SpawnFlotasPNJIniciales para evitar duplicados al cargar partida
- [x] Constructor secundario AlmacenCiudadDAO(SqliteConnection) para tests autónomos
- [x] Tablas FlotaPNJ y CargaFlotaPNJ en DatabaseManager (MigrarTablasFlotaPNJ)
- [x] Tests PlayMode SaveLoadAlmacenCiudadTests (3 tests — todos PASS)
- [x] Tests PlayMode SaveLoadFlotasPNJTests (2 tests — todos PASS)

---

### Clases modificadas

#### AlmacenCiudadDAO (Día 19)

| Miembro | Tipo | Descripción |
|---|---|---|
| `AlmacenCiudadDAO(SqliteConnection conexion)` | Constructor secundario | Para tests autónomos sin DatabaseManager. La propiedad privada `Conexion` resuelve entre `_conexionDirecta` y `_dbManager.Conexion`. |

#### DatabaseManager (Día 19)

| Miembro | Tipo | Descripción |
|---|---|---|
| `MigrarTablasFlotaPNJ()` | `void` (privado) | Crea `FlotaPNJ` y `CargaFlotaPNJ` con CREATE TABLE IF NOT EXISTS. Llamado desde `InicializarSlot()`. PosicionActual persiste como REAL x/y (Vector2). CasillaDestino persiste como INTEGER x/y/z (Vector3Int). InteligenciaComercial NO se persiste — se regenera aleatoriamente por sesión. |

#### SaveManager (Día 19)

| Miembro | Tipo | Descripción |
|---|---|---|
| `GuardarFlotasPNJ()` | `void` (privado) | Paso 6c del guardado. Itera `FlotaManager.ObtenerTodasLasFlotas()`, hace DELETE+INSERT en `FlotaPNJ` y DELETE+INSERT en `CargaFlotaPNJ` por cada flota. |

#### LoadManager (Día 19)

| Miembro | Tipo | Descripción |
|---|---|---|
| `CargarFlotasPNJ()` | `void` (privado) | Paso 7 de la carga. Lee `FlotaPNJ` y `CargaFlotaPNJ`, reconstruye cada `FlotaRuntimeData` y llama `FlotaManager.RegistrarFlota()`. `RutaActualTilemap` e `IndiceWaypointActual` no se restauran — se recalculan al entrar al mapamundi. |

#### FlotaManager (Día 19)

| Miembro | Tipo | Descripción |
|---|---|---|
| `SpawnFlotasPNJIniciales` | — | Guard añadido: `if (FlotasPorId.ContainsKey(id)) continue;` al inicio del bucle. Evita sobreescribir flotas ya restauradas desde BD al cargar partida. |

### Tests creados

| Archivo | Tests | Estado |
|---|---|---|
| `Assets/Tests/PlayMode/SaveLoadAlmacenCiudadTests.cs` | 3 (GuardarYCargar, NoPermiteNegativo, LimpiarSoloEsaCiudad) | ✅ todos PASS |
| `Assets/Tests/PlayMode/SaveLoadFlotasPNJTests.cs` | 2 (GuardarYCargar, SobreescribeCarga) | ✅ todos PASS |

Ambos archivos usan el patrón autónomo de MemoriaComercialPNJDAOTests: clases locales (TestableAlmacenCiudadDAO, FlotaRuntimeDataLocal) que replican la lógica SQL sin importar nada de Assembly-CSharp.

### TO-DOs abiertos tras el Día 19

- Combate naval auto-resolución (Día 20).
- Sistema de piratas con comportamiento de patrulla (Día 21).
- Sistema de producción del jugador: edificios que consumen materias primas y producen manufacturados (Días 23-24).
- `FindFirstObjectByType` deprecado en Unity 6 — reemplazar por `FindAnyObjectByType` en `ComerciantePNJController` (3 llamadas) y `DebugCasillaHex`.
- `MemoriaComercialPNJDAO` sin uso — evaluar mantener para estadísticas o eliminar antes del freeze (Día 32).
- `BienData` sin ID numérico propio — frágil con reordenación de assets en Inspector.
- [ ] **ZonaPeligro sin tile visual** — diferido al Día 21 (piratas).

---

## TO-DO Día 20

- [x] EstadoFlotaPNJ ampliado: Huyendo, Patrullando, Interceptando, HuyendoAPuerto, EsperandoEnPuerto
- [x] FlotaRuntimeData ampliado con stats de combate: IsPirata, VidaMax, VidaActual, FuerzaCanhones, VelocidadFlota, ManiobrabilidadFlota, HabilidadCapitan, NumBarcos, Tripulacion
- [x] Constructor secundario FlotaRuntimeData(id, nombre, esPirata) para flotas pirata
- [x] Métodos AplicarDanio, EstaDestruida, ResetearParaReabastecimiento en FlotaRuntimeData
- [x] ResultadoCombate.cs — clase inmutable con enum DesenlaceCombate
- [x] CombateNavalResolver.cs — resolución directa sin rondas: huida, rendición, combate
- [x] MapamundiController singleton + ComprobarProximidadCombate + TriggerCombate
- [x] FlotaIconoMapamundi conectado a ComprobarProximidadCombate al llegar a waypoint
- [x] 3 flotas pirata históricas en FlotaManager (Störtebeker, Gödeke Michels, Klaus Scheld)
- [x] Reabastecimiento semanal de piratas en FlotaManager (ReabastecerPiratas cada 7 días)
- [x] TickHuyendo y TickPatrullaPirata en ComerciantePNJController
- [x] Stubs de estados nuevos en Tick() para implementación Día 21

---

## DÍA 20 — Sistema de combate naval por auto-resolución

### Resumen

Día dedicado a implementar el sistema de encuentros navales entre piratas y comerciantes. Se crea la infraestructura de combate completa (resolver puro, resultado inmutable, detección por proximidad) sin interfaz gráfica; toda la resolución ocurre en lógica de juego y queda registrada en el log de consola. Se añaden también las tres flotas pirata históricas y el ciclo de reabastecimiento semanal.

### Clases nuevas

#### ResultadoCombate — `Assets/Scripts/Combate/ResultadoCombate.cs`

Clase inmutable que encapsula el resultado completo de un encuentro naval. Generada por `CombateNavalResolver.Resolver`; el llamador es el responsable de aplicar los cambios al estado del mundo.

| Miembro | Tipo | Descripción |
|---|---|---|
| `DesenlaceCombate` | `enum` | `PirataGana`, `ComercianteEscapa`, `ComercianteGana`, `Rendicion`, `Empate`. |
| `Desenlace` | `DesenlaceCombate` (get) | Desenlace final del encuentro. |
| `VidaFinalAtacante` | `float` (get) | Puntos de vida del pirata tras el combate. |
| `VidaFinalDefensor` | `float` (get) | Puntos de vida de la víctima tras el combate. |
| `BarcosHundidosAtacante` | `int` (get) | Bajas del pirata. |
| `BarcosHundidosDefensor` | `int` (get) | Bajas de la víctima. |
| `BarcosCapturedDefensor` | `int` (get) | Barcos capturados al defensor (solo si pirata gana). |
| `BotonCapturado` | `Dictionary<int,int>` (get) | Carga capturada: clave `id_bien`, valor unidades. Vacío si el pirata no ganó. |
| `Descripcion` | `string` (get) | Texto de log con el resumen del combate. |
| `ResultadoCombate(...)` | Constructor | Inicializa todos los campos. `boton` y `descripcion` admiten `null` (se convierten a colección vacía y `string.Empty`). |

#### CombateNavalResolver — `Assets/Scripts/Combate/CombateNavalResolver.cs`

Clase estática pura que resuelve encuentros navales de forma instantánea. No tiene estado propio y no modifica ningún objeto externo.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Resolver(FlotaRuntimeData pirata, FlotaRuntimeData victima, System.Random rng)` | `static ResultadoCombate` | Resuelve el encuentro en tres pasos secuenciales. `rng` puede ser `null`; se crea un generador nuevo en ese caso. |

### Clases modificadas

#### EstadoFlotaPNJ — `Assets/Scripts/PNJ/EstadoFlotaPNJ.cs`

| Valor nuevo | Descripción |
|---|---|
| `Huyendo` | Flota en retirada tras combate. Abstracto por ahora; implementación real en Día 21. |
| `Patrullando` | Pirata moviéndose por el mar buscando presas. Implementación real en Día 21. |
| `Interceptando` | Pirata persiguiendo activamente a un comerciante detectado. Implementación real en Día 21. |
| `HuyendoAPuerto` | Comerciante que detectó un pirata y busca el puerto más cercano como refugio. Implementación real en Día 21. |
| `EsperandoEnPuerto` | Comerciante refugiado esperando a que el peligro desaparezca. Implementación real en Día 21. |

#### FlotaRuntimeData — `Assets/Scripts/PNJ/FlotaRuntimeData.cs`

**Nuevas propiedades (backing field privado):**

| Propiedad | Tipo | Default comerciante | Default pirata | Descripción |
|---|---|---|---|---|
| `IsPirata` | `bool` (get) | `false` | `true` | Marca si la flota es hostil. |
| `VidaMax` | `float` (get) | `100f` | `100f` | Puntos de vida máximos. |
| `VidaActual` | `float` (get+set) | `100f` | `100f` | Puntos de vida actuales. |
| `FuerzaCanhones` | `float` (get) | `8f` | `25f` | Potencia de fuego combinada. |
| `VelocidadFlota` | `float` (get) | `5f` | `4f` | Velocidad de navegación. |
| `ManiobrabilidadFlota` | `float` (get) | `5f` | `4f` | Maniobrabilidad, influye en capturas. |
| `HabilidadCapitan` | `float` (get) | `= InteligenciaComercial` | `= InteligenciaComercial` | Habilidad táctica del capitán. |
| `NumBarcos` | `int` (get+set) | `5` | `5` | Barcos operativos. |
| `Tripulacion` | `int` (get+set) | `30` | `40` | Tripulantes activos. |

**Nuevo constructor:**

| Firma | Descripción |
|---|---|
| `FlotaRuntimeData(int id, string nombrePropietario, bool esPirata)` | Llama al constructor base y sobreescribe los campos de combate con valores piratas si `esPirata == true`. |

**Métodos nuevos:**

| Miembro | Tipo | Descripción |
|---|---|---|
| `AplicarDanio(float cantidad)` | `bool` | Reduce `VidaActual` sin bajar de 0. Devuelve `true` si la flota queda destruida. |
| `EstaDestruida()` | `bool` | Devuelve `NumBarcos <= 0`. |
| `ResetearParaReabastecimiento()` | `void` | Restaura `VidaActual`, `Tripulacion` y `NumBarcos` a sus valores iniciales y vacía la carga. Usado por el ciclo semanal pirata. |

#### MapamundiController — `Assets/Scripts/Navegacion/MapamundiController.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `_instance` | `static MapamundiController` (campo privado) | Referencia a la instancia activa. |
| `Instance` | `static MapamundiController` (get) | Punto de acceso global. Sin `DontDestroyOnLoad`. |
| `Awake()` | `private void` | Asigna `_instance = this`. |
| `ComprobarProximidadCombate(FlotaRuntimeData flotaQueSeMovio)` | `public void` | Itera todas las flotas activas; si hay una pirata y una no-pirata a distancia ≤ 1.5 unidades, llama a `TriggerCombate`. Solo resuelve el primer encuentro encontrado. |
| `TriggerCombate(FlotaRuntimeData pirata, FlotaRuntimeData victima)` | `private void` | Pausa la simulación, resuelve con `CombateNavalResolver.Resolver`, aplica los cambios de estado a ambas flotas según el desenlace y reanuda la simulación. |

#### FlotaIconoMapamundi — `Assets/Scripts/Mapamundi/FlotaIconoMapamundi.cs`

En `Update()`, al finalizar la ruta (bloque `IndiceWaypointActual >= RutaActualTilemap.Count`), después de limpiar la ruta y resetear el índice se añade:

```csharp
if (MapamundiController.Instance != null)
    MapamundiController.Instance.ComprobarProximidadCombate(Flota);
```

Esto conecta la llegada a cada waypoint con la detección de encuentros navales.

#### FlotaManager — `Assets/Scripts/PNJ/FlotaManager.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `_diasDesdeUltimoReabastecimientoPirata` | `int` (campo privado) | Contador de días desde el último reabastecimiento. |
| `ReabastecerPiratas()` | `private void` | Itera todas las flotas y llama a `ResetearParaReabastecimiento()` en las piratas. Llamado automáticamente desde `TickTodosLosControladores` cada 7 días. |
| Piratas en `SpawnFlotasPNJIniciales` | — | Tras los 18 comerciantes, crea 3 flotas pirata históricas (IDs 2001–2003): Störtebeker, Gödeke Michels, Klaus Scheld. Estado inicial `Patrullando`. Posición temporal proporcional al ID hasta que se asignen casillas de mar reales en el Día 21. |

#### ComerciantePNJController — `Assets/Scripts/PNJ/ComerciantePNJController.cs`

| Miembro | Tipo | Descripción |
|---|---|---|
| `TickHuyendo()` | `private void` | Si `IsPirata`: transiciona a `Patrullando`. Si no: transiciona a `EnPuerto`. Registra log de reagrupamiento. |
| `TickPatrullaPirata()` | `private void` | Asigna `_diasRestantesViaje` aleatorio (3–7), limpia la ruta y transiciona a `Patrullando`. Stub hasta Día 21. |
| Guard `IsPirata` en `TickEnPuerto` | — | `if (_flota.IsPirata) { TickPatrullaPirata(); return; }` — los piratas nunca entran en lógica comercial. |
| Guard `IsPirata` en `TickComerciando` | — | Igual que `TickEnPuerto`. |
| `TickViajando` modificado | — | Al llegar a destino: si pirata → `Patrullando`; si no → `Comerciando`. |
| Stubs en `Tick()` | — | Casos `Patrullando`, `Interceptando`, `HuyendoAPuerto`, `EsperandoEnPuerto` con `// TODO Día 21`. |

### Lógica de CombateNavalResolver.Resolver()

**Paso 1 — ¿Intenta huir la víctima?**
Si `pirata.FuerzaCanhones / victima.FuerzaCanhones >= 1.2`, la víctima intentará huir. Se calcula un `factorHuida` como el cociente de `(velocidad + maniobrabilidad + habilidadCapitán×10)` de la víctima entre el del pirata, multiplicado por ruido aleatorio `[0.8, 1.2]`. Si `factorHuida > 1.0` → `ComercianteEscapa`.

**Paso 2 — ¿Puede luchar la víctima?**
Si `FuerzaCanhones == 0 && NumBarcos <= 1`, la víctima se rinde. Se captura toda la carga y todos los barcos → `Rendicion`.

**Paso 3 — Combate**
Cada bando calcula su fuerza efectiva: `cañones + tripulación×0.5 + habilidadCapitán×10`, multiplicada por ruido `[0.8, 1.2]`. El `ratioA` (0–1) determina la proporción de daño infligido. Las bajas en barcos son proporcionales al ratio y al 80% de los barcos de cada bando. Si `ratioA > 0.5`, el pirata puede capturar parte de los barcos hundidos del defensor (probabilidad proporcional a maniobrabilidad y tripulación). El botín es el 50% de la carga si no hay captura de barcos, o el 100% si los hay. El desenlace es `PirataGana` si `ratioA >= 0.5`, `ComercianteGana` si no, o `Empate` si `|ratioA - 0.5| < 0.05`.

### TO-DOs abiertos tras el Día 20

- IA de detección y persecución pirata con memoria de casillas (Día 21)
- Zonas de peligro en tilemap (Día 21)
- Panel modal de encuentro para el jugador (Día 25)
- Stats reales por barco individual cuando existan cascos y módulos (Día 25+)
- FindFirstObjectByType deprecado en ComerciantePNJController (3 llamadas) — Día 32
- Posiciones iniciales reales de piratas en casillas de mar del tilemap (Día 21)

