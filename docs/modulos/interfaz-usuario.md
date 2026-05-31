# Módulo 10 — Interfaz de Usuario

**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos

Flujo de pantallas: Menú Principal → Selección de ciudad → Ciudad → [Mercado / Astillero / Taberna / Puerto] → Mapamundi → Encuentro Naval → Combate → Resultados.

Todos los componentes de UI usan `TextMeshProUGUI`. El HUD de tiempo persiste entre escenas con `DontDestroyOnLoad`.

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.SceneController> | Clase estática | Navegación entre escenas: `IrAMenuPrincipal`, `IrAMapamundi`, `IrACiudad`, `IrAMercado`, `SetPausa`, `TogglePausa`. |
| <xref:MareImperium.HUDDinero> | `MonoBehaviour` | Muestra el dinero del jugador reactivamente via `GameManager.OnDineroActualizado`. Sin polling. |
| <xref:MareImperium.HUDTiempo> | `MonoBehaviour` singleton | HUD persistente con fecha y velocidad. Se auto-destruye si detecta un duplicado. |
| <xref:MareImperium.MenuPrincipalUI> | `MonoBehaviour` | Menú principal: Nueva Partida, Cargar Partida, Salir. Gestiona la visibilidad del panel de selección de ciudad. |
| <xref:MareImperium.SeleccionCiudadUI> | `MonoBehaviour` | Botón de ciudad de inicio en la pantalla de nueva partida. Inicializa slot 0 y navega a Ciudad. |
| <xref:MareImperium.MenuPausa> | `MonoBehaviour` | Menú de pausa in-game. Toggle con Escape. Opciones: Continuar / Guardar / Cargar / Menú Principal / Salir. |
| <xref:MareImperium.PopUpEntradaCiudad> | `MonoBehaviour` | Modal al llegar a una ciudad: Entrar o Continuar navegando. Pausa la simulación mientras decide el jugador. |
| <xref:MareImperium.PanelInspeccionFlota> | `MonoBehaviour` | Panel informativo de flota en el mapamundi. Genera una fila por barco con stats completos. |
| <xref:MareImperium.EncuentroNavalUI> | `MonoBehaviour` | Panel de decisión en encuentro naval (ver [Módulo 3 — Combate Naval](combate-naval.md)). |
| <xref:MareImperium.MercadoUI> | `MonoBehaviour` | Panel del mercado (ver [Módulo 1 — Económico](economico.md)). |
| <xref:MareImperium.MarketRowUI> | `MonoBehaviour` | Fila del mercado (ver [Módulo 1 — Económico](economico.md)). |
| <xref:MareImperium.PanelFlotaUI> | `MonoBehaviour` | Panel de flota en ciudad (ver [Módulo 7 — Flotas](flotas-tripulacion.md)). |
| <xref:MareImperium.TabernaUI> | `MonoBehaviour` | Panel de taberna (ver [Módulo 7 — Flotas](flotas-tripulacion.md)). |
| <xref:MareImperium.AstilleroUI> | `MonoBehaviour` | Panel del astillero (ver [Módulo 4 — Navíos](construccion-navios.md)). |

---

## Formateo de dinero

`HUDDinero.FormatearDinero(long cantidad)` es pública y reutilizable en cualquier panel.  
Formato: `999.999 ƒ` (separador de miles es-ES, símbolo florin).
