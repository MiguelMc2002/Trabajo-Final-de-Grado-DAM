# Videojuego 2D Isométrico— Estrategia Marítima Medieval

Videojuego 2D Isométrico de estrategia marítima ambientado en la época de la Liga Hanseática. El jugador gestiona una flota mercante navegando entre ciudades portuarias, comprando y vendiendo mercancías para maximizar sus beneficios, construyendo y personalizando sus barcos, y enfrentándose a piratas y rivales comerciales en combates navales por turnos.

Referencia de gameplay: saga **Patrician** (especialmente Patrician III/IV).

---

## Stack tecnológico

| Tecnología | Uso |
|---|---|
| Unity 6 | Motor de juego — proyecto 2D URP |
| C# | Lenguaje de programación |
| SQLite | Persistencia — un fichero .db por partida |
| TextMesh Pro | Renderizado de texto en UI |

---

## Módulos del juego

### Verticales (jugabilidad)
- **Módulo económico** — Simulación de mercado con precios reactivos a oferta y demanda
- **Módulo de producción y cadenas** — Bienes primarios, intermedios y avanzados con cadenas de producción
- **Módulo de combate naval** — Tablero grid por turnos con sistema de abordaje
- **Módulo de construcción de navíos** — Cascos base con módulos de armamento, velas y bodega
- **Módulo de ciudades** — 6 ciudades: Venecia, Génova, Barcelona, Ruan, Lübeck, Brujas

### Transversales (infraestructura)
- **Módulo de mundo y navegación** — Mapamundi con niebla de guerra
- **Módulo de flotas y tripulación** — Hasta 5 barcos combatientes por flota
- **Módulo de comportamiento de PNJs** — Comerciantes, piratas y neutrales con máquinas de estados
- **Módulo de tiempo y simulación** — Velocidades 0.25x, 1x, 2x, 10x y pausa
- **Módulo de interfaz de usuario** — Flujo completo de pantallas
- **Módulo de guardado y carga** — SQLite con 5 slots máximo
- **Módulo de audio y feedback visual** — Efectos sonoros e indicadores visuales

---

## Estructura del proyecto

```
Assets/
├── Scripts/
│   ├── Core/          (GameManager, SceneController)
│   ├── Economico/     (MarketManager, OficinaComercial, BienData)
│   ├── Navegacion/    (MapamundiController)
│   ├── Ciudades/      (CiudadController, CiudadData, EdificioClickable)
│   ├── UI/            (MarketRowUI, MercadoUI)
│   ├── PNJs/          (post-beta)
│   └── Combate/       (post-beta)
├── Scenes/
├── Prefabs/
├── ScriptableObjects/
│   ├── Bienes/
│   └── Ciudades/
├── Sprites/
├── Fonts/
└── Audio/
```

---

## Documentación técnica

La documentación técnica completa — API pública de cada clase, decisiones de diseño y tareas pendientes — está disponible en [`FEATURES.md`](TFG/FEATURES.md).

---

## Convenciones de código

- `_camelCase` para variables privadas
- `PascalCase` para clases y métodos públicos
- XMLDoc obligatorio en todos los métodos y clases públicas
- Comentarios en español orientados al comportamiento del juego
