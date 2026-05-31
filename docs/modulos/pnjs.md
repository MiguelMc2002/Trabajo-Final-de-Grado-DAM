# Módulo 8 — Comportamiento de PNJs

**Estado:** ✅ Implementado (comerciantes y piratas con ciclo completo)  
**Dependencias:** Módulo Económico, Módulo de Mundo y Navegación, Módulo de Combate

Dos tipos de PNJ: **comerciantes** (ciclo EnPuerto → Viajando → Comerciando → EnPuerto) y **piratas** (detección asíncrona de presas, intercepción, huida). Los comerciantes tienen precios con 7 días de retraso. Los piratas usan `PirataBrain` en un hilo de background para no bloquear el hilo principal de Unity.

---

## Clases

| Clase | Tipo | Descripción |
|---|---|---|
| <xref:MareImperium.FlotaManager> | `MonoBehaviour` singleton | Registro global de flotas activas (PNJ + jugador). Límites: 20 comerciantes / 3 piratas. Gestiona spawn, tick diario y eliminación. |
| <xref:MareImperium.FlotaRuntimeData> | Clase C# | Estado completo de una flota en memoria: posición, carga, stats de combate, estado de la máquina de estados. |
| <xref:MareImperium.EstadoFlotaPNJ> | Enum | `EnPuerto`, `Viajando`, `Comerciando`, `Huyendo`, `Patrullando`, `Interceptando`, `HuyendoAPuerto`, `EsperandoEnPuerto`. |
| <xref:MareImperium.ComerciantePNJController> | Clase C# | Máquina de estados del comerciante. Avanza un paso por día vía `Tick()`. |
| <xref:MareImperium.PirataPNJController> | Clase C# | Máquina de estados del pirata. Gestiona únicamente el estado `Huyendo` post-combate (cooldown 2 días). |
| <xref:MareImperium.PirataBrain> | Clase C# | Cerebro asíncrono del pirata. Dos Tasks en background: `BucleDeteccion` e `BucleNavegacion`. Comunica comandos al hilo principal via `ColaSalida` (`ConcurrentQueue`). |
| <xref:MareImperium.PirataBrainBootstrapper> | `MonoBehaviour` singleton | Construye el grafo de navegación en el hilo principal y arranca un `PirataBrain` por cada flota pirata. |

---

## Ciclo de vida del comerciante

```
[Spawn] → EnPuerto (compra bienes baratos)
  → Viajando (navega hacia ciudad destino)
    → Comerciando (vende, espera 1 día)
      → EnPuerto (siguiente ciclo)
```

Los precios de compra se registran en `MemoriaComercialPNJ` con un retraso de 7 días de juego.

## Ciclo de vida del pirata

```
[Spawn] → Patrullando (casilla aleatoria de mar)
  PirataBrain.BucleDeteccion detecta comerciante en radio
    → Interceptando (persigue con A* recalculado)
      → [Alcanza] → CombateNavalResolver.Resolver()
        → [Pierde] → Huyendo (2 días cooldown) → Patrullando
        → [Gana]  → ResetearParaReabastecimiento() → Patrullando
```

## Arquitectura threading

`PirataBrain` ejecuta dos `Task` en background. El hilo principal:
1. Envía snapshots de posición via `EnviarSnapshot(FlotaSnapshot[])`.
2. Consume `ColaSalida` (máx. 1 comando por frame) en `FlotaIconoMapamundi.Update()`.
3. Cancela los Tasks en `OnDestroy` llamando a `PirataBrain.Detener()`.
