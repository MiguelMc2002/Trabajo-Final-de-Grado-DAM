using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor singleton de flotas PNJ activas en el mundo de juego.
/// Es la única puerta de entrada para registrar, consultar y cambiar
/// el estado de las flotas comerciantes durante la simulación.
/// </summary>
public class FlotaManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor de flotas PNJ.</summary>
    public static FlotaManager Instance { get; private set; }

    // ─── Referencia al estado de partida ─────────────────────────────────────

    private Dictionary<int, FlotaRuntimeData> FlotasPorId
        => GameManager.Instance.EstadoPartida.FlotasPorId;

    // ─── Controladores de comportamiento ─────────────────────────────────────

    private readonly Dictionary<int, ComerciantePNJController> _controladores = new();

    private int _diasDesdeUltimoReabastecimientoPirata = 0;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SimulacionTiempo.OnNuevoDia += TickTodosLosControladores;

        Debug.Log("[FlotaManager] Inicializado como singleton persistente.");
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Añade una flota PNJ al registro de flotas activas de la partida.
    /// Si ya existe una flota con el mismo <see cref="FlotaRuntimeData.Id"/>, la sobreescribe.
    /// </summary>
    /// <param name="flota">Datos de la flota a registrar. No puede ser <c>null</c>.</param>
    public void RegistrarFlota(FlotaRuntimeData flota)
    {
        if (flota == null)
        {
            Debug.LogError("[FlotaManager] RegistrarFlota: el parámetro flota es null.");
            return;
        }

        FlotasPorId[flota.Id] = flota;

        _controladores[flota.Id] = new ComerciantePNJController(flota, this);

        Debug.Log($"[FlotaManager] Flota registrada: id={flota.Id}, propietario={flota.NombrePropietario}");
    }

    /// <summary>
    /// Devuelve los datos de runtime de la flota con el identificador indicado.
    /// </summary>
    /// <param name="id">Identificador de la flota a buscar.</param>
    /// <returns>
    /// El <see cref="FlotaRuntimeData"/> correspondiente,
    /// o <c>null</c> si no hay ninguna flota con ese identificador.
    /// </returns>
    public FlotaRuntimeData ObtenerFlota(int id)
    {
        return FlotasPorId.TryGetValue(id, out FlotaRuntimeData flota) ? flota : null;
    }

    /// <summary>
    /// Devuelve todas las flotas PNJ actualmente activas en el mundo.
    /// </summary>
    /// <returns>Colección de solo lectura con todas las flotas registradas.</returns>
    public IReadOnlyCollection<FlotaRuntimeData> ObtenerTodasLasFlotas()
    {
        return FlotasPorId.Values;
    }

    /// <summary>
    /// Avanza un día de simulación en todos los controladores de comportamiento PNJ registrados.
    /// Suscrito a <see cref="SimulacionTiempo.OnNuevoDia"/> en <c>Awake</c>.
    /// </summary>
    public void TickTodosLosControladores()
    {
        foreach (ComerciantePNJController controlador in _controladores.Values)
            controlador.Tick();

        _diasDesdeUltimoReabastecimientoPirata++;
        if (_diasDesdeUltimoReabastecimientoPirata >= 7)
        {
            _diasDesdeUltimoReabastecimientoPirata = 0;
            ReabastecerPiratas();
        }
    }

    /// <summary>
    /// Restaura vida, tripulación y barcos de todas las flotas pirata activas.
    /// Se llama automáticamente cada 7 días de juego desde <see cref="TickTodosLosControladores"/>.
    /// </summary>
    private void ReabastecerPiratas()
    {
        foreach (FlotaRuntimeData flota in FlotasPorId.Values)
        {
            if (!flota.IsPirata) continue;
            flota.ResetearParaReabastecimiento();
            Debug.Log($"[FlotaManager] {flota.NombrePropietario} reabastecido.");
        }
    }

    /// <summary>
    /// Crea y registra los 18 comerciantes PNJ iniciales al comenzar una partida nueva.
    /// Garantiza que las 6 ciudades tienen mercado inicializado antes de crear las flotas.
    /// Los IDs van del 1001 al 1018 y se distribuyen 3 por ciudad de origen;
    /// el índice se resuelve con módulo para evitar desbordamiento si hay menos de 6 ciudades.
    /// </summary>
    /// <param name="ciudades">
    /// Lista de ciudades disponibles en la partida. Debe contener al menos una entrada.
    /// </param>
    public void SpawnFlotasPNJIniciales(IReadOnlyList<CiudadData> ciudades)
    {
        if (ciudades == null || ciudades.Count == 0)
        {
            Debug.LogWarning("[FlotaManager] SpawnFlotasPNJIniciales: lista de ciudades vacía, no se crean flotas.");
            return;
        }

        GameManager.Instance.InicializarMercadosCiudades(GameManager.Instance.CiudadesDisponibles);

        // (id, nombre, índice en ciudades[])
        var definiciones = new (int id, string nombre, int idxCiudad)[]
        {
            (1001, "Comerciante Hans",      0),
            (1002, "Comerciante Klaus",     1),
            (1003, "Comerciante Erik",      2),
            (1004, "Comerciante Pieter",    3),
            (1005, "Comerciante Johann",    4),
            (1006, "Comerciante Willem",    5),
            (1007, "Comerciante Dirk",      0),
            (1008, "Comerciante Conrad",    1),
            (1009, "Comerciante Albrecht",  2),
            (1010, "Comerciante Heinrich",  3),
            (1011, "Comerciante Gerhard",   4),
            (1012, "Comerciante Rutger",    5),
            (1013, "Comerciante Berthold",  0),
            (1014, "Comerciante Siegfried", 1),
            (1015, "Comerciante Wolfram",   2),
            (1016, "Comerciante Dietrich",  3),
            (1017, "Comerciante Kaspar",    4),
            (1018, "Comerciante Ludolf",    5),
        };

        foreach (var (id, nombre, idxCiudad) in definiciones)
        {
            // Si la flota ya fue cargada desde BD (por CargarFlotasPNJ), no duplicar
            if (FlotasPorId.ContainsKey(id)) continue;

            int idCiudad = ciudades[idxCiudad % ciudades.Count].IdCiudad;
            FlotaRuntimeData flota = new FlotaRuntimeData(id, nombre);
            flota.CiudadOrigenId = idCiudad;
            RegistrarFlota(flota);
        }

        Debug.Log($"[FlotaManager] 18 flotas PNJ iniciales creadas distribuidas entre {ciudades.Count} ciudades.");

        var defPiratas = new (int id, string nombre)[]
        {
            (2001, "Pirata Störtebeker"),
            (2002, "Pirata Gödeke Michels"),
            (2003, "Pirata Klaus Scheld"),
        };

        foreach (var (id, nombre) in defPiratas)
        {
            if (FlotasPorId.ContainsKey(id)) continue;
            var flota = new FlotaRuntimeData(id, nombre, esPirata: true);
            flota.CiudadOrigenId  = -1;
            flota.CiudadDestinoId = -1;
            flota.EstadoActual    = EstadoFlotaPNJ.Patrullando;
            // TODO Día 21: posicionar en casillas de mar real del tilemap
            flota.PosicionActual  = new UnityEngine.Vector2(id % 10, (id / 10) % 10);
            RegistrarFlota(flota);
        }

        Debug.Log("[FlotaManager] 3 flotas pirata creadas.");
    }

    /// <summary>
    /// Cuenta cuántas flotas PNJ viajan actualmente hacia la ciudad indicada
    /// transportando el bien indicado. Usado por <see cref="ComerciantePNJController"/> para
    /// evitar saturación de rutas cuando demasiados comerciantes eligen el mismo destino.
    /// </summary>
    /// <param name="idCiudad">Ciudad destino a comprobar.</param>
    /// <param name="idBien">Identificador del bien transportado.</param>
    /// <returns>Número de flotas en ruta hacia esa ciudad con ese bien.</returns>
    public int ContarFlotasEnRutaHacia(int idCiudad, int idBien)
    {
        int count = 0;
        foreach (FlotaRuntimeData flota in FlotasPorId.Values)
        {
            if (flota.EstadoActual == EstadoFlotaPNJ.Viajando &&
                flota.CiudadDestinoId == idCiudad &&
                flota.Carga.ContainsKey(idBien))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Realiza una transición de estado en la flota indicada y registra el cambio en el log.
    /// No realiza ninguna acción si la flota no existe en el registro.
    /// </summary>
    /// <param name="flotaId">Identificador de la flota cuyo estado se cambia.</param>
    /// <param name="nuevoEstado">Nuevo estado de la máquina de estados PNJ.</param>
    public void CambiarEstado(int flotaId, EstadoFlotaPNJ nuevoEstado)
    {
        FlotaRuntimeData flota = ObtenerFlota(flotaId);
        if (flota == null)
        {
            Debug.LogWarning($"[FlotaManager] CambiarEstado: no existe flota con id={flotaId}.");
            return;
        }

        flota.EstadoActual = nuevoEstado;
        Debug.Log($"[FlotaManager] Flota {flotaId} → {nuevoEstado}");
    }

    private void OnDestroy()
    {
        SimulacionTiempo.OnNuevoDia -= TickTodosLosControladores;
    }
}
