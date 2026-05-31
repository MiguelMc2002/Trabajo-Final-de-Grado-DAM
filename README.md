# Mare Imperium

**Videojuego de estrategia comercial medieval — TFG 2º DAM**

![Version](https://img.shields.io/badge/versión-v0.2-blue)
![Estado](https://img.shields.io/badge/estado-Beta%20cerrada-orange)
![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity)
![Licencia](https://img.shields.io/badge/licencia-MIT-green)

---

## Descripción

Mare Imperium es un videojuego 2D de estrategia comercial ambientado en la Europa medieval de 1300, durante el auge de la Liga Hanseática. El jugador asume el rol de un mercader que gestiona una flota de navíos entre ciudades portuarias —Venecia, Génova, Barcelona, Ruan, Lübeck y Brujas—, comprando y vendiendo mercancías para maximizar sus beneficios en mercados con precios reactivos a la oferta y la demanda.

El diseño de gameplay toma como referencia principal la saga **Patrician** (especialmente Patrician III/IV), adaptando su sistema económico y de navegación a un motor 2D moderno con pathfinding hexagonal. Los comerciantes y piratas PNJ operan de forma autónoma mediante máquinas de estado, afectando dinámicamente los mercados y la navegación del jugador.

La versión actual, **v0.2 — Beta cerrada**, constituye una demo técnica con los sistemas principales funcionales: economía reactiva, navegación A* en mapamundi hexagonal, construcción de barcos con patrón Decorator, combate naval de resolución automática, comportamiento autónomo de PNJs y persistencia completa en SQLite con 5 slots de guardado.

---

## Estado del proyecto

| Módulo | Estado |
|---|---|
| Económico (mercados, precios reactivos, compra/venta) | ✅ Implementado |
| Producción y cadenas (bienes primarios, intermedios, avanzados) | ⚠️ Parcial |
| Combate naval (resolución automática) | ⚠️ Parcial |
| Construcción y personalización de navíos (patrón Decorator) | ✅ Implementado |
| Ciudades (6 ciudades, edificios, paneles) | ✅ Implementado |
| Mundo y navegación (mapamundi hexagonal, A*) | ✅ Implementado |
| Flotas y tripulación (gestión de barcos, convoyes, capitanes) | ✅ Implementado |
| Comportamiento de PNJs (comerciantes y piratas) | ✅ Implementado |
| Tiempo y simulación (velocidades, calendario, eventos) | ✅ Implementado |
| Interfaz de usuario (HUD, menús, paneles) | ✅ Implementado |
| Guardado y carga (SQLite, 5 slots) | ✅ Implementado |
| Audio y feedback visual | ❌ Pendiente |

---

## Características principales

Funcionalidades implementadas en la v0.2:

- **Sistema económico con oferta y demanda reactiva** — cada ciudad tiene stock, producción y consumo diarios; el precio varía automáticamente en función del stock disponible.
- **Mapamundi hexagonal con pathfinding A\*** — navegación fluida con rutas calculadas sobre tiles, soporte para varianza de ruta y cámara libre con zoom.
- **PNJs comerciantes y piratas con máquinas de estado** — los comerciantes buscan rutas rentables entre ciudades; los piratas detectan y persiguen flotas mediante un cerebro asíncrono en background.
- **Sistema de combate naval (resolución automática)** — combate por turnos con varianza ±30 %, sistema de captura de barcos enemigos y panel de resultado post-combate.
- **Construcción y personalización de barcos (patrón Decorator)** — cuatro tipos de casco base (Cog, Hulk, Carraca, Galera) con módulos de armamento, velas y bodega.
- **Guardado y carga con SQLite (5 slots)** — persistencia completa del estado del juego: mercados, flotas, barcos, capitanes, almacenes y memoria comercial PNJ.
- **Sistema de calendario con eventos históricos** — simulación de tiempo con velocidades 0.25x, 1x, 2x y 10x, y desbloqueo de contenido por fecha (armas de pólvora a partir de 1380).

---

## Instalación desde Release

### Requisitos del sistema

| Requisito | Mínimo |
|---|---|
| Sistema operativo | Windows 10 / 11 (64-bit) |
| RAM | 4 GB |
| Almacenamiento | ~500 MB libres |
| GPU | Compatible con DirectX 11 |

### Pasos

1. Ve a la sección [**Releases**](https://github.com/MiguelMc2002/Trabajo-Final-de-Grado-DAM/releases) del repositorio.
2. Descarga el archivo `MareImperium_v0.2_Windows.zip`.
3. Extrae el zip en cualquier carpeta.
4. Ejecuta `MareImperium.exe`.
5. No requiere instalación adicional.

---

## Instalación desde código fuente

Para desarrolladores que quieran abrir el proyecto en Unity.

### Requisitos

- [Unity Hub](https://unity.com/download) instalado
- Unity 6 (6000.x LTS) instalado desde Unity Hub
- Visual Studio Code con la extensión **C# Dev Kit**
- Git

### Pasos

1. Clona el repositorio:
   ```bash
   git clone https://github.com/MiguelMc2002/Trabajo-Final-de-Grado-DAM.git
   ```
2. Abre Unity Hub.
3. Haz clic en **Add project** y selecciona la carpeta `TFG/` dentro del repositorio clonado.
4. Abre el proyecto con Unity 6.
5. Espera a que Unity importe todos los assets (puede tardar varios minutos en la primera apertura).

---

## Documentación técnica

- [Documentación API — GitHub Pages (DocFX)](https://miguelmc2002.github.io/Trabajo-Final-de-Grado-DAM/)
- [FEATURES.md — API pública y estado de módulos](TFG/FEATURES.md)

---

## Tecnologías utilizadas

| Tecnología | Uso |
|---|---|
| Unity 6 (6000.x LTS) | Motor de juego — proyecto 2D URP |
| C# (.NET) | Lenguaje de programación |
| SQLite | Persistencia — un fichero `.db` por slot de guardado |
| TextMesh Pro | Renderizado de texto en UI |
| DocFX | Generación de documentación técnica API |

---

## Autor

| | |
|---|---|
| **Autor** | Miguel Menéndez Caro |
| **Centro** | Colegios Marianistas Santa Ana y San Rafael — 2º DAM |
| **Tutor** | Alejandro Jiménez Vitoria |
| **Curso** | 2025-2026 |
