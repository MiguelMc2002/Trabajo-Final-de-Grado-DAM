# Módulo 4 — Construcción y Personalización de Navíos

**Estado:** ✅ Implementado  
**Dependencias:** Módulo Económico (coste en oro), Módulo de Flotas, Módulo de Ciudades

Patrón **Decorator** sobre la interfaz `IBarco`. Cuatro cascos base (`CascoCog`, `CascoHulk`, `CascoCarraca`, `CascoGalera`) y tres categorías de módulos: Armamento, Velas, Bodega. Los módulos de pólvora se desbloquean a partir del año 1380.

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.IBarco> | Interfaz | Contrato de un casco: stats base, capacidad de módulos, tripulación y coste. |
| <xref:MareImperium.TipoCascoData> | `ScriptableObject` | Implementación concreta de `IBarco` editable desde el Inspector. |
| <xref:MareImperium.CascoDecorador> | `ScriptableObject` abstracto | Decorator abstracto que envuelve un `TipoCascoData`. Las subclases sobreescriben las stats con valores hardcoded. |
| <xref:MareImperium.CascoCog> | Decorator | Cog (id=1). Vida=100, Vel=3. Equilibrado, carga media. |
| <xref:MareImperium.CascoHulk> | Decorator | Hulk (id=2). Vida=150, Vel=2. Máxima carga, lento. |
| <xref:MareImperium.CascoCarraca> | Decorator | Carraca (id=3). Vida=200, Vel=2. Máxima resistencia. |
| <xref:MareImperium.CascoGalera> | Decorator | Galera (id=4). Vida=80, Vel=5. Mínima carga, máxima velocidad. |
| <xref:MareImperium.ModuloBarcoData> | `ScriptableObject` | Módulo instalable: deltas de stats, slots necesarios y coste en oro. |
| <xref:MareImperium.TipoModulo> | Enum | `Armamento`, `Velas`, `Bodega`. |
| <xref:MareImperium.BarcoJugador> | Clase C# | Barco completo del jugador con casco decorado, módulos instalados y tripulación. |
| <xref:MareImperium.AstilleroManager> | `MonoBehaviour` singleton | Lógica de negocio: comprar barcos, instalar módulos, reparar, vender. |
| <xref:MareImperium.AstilleroUI> | `MonoBehaviour` | Panel del astillero con cinco subpaneles (Menú, Construir, Modificar, Reparar, Vender). |

---

## Cascos disponibles

| Casco | ID | Vida | Vel. | Maniob. | Carga | Slots | Tripulación |
|---|---|---|---|---|---|---|---|
| Cog | 1 | 100 | 3 | — | — | — | — |
| Hulk | 2 | 150 | 2 | — | — | — | — |
| Carraca | 3 | 200 | 2 | — | — | — | — |
| Galera | 4 | 80 | 5 | — | — | — | — |

---

## Notas

- Máximo un módulo por tipo (`Armamento`, `Velas`, `Bodega`) por barco.
- Los módulos con `requierePolvora = true` solo se desbloquean si `AñoActual >= 1380`.
- `AstilleroManager.VenderBarco()` devuelve el 50 % del coste total del barco con sus módulos.
