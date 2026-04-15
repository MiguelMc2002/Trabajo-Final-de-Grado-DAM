# Contexto del Proyecto — TFG Unity 2D

## Descripción general

Videojuego 2D de estrategia marítima medieval desarrollado en Unity con C#.
Ambientado en la época de la Liga Hanseática. Referencia de gameplay y mecánicas: **saga Patrician** (especialmente Patrician III/IV).

**Alumno:** Miguel Menéndez Caro
**Centro:** Colegios Marianistas Santa Ana y San Rafael — 2º DAM
**Motor:** Unity 2D (última versión estable)
**Lenguaje:** C#
**Base de datos:** SQLite (fichero .db independiente por partida)
**Editor de scripts:** VS Code o Visual Studio

---

## Stack tecnológico

- **Motor:** Unity 2D
- **Lenguaje:** C# con XMLDoc en todos los métodos públicos
- **Persistencia:** SQLite via fichero .db por partida (máximo 5 slots)
- **Convenciones:** camelCase para variables privadas con prefijo `_`, PascalCase para clases y métodos públicos
- **Documentación:** XMLDoc en métodos públicos + FEATURES.md en raíz del proyecto

---

## Módulos del proyecto

### Módulos verticales (jugabilidad)

1. **Módulo económico** — Simulación de mercado en múltiples ciudades. Stock dinámico, precios reactivos a oferta/demanda. Fórmula: `precio_actual = precio_base * (stock_max / Mathf.Max(stock_actual, 1))`
2. **Módulo de producción y cadenas** — Bienes primarios, intermedios y avanzados. Producción diaria basada en edificios y materias primas disponibles.
3. **Módulo de combate naval** — Tablero grid por turnos. Combate automático y manual. Sistema de abordaje con unidades de tripulación.
4. **Módulo de construcción y personalización de navíos** — Cascos base + módulos (armamento, velas, bodega). Desbloqueo de armas de pólvora según calendario del juego.
5. **Módulo de ciudades** — 6 ciudades: Venecia, Génova, Barcelona, Ruan, Lübeck, Brujas. Mercado propio, edificios de producción, astillero, taberna.

### Módulos transversales (infraestructura)

6. **Módulo de mundo y navegación** — Mapamundi funcional con radio de visión (niebla de guerra). Detección de encuentros entre flotas.
7. **Módulo de flotas y gestión de tripulación** — Hasta 5 barcos combatientes por flota. Requiere capitán asignado. Si el barco del capitán es hundido, la flota se disuelve.
8. **Módulo de comportamiento de PNJs** — Máquinas de estados simples. Comerciantes con precios con 7 días de retraso. Piratas que patrullan zonas y evitan comportamiento suicida.
9. **Módulo de tiempo y simulación** — Velocidades: 0.25x, 1x, 2x, 10x + pausa. Fechas de inicio predeterminadas que desbloquean tecnologías distintas.
10. **Módulo de interfaz de usuario** — Flujo: Menú Principal → Ciudad → Mercado/Astillero/Taberna → Mapamundi → Combate Naval → Abordaje → Resultados.
11. **Módulo de guardado y carga** — SQLite. 5 slots máximo. Un fichero .db por partida con estado completo del mundo.
12. **Módulo de audio y feedback visual** — Efectos sonoros contextuales. Indicadores visuales de daño. Marcadores en mapamundi.

---

## Schema de base de datos SQLite

