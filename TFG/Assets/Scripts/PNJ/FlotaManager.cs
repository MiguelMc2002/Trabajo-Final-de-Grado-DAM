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

        // Crear y registrar el controlador de comportamiento para esta flota
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
    }

    /// <summary>
    /// Crea y registra las flotas PNJ iniciales de prueba al comenzar una partida nueva.
    /// Genera exactamente 2 comerciantes: Hans (id 1001) y Klaus (id 1002),
    /// asignando sus ciudades de origen a partir del catálogo recibido.
    /// </summary>
    /// <param name="ciudades">
    /// Lista de ciudades disponibles en la partida.
    /// Se usa la primera y la segunda ciudad (o la primera para ambas si solo hay una).
    /// </param>
    public void SpawnFlotasPNJIniciales(IReadOnlyList<CiudadData> ciudades)
    {
        if (ciudades == null || ciudades.Count == 0)
        {
            Debug.LogWarning("[FlotaManager] SpawnFlotasPNJIniciales: lista de ciudades vacía, no se crean flotas.");
            return;
        }

        int ciudadHansId  = ciudades[0].IdCiudad;
        int ciudadKlausId = ciudades.Count >= 2 ? ciudades[1].IdCiudad : ciudades[0].IdCiudad;

        FlotaRuntimeData hans = new FlotaRuntimeData(1001, "Comerciante Hans");
        hans.CiudadOrigenId = ciudadHansId;
        RegistrarFlota(hans);

        FlotaRuntimeData klaus = new FlotaRuntimeData(1002, "Comerciante Klaus");
        klaus.CiudadOrigenId = ciudadKlausId;
        RegistrarFlota(klaus);

        Debug.Log($"[FlotaManager] Flotas PNJ iniciales creadas: Hans (ciudad {ciudadHansId}), Klaus (ciudad {ciudadKlausId}).");
    }

    private void OnDestroy()
    {
        SimulacionTiempo.OnNuevoDia -= TickTodosLosControladores;
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
}
