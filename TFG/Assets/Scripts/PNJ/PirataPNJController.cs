using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador puro (no MonoBehaviour) que gobierna el comportamiento de una flota pirata
/// mediante una máquina de estados. Se instancia desde <see cref="FlotaManager.RegistrarFlota"/>
/// cuando la flota tiene <see cref="FlotaRuntimeData.IsPirata"/> == <c>true</c>.
/// Avanza un paso por día de juego al invocar <see cref="Tick"/>.
/// </summary>
public class PirataPNJController
{
    private readonly FlotaRuntimeData      _flota;
    private readonly FlotaManager          _manager;
    private RutaCalculadorTilemap _rutaCalculador;

    // Radio de detección de objetivos en distancia cube (casillas hex)
    private const int RadioDeteccion = 3;

    // Waypoints de patrulla predefinidos en coordenadas offset del Tilemap.
    // Representan zonas marítimas: mar del Norte, golfo de León, estrecho de Gibraltar, mar Adriático.
    private static readonly Vector3Int[] WaypointsPatrulla =
    {
        new Vector3Int(-10, -2,  0),
        new Vector3Int(-10, -14, 0),
        new Vector3Int(-14, -18, 0),
        new Vector3Int(-4,  -14, 0),
    };

    private int             _indiceWaypoint;
    private FlotaRuntimeData _objetivo;
    private int             _ticksHuyendo;
    private int             _ticksDesdeUltimoRecalculo;

    /// <summary>
    /// Inicializa el controlador vinculándolo a la flota pirata, su gestor y el calculador de rutas.
    /// </summary>
    /// <param name="flota">Datos de runtime de la flota pirata que este controlador gobierna.</param>
    /// <param name="manager">Gestor central de flotas, usado para aplicar transiciones de estado.</param>
    /// <param name="rutaCalculador">
    /// Calculador de rutas A* del tilemap. Puede ser <c>null</c> si la escena Mapamundi no está cargada;
    /// en ese caso el movimiento queda pendiente hasta que se asigne.
    /// </param>
    public PirataPNJController(FlotaRuntimeData flota, FlotaManager manager, RutaCalculadorTilemap rutaCalculador)
    {
        _flota          = flota;
        _manager        = manager;
        _rutaCalculador = rutaCalculador;
        _indiceWaypoint = 0;
    }

    /// <summary>
    /// Avanza la lógica de comportamiento pirata un día de juego.
    /// Delega en el método privado correspondiente al estado actual de la flota.
    /// </summary>
    public void Tick()
    {
        switch (_flota.EstadoActual)
        {
            case EstadoFlotaPNJ.Patrullando:
                TickPatrullando();
                break;
            case EstadoFlotaPNJ.Interceptando:
                TickInterceptando();
                break;
            case EstadoFlotaPNJ.Huyendo:
                TickHuyendo();
                break;
        }
    }

    /// <summary>
    /// Reasigna el RutaCalculadorTilemap a todos los controladores pirata.
    /// Llamar desde MapamundiController.Start() tras cargar la escena.
    /// </summary>
    public void AsignarRutaCalculador(RutaCalculadorTilemap rutaCalculador)
    {
        _rutaCalculador = rutaCalculador;
    }

    // ─── Estados ─────────────────────────────────────────────────────────────

    private void TickPatrullando()
    {
        // Detectar objetivo antes de moverse
        FlotaRuntimeData candidato = BuscarObjetivoMasCercano();
        if (candidato != null)
        {
            _objetivo = candidato;
            CambiarEstado(EstadoFlotaPNJ.Interceptando);
            Debug.Log($"[Pirata] {_flota.NombrePropietario} detectó objetivo: {candidato.NombrePropietario}. Interceptando.");
            return;
        }

        if (_rutaCalculador == null) return;

        // Obtener casilla actual
        Vector3Int casillaActual = _rutaCalculador.MundoACasilla(_flota.PosicionActual);

        // Obtener vecinos navegables y elegir uno aleatorio
        List<Vector3Int> vecinos = _rutaCalculador.GetVecinosNavegables(casillaActual);
        if (vecinos == null || vecinos.Count == 0) return;

        Vector3Int destino = vecinos[Random.Range(0, vecinos.Count)];

        // Asignar ruta de un solo paso
        _flota.RutaActualTilemap    = new List<Vector3Int> { casillaActual, destino };
        _flota.IndiceWaypointActual = 0;
        _flota.CasillaDestino       = destino;

        Debug.Log($"[Pirata] {_flota.NombrePropietario} moviéndose a casilla aleatoria {destino}.");
        Debug.Log($"[Pirata] {_flota.NombrePropietario} ruta asignada: {_flota.RutaActualTilemap?.Count} casillas, destino={_flota.CasillaDestino}");
    }

