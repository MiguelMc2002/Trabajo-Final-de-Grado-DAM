# Módulo 7 — Flotas y Gestión de Tripulación

**Estado:** ✅ Implementado  
**Dependencias:** Módulo de Construcción de Navíos, Módulo de Ciudades, Módulo de Guardado y Carga

El jugador puede tener hasta 5 barcos en su flota. Cada barco necesita tripulación contratada en la taberna. Los convoyes agrupan hasta 5 barcos del jugador en una formación. Los capitanes se contratan en la taberna y se asignan a barcos concretos.

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.FlotaJugador> | Clase C# | Lista de hasta 5 `BarcoJugador`. Calcula stats agregados (vida, velocidad mínima, carga máxima, fuerza). Puede exportarse como `FlotaRuntimeData` para combate. |
| <xref:MareImperium.TabernaManager> | `MonoBehaviour` singleton | Contratación de marineros (50 oro/marinero) y capitanes (500 oro). |
| <xref:MareImperium.CapitanData> | `ScriptableObject` | Datos del capitán: ID, nombre, habilidades de navegación y combate. |
| <xref:MareImperium.ConvoyData> | Clase C# | Agrupa hasta 5 barcos del jugador. El barco líder no puede abandonar la formación. |
| <xref:MareImperium.ConvoyManager> | `MonoBehaviour` singleton | Gestiona todos los convoyes activos. Persiste entre escenas con `DontDestroyOnLoad`. |
| <xref:MareImperium.PanelFlotaUI> | `MonoBehaviour` | Panel lateral en la escena Ciudad que muestra datos del barco seleccionado y el subpanel de bodega. Toggle con tecla **F**. |
| <xref:MareImperium.TabernaUI> | `MonoBehaviour` | Panel de taberna: Menú / Contratar Marineros / Contratar Capitán. Botones +/- con aceleración en tres fases. |

---

## Constantes

| Constante | Valor | Descripción |
|---|---|---|
| `FlotaJugador.MaxBarcos` | 5 | Máximo de barcos combatientes en la flota del jugador. |
| `TabernaManager.PrecioMarinero` | 50 | Oro por marinero contratado. |
| Precio capitán | 500 | Oro por capitán contratado (hardcoded en `TabernaManager`). |

---

## Flujo de contratación

```
TabernaUI → TabernaManager.ContratarMarineros(barco, cantidad)
  → GameManager.ModificarDinero(-coste)
  → barco.ContratarMarineros(cantidad)

TabernaUI → TabernaManager.ContratarCapitan(barco, capitan)
  → GameManager.ModificarDinero(-500)
  → capitan.IdBarcoAsignado = barco.IdBarco
```
