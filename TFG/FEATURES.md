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
