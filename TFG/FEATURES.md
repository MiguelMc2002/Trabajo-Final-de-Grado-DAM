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
| **Descripción** | Agrupa la configuración de un bien dentro del mercado de una ciudad concreta: referencia al bien, stock inicial, stock máximo, tasas diarias de producción y consumo, y precio calculado en tiempo de ejecución. Se serializa en el Inspector para configurar el mercado desde el editor. `MarketManager` realiza una copia profunda de estas entradas al iniciar la partida para no mutar el asset. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Bien` | `BienData` | Referencia al `BienData` que define nombre, categoría y precio base del bien. |
| `StockActual` | `int` | Unidades del bien disponibles actualmente en el mercado. Se reduce al comprar y aumenta al vender. |
| `StockMax` | `int` | Capacidad máxima de este bien en la ciudad. Límite de acumulación; previsto para renombrarse a `UmbralFlush` en la release. |
| `ProduccionDiaria` | `int` | Unidades que la ciudad genera de este bien cada día de juego por sus edificios productivos. |
| `ConsumoDiario` | `int` | Unidades que la ciudad consume de este bien cada día de juego para satisfacer a su población. |
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
| `GetEntradas()` | `IReadOnlyList<EntradaMercado>` | Devuelve la lista completa de entradas del mercado. Útil para que la interfaz construya todas las filas al abrir el panel. |
| `GetNombreCiudad()` | `string` | Devuelve el nombre de la ciudad leído desde `DatosCiudad`. Retorna cadena vacía si no hay asset asignado. |
| `GetStockActual(BienData bien)` | `int` | Devuelve el stock actual de un bien en este mercado. Retorna 0 si el bien no existe. |
| `GetPrecioActual(BienData bien)` | `float` | Devuelve el precio actual de un bien calculado con la fórmula de oferta y demanda. Retorna 0 si el bien no existe. |
| `Comprar(BienData bien, int cantidad)` | `bool` | Ejecuta la compra de un bien: descuenta el coste del tesoro, reduce el stock de la ciudad y carga las unidades en bodega. Devuelve `false` si stock, dinero o espacio de bodega son insuficientes. |
| `Vender(BienData bien, int cantidad)` | `bool` | Ejecuta la venta de un bien: ingresa el precio en el tesoro, aumenta el stock de la ciudad y retira las unidades de bodega. Devuelve `false` si el jugador no tiene suficiente cantidad. |

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
| **Descripción** | Controla una fila del panel de mercado. Muestra el nombre del bien, el stock de la ciudad, el stock en bodega del jugador, el precio actual con indicador de color, y los botones de compra/venta (+1, +10, +100). Reacciona automáticamente a los cambios del mercado suscribiéndose al evento `MarketManager.OnMercadoActualizado`. Las operaciones de compra/venta se delegan en `OficinaComercial` en lugar de llamar directamente a `MarketManager`. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `Inicializar(BienData bien, MarketManager marketManager, OficinaComercial oficina)` | `void` | Inicializa la fila con el bien, el gestor de mercado y la oficina comercial. Limpia y registra los listeners de los 6 botones y se suscribe al evento de actualización. Puede llamarse varias veces de forma segura gracias a `RemoveAllListeners()`. |

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
| `UltimoMensaje` | `string` (get) | Descripción textual del resultado de la última operación. La UI lo lee tras cada compra o venta para informar al jugador. |
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
| **Descripción** | Coordinador central de la escena Ciudad. En `Start` muestra el nombre del puerto leyendo `DatosCiudad.NombreCiudad` (con fallback a `GameManager.Instance.CiudadActual` y, en último caso, "Ciudad de prueba") y oculta todos los paneles. Recibe los clicks de los edificios del mapa visual a través de `AbrirEdificio` y gestiona qué panel de UI mostrar u ocultar activando el panel correspondiente y cerrando los demás. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `DatosCiudad` | `CiudadData` | Asset con el nombre y el mercado de la ciudad actual. Si está asignado, se usa su `NombreCiudad` en lugar de consultar `GameManager`. |
| `PanelMercado` | `GameObject` | Panel de UI del mercado. Se activa al pulsar el edificio Mercado. Asignar desde el Inspector. |
| `PanelAstillero` | `GameObject` | Panel de UI del astillero. Se activa al pulsar el edificio Astillero. Asignar desde el Inspector. |
| `PanelTaberna` | `GameObject` | Panel de UI de la taberna. Se activa al pulsar el edificio Taberna. Asignar desde el Inspector. |
| `CerrarTodosPaneles()` | `void` | Oculta los tres paneles de edificio llamando a `SetActive(false)`. Se invoca al iniciar la escena y antes de abrir cualquier panel. |
| `AbrirEdificio(TipoEdificio tipo)` | `void` | Cierra todos los paneles y activa el que corresponde al edificio pulsado. Los componentes `EdificioClickable` de la escena invocan este método. |

### Dependencias

- `CiudadData` — fuente del nombre de ciudad y configuración del mercado.
- `GameManager` — fallback para leer `CiudadActual` si no hay `DatosCiudad`.
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

## CiudadesEditorSetup

| Campo | Valor |
|---|---|
| **Ruta** | `Assets/Editor/CiudadesEditorSetup.cs` |
| **Tipo** | `static class` (editor only, `#if UNITY_EDITOR`) |
| **Módulo** | Utilidades de editor — Ciudades |
| **Descripción** | Genera los assets `CiudadData` de las ciudades de la beta con su mercado inicial preconfigurado. Crea la carpeta `Assets/ScriptableObjects/Ciudades/` si no existe usando `AssetDatabase.CreateFolder`. Los `BienData` se cargan con `AssetDatabase.LoadAssetAtPath` desde `Assets/ScriptableObjects/Bienes/`; si alguno falta emite un aviso indicando que hay que ejecutar primero `TFG/Crear Bienes Primarios`. Solo se compila en el editor; no afecta a las builds. |

### API pública

| Miembro | Tipo | Descripción |
|---|---|---|
| `CrearAssetsCiudades()` | `static void` | Genera o actualiza `Lubeck.asset` en `Assets/ScriptableObjects/Ciudades/` con los cinco bienes del mercado de Lübeck. Acceder desde el menú Unity: **TFG → Crear Assets de Ciudades**. |

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

### Revisión de arquitectura (pendiente — Día 6)

- Análisis estático de dependencias entre clases para detectar acoplamiento excesivo.
- Verificar que ninguna clase UI accede directamente a `GameManager` o `MarketManager` sin pasar por `OficinaComercial`.
- Verificar separación correcta entre datos (`ScriptableObjects`) y lógica (Managers).
- Verificar que no hay referencias circulares entre módulos.
