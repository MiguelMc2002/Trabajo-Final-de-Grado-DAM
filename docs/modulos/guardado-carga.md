# Módulo 11 — Guardado y Carga

**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos

Persistencia completa en SQLite. Hasta 5 slots de guardado, cada uno en un fichero `.db` independiente (`slot_1.db` … `slot_5.db`). La tabla `estadoJuego` es independiente y no tiene claves foráneas con el resto del schema.

---

## Clases de gestión

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.DatabaseManager> | `MonoBehaviour` singleton | Abre y mantiene la conexión SQLite activa. `InicializarSlot(int)` crea o migra el `.db`. |
| <xref:MareImperium.SaveManager> | `MonoBehaviour` singleton | `GuardarPartida(int slot)` — persiste estado global, mercados, almacenes, flota, capitanes, flotas PNJ y memoria comercial. |
| <xref:MareImperium.LoadManager> | `MonoBehaviour` singleton | `CargarPartida(int slot)` — restaura en 9 pasos ordenados: tiempo → dinero → almacenes → mercados → flotas PNJ → flota jugador → capitanes. |

## Clases de interfaz de slots

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.PantallaSlotsUI> | `MonoBehaviour` | Panel de selección de slots. Escanea los `.db` en disco y rellena cada fila `SlotUI`. Modos: Guardar / Cargar. |
| <xref:MareImperium.SlotData> | Clase C# | Metadatos de un slot: número, ocupado, nombre, fecha de guardado, días jugados. |
| <xref:MareImperium.SlotUI> | `MonoBehaviour` | Fila de slot. Botones: Guardar / Cargar / Borrar (según modo). |

---

## DAOs activos

| DAO | Tabla(s) | Descripción |
|---|---|---|
| <xref:MareImperium.EstadoJuegoDAO> | `estadoJuego` | Estado global: fecha, velocidad, dinero, modo pirata. |
| <xref:MareImperium.CiudadDAO> | `Ciudad`, `EstadoMercadoCiudad`, `EdificiosCiudad` | Ciudades, mercados y edificios. |
| <xref:MareImperium.BarcoDAO> | `Barco`, `TipoCasco`, `EstadoSeccionBarco`, `CargaBarco` | Barcos con módulos y secciones. |
| <xref:MareImperium.FlotaDAO> | `Flota` | Flota del jugador y flotas PNJ. |
| <xref:MareImperium.ModuloBarcoDAO> | `ModuloBarco` | Módulos instalados por barco. |
| <xref:MareImperium.CapitanDAO> | `Capitan` | Capitanes y asignaciones. |
| <xref:MareImperium.AlmacenCiudadDAO> | `AlmacenCiudadJugador` | Almacén del jugador por ciudad. |
| <xref:MareImperium.MemoriaComercialPNJDAO> | `MemoriaComercialPNJ` | Precios conocidos por los comerciantes PNJ. |

---

## Orden de carga (`LoadManager`)

1. Tiempo (fecha y velocidad)
2. Dinero del jugador
3. Almacenes de ciudad
4. Mercados por ciudad
5. Flotas PNJ (con carga)
6. Flota del jugador (barcos + módulos + tripulación)
7. Capitanes contratados
8. Spawn de iconos en mapamundi
9. Memoria comercial PNJ
