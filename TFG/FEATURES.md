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
| **Descripción** | Registro central de la partida. Conserva el estado del comerciante —tesoro, puerto actual y capacidad de bodega— mientras el jugador navega entre las distintas pantallas del juego. En la beta los datos viven en memoria; en la release se persistirán en SQLite. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Instance` | `static GameManager` (get) | Punto de acceso global al estado de la partida activa. Permite a cualquier pantalla consultar el tesoro o el puerto del jugador. |
| `Dinero` | `long` (get) | Monedas de oro en el cofre del comerciante. Sube al vender mercancía y baja al comprar en cualquier mercado de la Liga. |
| `CiudadActual` | `string` (get) | Puerto en el que está atracado el jugador. Vacío mientras navega por el mapamundi. |
| `CapacidadAlmacen` | `const int` | Capacidad de bodega en la beta: sin límite. En la release se sustituirá por la capacidad real del barco. |
| `ModificarDinero(long cantidad)` | `bool` | Registra un movimiento de dinero en el cofre. Valor positivo al cobrar una venta, negativo al pagar una compra. Devuelve `false` si el tesoro no cubre el gasto. |
| `SetCiudadActual(string nombreCiudad)` | `void` | Indica al juego en qué puerto ha atracado el jugador. Se actualiza cada vez que la flota llega a una nueva ciudad. |
| `GetCantidadBien(BienData bien)` | `int` | Devuelve las unidades del bien indicado que hay en la bodega del jugador. Retorna 0 si el bien no está en el inventario. |
| `ModificarCantidadBien(BienData bien, int cantidad)` | `bool` | Modifica la cantidad de un bien en bodega. Positivo al cargar mercancía (compra), negativo al descargarla (venta). Devuelve `false` si el resultado sería negativo o superaría `CapacidadAlmacen`. |
| `GetTotalUnidadesAlmacen()` | `int` | Devuelve el total de unidades de todas las mercancías almacenadas en bodega. Se usa para comprobar si hay espacio antes de cargar más mercancía. |
| `GetAlmacen()` | `IReadOnlyDictionary<BienData, int>` | Expone el inventario completo de bodega en modo solo lectura. Útil para que la interfaz del almacén enumere todos los bienes cargados. |

### Dependencias

- `BienData` — clave del diccionario de bodega.

---

## SceneController

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Core/SceneController.cs` |
| **Tipo** | `MonoBehaviour` (métodos estáticos) |
| **Módulo** | Core — Interfaz de usuario / Navegación |
| **Descripción** | Gestiona todos los cambios de pantalla del juego. Desde aquí se ordena pasar del mapa al mercado, atracar en una ciudad o volver al menú principal, manteniendo el flujo de juego coherente. Flujo beta: Menú Principal → Mapamundi → Ciudad → Mercado → (vuelta al mapa). |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `IrAMenuPrincipal()` | `static void` | Lleva al jugador al Menú Principal, abandonando la partida en curso. Se usa al iniciar nueva partida o al salir desde la pantalla de pausa. |
| `IrAMapamundi()` | `static void` | Muestra el mapamundi para que el jugador elija su próximo destino. Se invoca al salir de una ciudad o al zarpar desde el puerto. |
| `IrACiudad(string nombreCiudad)` | `static void` | Atraca la flota en el puerto indicado y abre la pantalla de ciudad, desde donde se puede acceder al mercado, astillero o taberna. |
| `IrAMercado()` | `static void` | Abre el mercado del puerto actual. Solo válido si el jugador está dentro de una ciudad. |
| `RecargarEscenaActual()` | `static void` | Reinicia la pantalla actual a su estado inicial sin abandonar la partida. |
| `SetPausa(bool pausado)` | `static void` | Detiene o reanuda el tiempo del juego, congelando o reactivando flotas, producción y simulación de mercado. |
| `TogglePausa()` | `static void` | Alterna entre pausa y juego activo con una sola llamada. Útil para el botón de pausa del HUD o la tecla asignada. |

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
| `precioBase` | `float` | Precio de referencia en monedas de oro cuando el stock de la ciudad está al máximo. Base de la fórmula dinámica de precios. |
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

