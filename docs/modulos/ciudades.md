# Módulo 5 — Ciudades

**Estado:** ✅ Implementado  
**Dependencias:** Todos los módulos verticales

Cada ciudad tiene su propio mercado, astillero, taberna y puerto. El jugador llega a una ciudad desde el mapamundi a través del pop-up de entrada.

---

## Ciudades del juego

| Ciudad | Región | Especialización |
|---|---|---|
| Venecia | Mediterráneo | Bienes de lujo y alto valor |
| Génova | Mediterráneo | Comercio marítimo y redistribución |
| Barcelona | Mediterráneo | Agrícola y vinícola |
| Ruan | Norte de Francia | Manufactura y alto consumo urbano |
| Lübeck | Norte de Alemania | Materias primas y productos básicos |
| Brujas | Mar del Norte | Producción textil e intercambio comercial |

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.CiudadData> | `ScriptableObject` | Datos de una ciudad: ID, nombre, casilla en el tilemap y mercado inicial. |
| <xref:MareImperium.CiudadController> | `MonoBehaviour` singleton | Controlador de la escena Ciudad. Gestiona la apertura de paneles (mercado, astillero, taberna, puerto) y el regreso al mapamundi. |
| <xref:MareImperium.EdificioClickable> | `MonoBehaviour` | Botón de edificio en la pantalla de ciudad. Notifica a `CiudadController.AbrirEdificio()` al pulsarlo. |
| <xref:MareImperium.PanelAstilleroUI> | `MonoBehaviour` | Panel de astillero (stub de beta). Sustituido por `AstilleroUI` en release. |
| <xref:MareImperium.PanelTabernaUI> | `MonoBehaviour` | Panel de taberna (stub de beta). Sustituido por `TabernaUI` en release. |

---

## Enum asociado

**`TipoEdificio`** `{ Mercado, Astillero, Taberna, Puerto }` — identifica el panel a abrir cuando el jugador pulsa un edificio.

---

## Flujo de navegación

```
Mapamundi → [clic en ciudad] → PopUpEntradaCiudad
  → [Entrar] → Escena Ciudad
    → EdificioClickable → CiudadController.AbrirEdificio()
      → Panel Mercado / Astillero / Taberna / Puerto
  → [Continuar] → navega sin entrar
```