    private void TickInterceptando()
    {
        if (_objetivo == null || _objetivo.EstaDestruida())
        {
            _objetivo = null;
            CambiarEstado(EstadoFlotaPNJ.Patrullando);
            Debug.Log($"[Pirata] {_flota.NombrePropietario} objetivo perdido. Volviendo a Patrullando.");
            return;
        }

        _ticksDesdeUltimoRecalculo++;

        // Recalcular ruta cada 3 ticks para seguir al objetivo en movimiento
        if (_rutaCalculador != null && _ticksDesdeUltimoRecalculo >= 3)
        {
            _ticksDesdeUltimoRecalculo = 0;
            Vector3Int origenCasilla  = _rutaCalculador.MundoACasilla(_flota.PosicionActual);
            Vector3Int destinoCasilla = _rutaCalculador.MundoACasilla(_objetivo.PosicionActual);
            List<Vector3Int> ruta = _rutaCalculador.CalcularRutaConPreferenciaZonaPeligro(origenCasilla, destinoCasilla);
            _flota.RutaActualTilemap    = ruta;
            _flota.IndiceWaypointActual = 0;
            _flota.CasillaDestino       = destinoCasilla;
        }

        // Comprobar si alcanzó al objetivo
        float distancia = DistanciaEuclidiana(_flota.PosicionActual, _objetivo.PosicionActual);
        if (distancia <= 1f)
        {
            CombateEventos.DispararCombate(_flota, _objetivo);
            _objetivo = null;
            CambiarEstado(EstadoFlotaPNJ.Patrullando);
        }
    }

    private void TickHuyendo()
    {
        // Moverse hacia el waypoint de patrulla más cercano usando rutas con preferencia a zonas de peligro
        if (_rutaCalculador != null)
        {
            Vector3Int origenCasilla  = _rutaCalculador.MundoACasilla(_flota.PosicionActual);
            Vector3Int waypointCercano = WaypointMasCercano(origenCasilla);
            List<Vector3Int> ruta = _rutaCalculador.CalcularRutaConPreferenciaZonaPeligro(origenCasilla, waypointCercano);
            _flota.RutaActualTilemap    = ruta;
            _flota.IndiceWaypointActual = 0;
            _flota.CasillaDestino       = waypointCercano;
        }

        _ticksHuyendo++;
        if (_ticksHuyendo >= 2)
        {
            _ticksHuyendo = 0;
            CambiarEstado(EstadoFlotaPNJ.Patrullando);
            Debug.Log($"[Pirata] {_flota.NombrePropietario} terminó de huir. Volviendo a Patrullando.");
        }
    }

    // ─── Detección ───────────────────────────────────────────────────────────

    /// <summary>
    /// Busca el comerciante no pirata más cercano dentro del radio de detección.
    /// La distancia se calcula en espacio de mundo como aproximación euclidiana.
    /// </summary>
    /// <returns>La flota comerciante más cercana, o <c>null</c> si no hay ninguna en radio.</returns>
    private FlotaRuntimeData BuscarObjetivoMasCercano()
    {
        FlotaRuntimeData mejor      = null;
        float            mejorDist  = float.MaxValue;

        foreach (FlotaRuntimeData candidato in FlotaManager.Instance.ObtenerTodasLasFlotas())
        {
            if (candidato.IsPirata)       continue;
            if (candidato.EstaDestruida()) continue;

            float dist = DistanciaEuclidiana(_flota.PosicionActual, candidato.PosicionActual);

            // Convertir radio de casillas a unidades de mundo: 1 casilla ≈ 1 unidad (aproximación)
            if (dist < RadioDeteccion && dist < mejorDist)
            {
                mejorDist = dist;
                mejor     = candidato;
            }
        }

        return mejor;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void CambiarEstado(EstadoFlotaPNJ nuevoEstado)
    {
        _manager.CambiarEstado(_flota.Id, nuevoEstado);
    }

    private static float DistanciaEuclidiana(Vector2 a, Vector2 b)
        => Vector2.Distance(a, b);

    /// <summary>
    /// Devuelve el waypoint de patrulla predefinido más cercano a la casilla indicada.
    /// </summary>
    /// <param name="casillaActual">Posición actual en coordenadas offset del Tilemap.</param>
    /// <returns>Waypoint más cercano en coordenadas offset.</returns>
    private static Vector3Int WaypointMasCercano(Vector3Int casillaActual)
    {
        Vector3Int mejor    = WaypointsPatrulla[0];
        float      mejorDist = float.MaxValue;

        foreach (Vector3Int wp in WaypointsPatrulla)
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
