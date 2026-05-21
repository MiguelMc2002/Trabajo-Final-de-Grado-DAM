using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador puro (no MonoBehaviour) que gobierna el comportamiento de una flota pirata
/// mediante una máquina de estados. Se instancia desde <see cref="FlotaManager.RegistrarFlota"/>
/// cuando la flota tiene <see cref="FlotaRuntimeData.IsPirata"/> == <c>true</c>.
/// La detección de presas y el cálculo de rutas se delegan al <see cref="PirataBrain"/> asíncrono.
/// Avanza un paso por día de juego al invocar <see cref="Tick"/>.
/// </summary>
public class PirataPNJController
{
    private readonly FlotaRuntimeData _flota;
    private readonly FlotaManager     _manager;
    private RutaCalculadorTilemap     _rutaCalculador;

    private int _ticksHuyendo;

    // Waypoints de huida: zonas marítimas alejadas de las rutas comerciales.
    private static readonly Vector3Int[] WaypointsHuida =
    {
        new Vector3Int(-10, -2,  0),
        new Vector3Int(-10, -14, 0),
        new Vector3Int(-14, -18, 0),
        new Vector3Int(-4,  -14, 0),
    };

    /// <summary>
    /// Inicializa el controlador vinculándolo a la flota pirata, su gestor y el calculador de rutas.
    /// </summary>
    /// <param name="flota">Datos de runtime de la flota pirata que este controlador gobierna.</param>
    /// <param name="manager">Gestor central de flotas, usado para aplicar transiciones de estado.</param>
    /// <param name="rutaCalculador">
    /// Calculador de rutas A* del tilemap. Puede ser <c>null</c> si la escena Mapamundi no está cargada.
    /// </param>
    public PirataPNJController(FlotaRuntimeData flota, FlotaManager manager, RutaCalculadorTilemap rutaCalculador)
    {
        _flota          = flota;
        _manager        = manager;
        _rutaCalculador = rutaCalculador;
    }

    /// <summary>
    /// Avanza la lógica de comportamiento pirata un día de juego.
    /// La lógica de detección y navegación la gestionan los <see cref="PirataBrain"/> Tasks.
    /// Tick solo gestiona <see cref="TickHuyendo"/> para el estado de huida post-combate.
    /// </summary>
    public void Tick()
    {
        if (_flota.EstadoActual == EstadoFlotaPNJ.Huyendo)
            TickHuyendo();
    }

    /// <summary>
    /// Reasigna el RutaCalculadorTilemap al controlador.
    /// Llamar desde MapamundiController.Start() tras cargar la escena.
    /// </summary>
    public void AsignarRutaCalculador(RutaCalculadorTilemap rutaCalculador)
    {
        _rutaCalculador = rutaCalculador;
    }

    // ─── Estado Huyendo ───────────────────────────────────────────────────────

    private void TickHuyendo()
    {
        if (_rutaCalculador != null)
        {
            Vector3Int origenCasilla   = _rutaCalculador.MundoACasilla(_flota.PosicionActual);
            Vector3Int waypointCercano = WaypointHuidaMasCercano(origenCasilla);
            List<Vector3Int> ruta      = _rutaCalculador.CalcularRutaConPreferenciaZonaPeligro(origenCasilla, waypointCercano);
            _flota.RutaActualTilemap    = ruta;
            _flota.IndiceWaypointActual = 0;
            _flota.CasillaDestino       = waypointCercano;
        }

        _ticksHuyendo++;
        if (_ticksHuyendo >= 2)
        {
            _ticksHuyendo = 0;
            _manager.CambiarEstado(_flota.Id, EstadoFlotaPNJ.Patrullando);
            Debug.Log($"[Pirata] {_flota.NombrePropietario} terminó de huir. Volviendo a Patrullando.");
        }
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static Vector3Int WaypointHuidaMasCercano(Vector3Int casillaActual)
    {
        Vector3Int mejor     = WaypointsHuida[0];
        float      mejorDist = float.MaxValue;

        foreach (Vector3Int wp in WaypointsHuida)
        {
            float dist = Vector3Int.Distance(casillaActual, wp);
            if (dist < mejorDist)
            {
                mejorDist = dist;
                mejor     = wp;
            }
        }

        return mejor;
    }
}