```sql
-- Tabla auxiliar independiente (sin FK con el resto)
CREATE TABLE estadoJuego (
    id_estado        INTEGER PRIMARY KEY,
    dia_juego        INTEGER NOT NULL,
    mes_juego        INTEGER NOT NULL,
    año_juego        INTEGER NOT NULL,
    velocidad_tiempo INTEGER NOT NULL,
    fecha_guardado   TIMESTAMP NOT NULL
);

CREATE TABLE Ciudad (
    id_ciudad INTEGER PRIMARY KEY,
    nombre    TEXT NOT NULL
);

CREATE TABLE Bien (
    id_bien     INTEGER PRIMARY KEY,
    nombre      TEXT NOT NULL,
    categoria   TEXT NOT NULL,  -- 'primario', 'intermedio', 'avanzado'
    precio_base DECIMAL NOT NULL
);

CREATE TABLE EstadoMercadoCiudad (
    id_ciudad     INTEGER NOT NULL REFERENCES Ciudad(id_ciudad),
    id_bien       INTEGER NOT NULL REFERENCES Bien(id_bien),
    stock         INTEGER NOT NULL,
    produccion    INTEGER NOT NULL,
    consumo       INTEGER NOT NULL,
    precio_actual DECIMAL NOT NULL,
    PRIMARY KEY (id_ciudad, id_bien)
);

CREATE TABLE TipoEdificio (
    id_tipo_edificio INTEGER PRIMARY KEY,
    nombre           TEXT NOT NULL
);

CREATE TABLE EdificiosCiudad (
    id_ciudad        INTEGER NOT NULL REFERENCES Ciudad(id_ciudad),
    id_tipo_edificio INTEGER NOT NULL REFERENCES TipoEdificio(id_tipo_edificio),
    cantidad         INTEGER NOT NULL,
    PRIMARY KEY (id_ciudad, id_tipo_edificio)
);

CREATE TABLE Capitan (
    id_capitan INTEGER PRIMARY KEY,
    nombre     TEXT NOT NULL,
    asignado   BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE TipoCasco (
    id_tipo_casco            INTEGER PRIMARY KEY,
    nombre                   TEXT NOT NULL,
    vida_base                INTEGER NOT NULL,
    velocidad_base           INTEGER NOT NULL,
    maniobrabilidad_base     INTEGER NOT NULL,
    capacidad_carga_base     INTEGER NOT NULL
);

CREATE TABLE Flota (
    id_flota         INTEGER PRIMARY KEY,
    tipo_propietario TEXT NOT NULL,  -- 'jugador', 'comerciante', 'pirata', 'neutral'
    id_ciudad_actual INTEGER REFERENCES Ciudad(id_ciudad),
    posicion_x       FLOAT,
    posicion_y       FLOAT,
    id_capitan       INTEGER REFERENCES Capitan(id_capitan)
);

CREATE TABLE Barco (
    id_barco              INTEGER PRIMARY KEY,
    id_tipo_casco         INTEGER NOT NULL REFERENCES TipoCasco(id_tipo_casco),
    nombre_barco          TEXT NOT NULL,
    es_barco_combate      BOOLEAN NOT NULL DEFAULT FALSE,
    vida_actual           INTEGER NOT NULL,
    tripulacion_actual    INTEGER NOT NULL,
    capacidad_tripulacion INTEGER NOT NULL,
    id_flota              INTEGER REFERENCES Flota(id_flota)
);

CREATE TABLE ModuloBarco (
    id_modulo_barco INTEGER PRIMARY KEY,
    id_barco        INTEGER NOT NULL REFERENCES Barco(id_barco),
    tipo_modulo     TEXT NOT NULL,  -- 'armamento', 'velas', 'bodega'
    nombre_modulo   TEXT NOT NULL,
    valor_a         INTEGER NOT NULL,
    valor_b         INTEGER NOT NULL
);

CREATE TABLE EstadoSeccionBarco (
    id_barco     INTEGER NOT NULL REFERENCES Barco(id_barco),
    seccion      TEXT NOT NULL,  -- 'timon', 'velas', 'armamento', 'flotacion'
    vida_seccion INTEGER NOT NULL,
    PRIMARY KEY (id_barco, seccion)
);

CREATE TABLE CargaBarco (
    id_barco INTEGER NOT NULL REFERENCES Barco(id_barco),
    id_bien  INTEGER NOT NULL REFERENCES Bien(id_bien),
    cantidad INTEGER NOT NULL,
    PRIMARY KEY (id_barco, id_bien)
);

CREATE TABLE MemoriaComercialPNJ (
    id_flota        INTEGER NOT NULL REFERENCES Flota(id_flota),
    id_bien         INTEGER NOT NULL REFERENCES Bien(id_bien),
    precio_conocido DECIMAL NOT NULL,
    dia_conocido    INTEGER NOT NULL,
    mes_conocido    INTEGER NOT NULL,
    año_conocido    INTEGER NOT NULL,
    PRIMARY KEY (id_flota, id_bien)
);
```

---

## Beta — PMV planificado (6 días)

### Scope del PMV

El PMV cubre el bucle jugable mínimo:
> Entrar al juego → ir a una ciudad en el mapa → abrir el mercado → comprar barato → ir a otra ciudad → vender caro → ganar dinero

### Decisiones de diseño para la beta

- **Dinero inicial:** 999.999.999 (para testear reacción de precios sin restricciones)
- **Capacidad de almacén:** `int.MaxValue` — ilimitada en beta, se activa con límite en release
- **Movimiento en mapa:** click directo en ciudad → transición inmediata (sin animación de flota)
- **Número de ciudades:** 2 ciudades para la beta (no las 6 definitivas)
- **Guardado:** GameManager singleton con DontDestroyOnLoad — SQLite se implementa después de la beta
- **PNJs:** no hay en la beta
- **Combate:** no hay en la beta

### Interfaz del mercado

Columnas: Producto | Stock ciudad | Comprar (+1 / +10 / +100) | Vender (-1 / -10 / -100) | Stock almacén

Header de almacén: `{capacidad usada} / ∞` (en beta) → `{usado} / {total}` (en release)

Indicador de precio por color:
- 🟢 Stock alto → precio bajo → buen momento para vender aquí
- 🔴 Stock bajo → precio alto → buen momento para comprar aquí
- 🟡 Stock normal

Fórmula de precio: `precio_actual = precio_base * (stock_max / Mathf.Max(stock_actual, 1))`

### Plan de desarrollo por días

