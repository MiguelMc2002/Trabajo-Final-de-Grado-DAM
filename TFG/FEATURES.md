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
| **Descripción** | Registro central de la partida. Conserva el estado del comerciante —tesoro, ciudad actual, última ciudad visitada y bodega— mientras el jugador navega entre las distintas pantallas del juego. En la beta los datos viven en memoria; en la release se persistirán en SQLite. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static GameManager` (get) | Punto de acceso global al estado de la partida activa. |
| `Dinero` | `long` (get) | Monedas de oro en el cofre del comerciante. Sube al vender y baja al comprar. |
| `CiudadActual` | `CiudadData` (get) | Puerto en el que está atracado el jugador. `null` mientras navega por el mapamundi. |
| `UltimaCiudad` | `CiudadData` (get) | Puerto visitado antes del destino actual. `null` si el jugador no ha viajado todavía. Útil para ofrecer volver al origen. |
| `CapacidadAlmacen` | `const int` | Capacidad de bodega en la beta: `int.MaxValue`. En la release se sustituirá por la capacidad real del barco. |
| `EstablecerCiudadActual(CiudadData ciudad)` | `void` | Registra el puerto de destino. Guarda el valor anterior en `UltimaCiudad` antes de sobrescribir `CiudadActual`. Invocado desde `MapamundiController`. |
| `ModificarDinero(long cantidad)` | `bool` | Registra un movimiento de dinero. Positivo al cobrar una venta, negativo al pagar una compra. Devuelve `false` si el tesoro no cubre el gasto. |
| `GetCantidadBien(BienData bien)` | `int` | Devuelve las unidades del bien indicado en bodega. Retorna 0 si no está en el inventario. |
| `ModificarCantidadBien(BienData bien, int cantidad)` | `bool` | Modifica la cantidad de un bien en bodega. Devuelve `false` si el resultado sería negativo o superaría `CapacidadAlmacen`. |
| `GetTotalUnidadesAlmacen()` | `int` | Devuelve el total de unidades de todas las mercancías en bodega. |
| `GetAlmacen()` | `IReadOnlyDictionary<BienData, int>` | Expone el inventario completo de bodega en modo solo lectura. |

### Dependencias

- `BienData` — clave del diccionario de bodega.
- `CiudadData` — tipo de `CiudadActual` y `UltimaCiudad`.

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
| `NombreCiudad` | `string` | Nombre de la ciudad que se mostrará en la interfaz (p. ej. "Lübeck", "Brujas"). |
| `Mercado` | `List<EntradaMercado>` | Lista de bienes disponibles en el mercado de esta ciudad, con su stock inicial y cadencias diarias. |

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
| **Descripción** | Representa el estado del mercado de una ciudad concreta de la Liga Hanseática. En `Start`, si `DatosCiudad` está asignado, inicializa la lista de entradas con una copia profunda del asset para no mutar sus datos en partida. Gestiona el stock disponible de cada bien, calcula precios dinámicos según la fórmula `precio = precioBase × (stockMaximo / max(stock, 1))`, y ejecuta las operaciones de compra y venta del jugador. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `DatosCiudad` | `CiudadData` | Asset con la configuración de la ciudad. Si está asignado, `Start` copia sus entradas de mercado y lee el nombre de ciudad desde aquí. |
| `OnMercadoActualizado` | `event Action<BienData>` | Se lanza cada vez que el stock o el precio de cualquier bien cambia. La interfaz del mercado se suscribe para refrescar las filas afectadas. |
| `GetEntradas()` | `IReadOnlyList<EntradaMercado>` | Devuelve la lista completa de entradas del mercado. |
| `GetNombreCiudad()` | `string` | Devuelve el nombre de la ciudad leído desde `DatosCiudad`. Retorna cadena vacía si no hay asset asignado. |
| `GetStockActual(BienData bien)` | `int` | Devuelve el stock actual de un bien en este mercado. Retorna 0 si el bien no existe. |
| `GetPrecioActual(BienData bien)` | `float` | Devuelve el precio actual de un bien calculado con la fórmula de oferta y demanda. Retorna 0 si el bien no existe. |
| `Comprar(BienData bien, int cantidad)` | `bool` | Descuenta el coste del tesoro, reduce el stock de la ciudad y carga las unidades en bodega. Devuelve `false` si stock, dinero o espacio son insuficientes. |
| `Vender(BienData bien, int cantidad)` | `bool` | Ingresa el precio en el tesoro, aumenta el stock de la ciudad y retira las unidades de bodega. Devuelve `false` si el jugador no tiene suficiente cantidad. |

### Dependencias

- `CiudadData` — fuente de configuración del mercado; se lee en `Start`.
- `EntradaMercado` — estado dinámico de cada bien en tiempo de partida.
- `BienData` — referencia a los datos estáticos de cada bien.
- `GameManager` — para modificar dinero y bodega del jugador.

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
| **Descripción** | Controla la lógica del Menú Principal: mostrar el panel de selección de ciudad al iniciar nueva partida, cerrarlo con Escape o con el botón Atrás, y salir de la aplicación. El botón Cargar Partida es un stub pendiente de la release. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `panelSeleccionCiudad` | `GameObject` | Panel con los botones de ciudad para comenzar una nueva partida. Se asigna desde el Inspector. |
| `IniciarNuevaPartida()` | `void` | Activa el panel de selección de ciudad. Llamado por el botón "Nueva Partida". |
| `CerrarPanelSeleccion()` | `void` | Oculta el panel de selección. Llamado por el botón "Atrás" y por la tecla Escape. |
| `CargarPartida()` | `void` | Stub pendiente post-beta. Llamado por el botón "Cargar Partida". |
| `Salir()` | `void` | Cierra la aplicación con `Application.Quit()`. |

### Dependencias

- `SceneController` — navegación entre pantallas (uso indirecto vía `SeleccionCiudadUI`).

---

## SeleccionCiudadUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/SeleccionCiudadUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Menú Principal |
| **Descripción** | Componente adjunto a cada botón de ciudad en el panel de selección. Al pulsarlo, registra la ciudad en `GameManager` y carga la escena de ciudad. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `datosCiudad` | `CiudadData` | Ciudad asociada a este botón. Asignar desde el Inspector. |
| `SeleccionarCiudad()` | `void` | Establece `GameManager.CiudadActual` y llama a `SceneController.IrACiudad()`. Asignar al evento `OnClick` del botón. |

### Dependencias

- `CiudadData` — datos del puerto que representa el botón.
- `GameManager` — registra la ciudad seleccionada.
- `SceneController` — carga la escena Ciudad.

---

## MenuPausa

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/MenuPausa.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Pausa |
| **Descripción** | Gestiona el menú de pausa en las escenas jugables (Ciudad, Mapamundi). La tecla Escape alterna la visibilidad del panel y congela/reanuda `Time.timeScale`. Añadir a un GameObject persistente en cada escena jugable y asignar el panel desde el Inspector. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Continuar()` | `void` | Oculta el panel y reanuda el tiempo (`timeScale = 1`). Asignar al botón "Continuar". |
| `IrAMenuPrincipal()` | `void` | Reanuda el tiempo y carga el Menú Principal abandonando la partida. Asignar al botón "Menú Principal". |
| `SalirAlEscritorio()` | `void` | Reanuda el tiempo y cierra la aplicación. En el editor detiene el modo Play. Asignar al botón "Salir". |

### Dependencias

- `SceneController` — carga el Menú Principal en `IrAMenuPrincipal()`.

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
