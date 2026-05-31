# Módulo 2 — Producción y Cadenas

**Estado:** ⚠️ Parcial  
**Dependencias:** Módulo Económico, Módulo de Ciudades

Define las cadenas de producción entre bienes primarios, intermedios y avanzados. Las tablas de base de datos están definidas y los DAOs mantienen la estructura, pero el gestor de producción diaria aún no está implementado.

---

## Cadenas de producción

| Bien (avanzado/intermedio) | Requiere | Categoría final |
|---|---|---|
| Harina | Grano | Intermedio |
| Cerveza | Grano | Intermedio |
| Tela | Lana | Intermedio |
| Lingotes de hierro | Mineral de hierro | Intermedio |
| Vino | Uvas | Intermedio |
| Pan | Harina | Avanzado |
| Ropa | Tela | Avanzado |
| Herramientas | Lingotes + Madera | Avanzado |
| Armas | Lingotes + Herramientas | Avanzado |
| Secciones de barco | Madera + Herramientas + Brea | Avanzado |

---

## Estado de implementación

Las tablas `RecetaProduccion` y `EdificiosCiudad` están definidas en el schema SQLite. El DAO <xref:MareImperium.CiudadDAO> mantiene la estructura de ciudad con `MigrarColumnasCasilla()`.

> **TO-DO:** Implementar `ProduccionCiudadManager` que procese recetas por ciudad en cada tick de `SimulacionTiempo.OnNuevoDia`.

---

## Clases relacionadas

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.BienData> | `ScriptableObject` | Define nombre, categoría y precio base de cada bien. |
| <xref:MareImperium.CiudadDAO> | DAO | Persiste y recupera estado del mercado, edificios y producción por ciudad. |
| <xref:MareImperium.EdificiosCiudadDAO> | DAO | Gestiona los edificios de producción por ciudad. |
| <xref:MareImperium.SimulacionTiempo> | `MonoBehaviour` | Dispara `OnNuevoDia` — punto de entrada para la producción diaria futura. |