| Día | Módulo | Entregable |
|---|---|---|
| 1 | Core y navegación de pantallas | GameManager, SceneController, escenas vacías navegables |
| 2 | Bienes y mercado | BienData ScriptableObjects, MarketManager, UI del mercado |
| 3 | Oficina comercial y compra/venta | OficinaComercial, lógica comprar/vender conectada al mercado |
| 4 | Ciudad | CiudadController, pantalla con acceso a mercado y oficina |
| 5 | Mapamundi | Mapa con 2 ciudades clickables, transición ciudad↔mapa |
| 6 | Integración y bugfixing | Persistencia entre escenas, menú pausa, flujo completo jugable |

### Estructura de carpetas Unity recomendada

```
Assets/
├── Scripts/
│   ├── Core/          (GameManager, SceneController)
│   ├── Economico/     (MarketManager, OficinaComercial, BienData)
│   ├── Navegacion/    (MapamundiController)
│   ├── Ciudades/      (CiudadController)
│   ├── UI/            (MarketRow, HUDController)
│   ├── PNJs/          (FlotaPNJ — post-beta)
│   └── Combate/       (CombateManager — post-beta)
├── Scenes/
│   ├── MenuPrincipal.unity
│   ├── Ciudad.unity
│   ├── Mapamundi.unity
│   └── Mercado.unity
├── Prefabs/
│   └── UI/MarketRow.prefab
├── ScriptableObjects/
│   └── Bienes/        (Grano.asset, Madera.asset, etc.)
├── Sprites/
├── Audio/
└── Database/
    └── schema.sql
```

### Assets gratuitos recomendados para la beta

- **Kenney.nl** — UI Pack RPG Expansion, Cartography Pack (botones, iconos, mapa)
- **game-icons.net** — iconos de bienes (wheat, wood, fish, iron, coin, ship)
- **Google Fonts** — Cinzel o IM Fell English (tipografía medieval)
- **OpenGameArt.org** — mapas medievales placeholder

---

## Lista de bienes del juego

### Primarios
| Bien | Origen | Uso principal |
|---|---|---|
| Grano | Granjas | Alimentación / cerveza |
| Carne | Ganadería | Alimentación urbana |
| Pescado | Puertos | Alimentación |
| Madera | Aserraderos | Construcción / herramientas |
| Lana | Ganadería | Textiles |
| Mineral de hierro | Minas | Metalurgia |
| Brea | Costas | Construcción naval |
| Uvas | Viñedos | Vino |
| Ladrillos | Canteras / hornos | Construcción urbana |

### Intermedios
| Bien | Requiere | Uso |
|---|---|---|
| Harina | Grano | Pan |
| Cerveza | Grano | Consumo urbano |
| Tela | Lana | Ropa |
| Lingotes de hierro | Mineral de hierro | Herramientas / armas |
| Vino | Uvas | Bien de lujo |

### Avanzados
| Bien | Requiere | Uso |
|---|---|---|
| Pan | Harina | Alimentación |
| Ropa | Tela | Consumo urbano |
| Herramientas | Lingotes + Madera | Construcción / producción |
| Armas | Lingotes + Herramientas | Combate |
| Secciones de barco | Madera + Herramientas + Brea | Construcción naval |

---

## Ciudades del juego

| Ciudad | Región | Especialización |
|---|---|---|
| Venecia | Mediterráneo | Bienes de lujo y alto valor |
| Génova | Mediterráneo | Comercio marítimo y redistribución |
| Barcelona | Mediterráneo | Agrícola y vinícola (puente entre regiones) |
| Ruan | Norte de Francia | Manufactura y alto consumo urbano |
| Lübeck | Norte de Alemania | Materias primas y productos básicos |
| Brujas | Mar del Norte | Producción textil e intercambio comercial |

---

## Skills creadas en esta sesión

### unity-sqlite-schema
Contiene el esquema completo de la BBDD con todas las tablas, tipos, claves y relaciones. Usar siempre que se generen queries SQL, DAOs en C# o código de persistencia.

**Fichero:** `tfg-sqlite-schema.md`

### unity-autodoc
Genera comentarios XMLDoc para C# al estilo Javadoc y añade entradas al `FEATURES.md` del proyecto con la API pública de cada clase.

**Fichero:** `unity-autodoc.md`

### unity-pnj-statemachine (recomendada, pendiente de crear)
Describir los 3 tipos de PNJ (comerciante, pirata, neutral) y sus estados (viajar, comerciar, perseguir, huir, rendirse) para que Claude genere máquinas de estado coherentes con la arquitectura.

---

## Instrucciones para Claude en este proyecto

- El proyecto es un videojuego Unity 2D en C# con ambientación medieval marítima
- Referencia de gameplay: saga Patrician (especialmente Patrician III/IV)
- Usar siempre los nombres exactos de tablas y campos del schema SQLite definido arriba
- Los comentarios en código van en español
- XMLDoc obligatorio en todos los métodos y clases públicas
- Orientar las descripciones al comportamiento del juego, no a la implementación técnica
- La tabla `estadoJuego` es independiente y NO tiene FK con el resto de tablas
- En la beta: dinero = 999.999.999, capacidad almacén = int.MaxValue
- Estructura de carpetas según el árbol definido en este documento
- Al generar código de UI usar TextMeshProUGUI, no Text legacy
