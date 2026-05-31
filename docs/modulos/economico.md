# Módulo 1 — Económico

**Estado:** ✅ Implementado  
**Dependencias:** Módulo de Ciudades, Módulo de PNJs, Módulo de Guardado y Carga

Simula el mercado en múltiples ciudades con stock dinámico y precios reactivos a la oferta/demanda.

**Fórmula de precio:** `precio_actual = precio_base × (stock_max / max(stock_actual, 1))`

El indicador de color en la interfaz resume el estado del mercado: verde = precio bajo (vender aquí), amarillo = precio normal, rojo = precio alto (comprar aquí).

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.BienData> | `ScriptableObject` | Datos de un bien comerciable: nombre, categoría (Primario/Intermedio/Avanzado), precio base y stock máximo. |
| <xref:MareImperium.EntradaMercado> | Clase C# | Estado en tiempo real de un bien en el mercado de una ciudad: stock actual, producción/consumo diarios y precio calculado. |
| <xref:MareImperium.MarketManager> | `MonoBehaviour` | Gestor del mercado de una ciudad. Expone `Comprar` y `Vender`, actualiza precios y dispara `OnMercadoActualizado`. |
| <xref:MareImperium.OficinaComercial> | Clase C# | Intermediario de operaciones comerciales. Abstrae el origen y destino de mercancías (mercado, almacén ciudad, bodega barco). |
| <xref:MareImperium.MercadoUI> | `MonoBehaviour` | Panel modal del mercado. Instancia una fila `MarketRowUI` por bien y se suscribe a `OnMercadoActualizado`. |
| <xref:MareImperium.MarketRowUI> | `MonoBehaviour` | Fila del mercado. Muestra precio, stocks e indicador de color. Botones +1/+10/+100 para comprar y vender. |

### Sección económica en GameManager

<xref:MareImperium.GameManager> centraliza el estado económico del jugador:

- `Dinero` — oro actual del jugador
- `GetAlmacen()` / `ModificarCantidadBien()` — almacén global del jugador
- `MercadosPorCiudad` — estado de todos los mercados cargados en memoria
- `OnDineroActualizado` — evento estático que dispara `HUDDinero`

---

## Enum asociado

**`CategoriaBien`** `{ Primario, Intermedio, Avanzado }` — clasificación de los bienes en la cadena de producción.

**`OrigenDestino`** `{ Mercado, AlmacenCiudad, BodegaBarco }` — usado por `OficinaComercial.Transferir()`.