## MarketManager

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Economico/MarketManager.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Económico — Bienes y mercado |
| **Descripción** | Representa el estado del mercado de una ciudad concreta de la Liga Hanseática. Gestiona el stock disponible de cada bien, calcula precios dinámicos según la fórmula de oferta y demanda (`precio = precioBase × (stockMaximo / max(stock, 1))`), y ejecuta las operaciones de compra y venta del jugador. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `OnMercadoActualizado` | `event Action<BienData>` | Se lanza cada vez que el stock o el precio de cualquier bien cambia. La interfaz del mercado se suscribe para refrescar las filas afectadas. |
| `GetEntradas()` | `IReadOnlyList<EntradaMercado>` | Devuelve la lista completa de entradas del mercado. Útil para que la interfaz construya todas las filas al abrir la pantalla. |
| `GetNombreCiudad()` | `string` | Devuelve el nombre de la ciudad cuyo mercado gestiona este componente. |
| `GetStockActual(BienData bien)` | `int` | Devuelve el stock actual de un bien en este mercado. Retorna 0 si el bien no existe. |
| `GetPrecioActual(BienData bien)` | `float` | Devuelve el precio actual de un bien calculado con la fórmula de oferta y demanda. Retorna 0 si el bien no existe. |
| `Comprar(BienData bien, int cantidad)` | `bool` | Ejecuta la compra de un bien: descuenta el coste del tesoro, reduce el stock de la ciudad y carga las unidades en bodega. Devuelve `false` si stock, dinero o espacio de bodega son insuficientes. |
| `Vender(BienData bien, int cantidad)` | `bool` | Ejecuta la venta de un bien: ingresa el precio en el tesoro, aumenta el stock de la ciudad y retira las unidades de bodega. Devuelve `false` si el jugador no tiene suficiente cantidad. |

### Dependencias

- `BienData` — referencia a los datos estáticos de cada bien.
- `EntradaMercado` — estructura serializable con el estado dinámico de cada bien.
- `GameManager` — para modificar dinero y bodega del jugador.

---

## EntradaMercado

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/Economico/MarketManager.cs` |
| **Tipo** | `[Serializable] class` |
| **Módulo** | Económico — Bienes y mercado |
| **Descripción** | Agrupa el estado dinámico de un bien concreto dentro del mercado de una ciudad: referencia al bien, unidades disponibles y precio calculado. Se serializa en el Inspector para configurar el stock inicial desde el editor. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `bien` | `BienData` | Referencia al `BienData` que define nombre, categoría y precio base. |
| `stockActual` | `int` | Unidades del bien disponibles actualmente en el mercado de la ciudad. Se reduce al comprar y aumenta al vender. |
| `precioActual` | `float` | Precio calculado en tiempo de ejecución según la fórmula de oferta y demanda. No editable desde el Inspector. |

### Dependencias

- `BienData` — datos estáticos del bien representado.

---

## MarketRowUI

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Scripts/UI/MarketRowUI.cs` |
| **Tipo** | `MonoBehaviour` |
| **Módulo** | Interfaz de usuario — Mercado |
| **Descripción** | Controla una fila de la pantalla de mercado. Muestra el nombre del bien, el stock de la ciudad, el stock en bodega del jugador, el precio actual con indicador de color, y los botones de compra/venta (+1, +10, +100). Reacciona automáticamente a los cambios del mercado suscribiéndose al evento `MarketManager.OnMercadoActualizado`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Inicializar(BienData bien, MarketManager marketManager)` | `void` | Inicializa la fila con el bien y el gestor de mercado. Registra los listeners de los botones y se suscribe al evento de actualización. Debe llamarse una vez justo después de instanciar el prefab. |

### Dependencias

- `BienData` — datos del bien que representa la fila.
- `MarketManager` — fuente de datos de stock y precio, y destino de las operaciones de compra/venta.
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
| **Descripción** | Gestiona la pantalla del mercado de una ciudad: instancia una fila `MarketRowUI` por cada bien disponible, muestra la cabecera con el nombre de la ciudad y el estado del almacén (`{usado} / ∞` en beta), y mantiene la interfaz sincronizada con el `MarketManager`. |

### API pública

_No expone miembros públicos propios; toda la comunicación se realiza a través de referencias serializadas en el Inspector y del evento `MarketManager.OnMercadoActualizado`._

### Dependencias

- `MarketManager` — fuente de datos del mercado activo.
- `MarketRowUI` — prefab que se instancia por cada bien.
- `GameManager` — para mostrar la capacidad usada del almacén en la cabecera.
- `TextMeshProUGUI` (TMPro) — etiquetas de cabecera.
