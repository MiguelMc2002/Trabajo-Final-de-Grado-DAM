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

    private readonly Dictionary<int, ComerciantePNJController> _controladores        = new();
    private readonly Dictionary<int, PirataPNJController>     _controladores_pirata = new();
    private readonly Dictionary<int, PirataBrain>             _brainsPirata         = new();

    private int _diasDesdeUltimoReabastecimientoPirata = 0;

    /// <summary>
    /// Cascos del patrón Decorator disponibles para generar flotas PNJ.
    /// Asignar los mismos 4 assets CascoDecorador que usa AstilleroManager
    /// (CascoCog, CascoHulk, CascoCarraca, CascoGalera) desde el Inspector.
    /// </summary>
    [SerializeField] private List<CascoDecorador> _cascosParaPNJ;

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

    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Añade una flota PNJ al registro de flotas activas de la partida.
    /// Si ya existe una flota con el mismo <see cref="FlotaRuntimeData.Id"/>, la sobreescribe.
    /// </summary>
    /// <param name="flota">Datos de la flota a registrar. No puede ser <c>null</c>.</param>
    public void RegistrarFlota(FlotaRuntimeData flota)
    {
        if (flota == null) return;

        FlotasPorId[flota.Id] = flota;

        if (flota.IsPirata)
        {
            RutaCalculadorTilemap rutaCalculador = Object.FindFirstObjectByType<RutaCalculadorTilemap>();
            _controladores_pirata[flota.Id] = new PirataPNJController(flota, this, rutaCalculador);
        }
        else
        {
            _controladores[flota.Id] = new ComerciantePNJController(flota, this);
        }

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

        foreach (PirataPNJController controlador in _controladores_pirata.Values)
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
        }
    }

    /// <summary>
    /// Registra el <see cref="PirataBrain"/> asociado a una flota pirata.
    /// Llamado desde <see cref="PirataBrainBootstrapper"/> tras construir el brain.
    /// </summary>
    /// <param name="flotaId">Identificador de la flota pirata.</param>
    /// <param name="brain">Brain a registrar.</param>
    public void RegistrarPirataBrain(int flotaId, PirataBrain brain)
        => _brainsPirata[flotaId] = brain;

    /// <summary>
    /// Devuelve el <see cref="PirataBrain"/> asociado a la flota indicada,
    /// o <c>null</c> si no hay ninguno registrado para ese identificador.
    /// </summary>
    /// <param name="flotaId">Identificador de la flota pirata.</param>
    public PirataBrain ObtenerPirataBrain(int flotaId)
        => _brainsPirata.TryGetValue(flotaId, out PirataBrain b) ? b : null;

    /// <summary>
    /// Reasigna el RutaCalculadorTilemap a todos los controladores pirata.
    /// Llamar desde MapamundiController.Start() tras cargar la escena.
    /// </summary>
    public void AsignarRutaCalculadorAPiratas(RutaCalculadorTilemap rutaCalculador)
    {
        foreach (var kvp in _controladores_pirata)
            kvp.Value.AsignarRutaCalculador(rutaCalculador);
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
        if (ciudades == null || ciudades.Count == 0) return;

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
            AleatoriarStatsFlota(flota);
            RegistrarFlota(flota);
        }


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
            AleatoriarStatsFlota(flota);
            RegistrarFlota(flota);
        }

    }

    /// <summary>
    /// Genera entre 3 y 5 BarcoJugador con cascos aleatorios del patrón Decorator.
    /// Piratas prefieren cascos rápidos (Galera id=4, Cog id=1).
    /// Comerciantes prefieren cascos de carga (Hulk id=2, Carraca id=3, Cog id=1).
    /// Devuelve lista vacía si _cascosParaPNJ no está asignado.
    /// </summary>
    private List<BarcoJugador> GenerarBarcosAleatorios(bool esPirata)
    {
        var barcos = new List<BarcoJugador>();
        if (_cascosParaPNJ == null || _cascosParaPNJ.Count == 0) return barcos;

        int cantidad = Random.Range(3, 6); // 3, 4 o 5
        int idBase   = esPirata ? 9000 : 8000;

        for (int i = 0; i < cantidad; i++)
        {
            IBarco casco;
            if (esPirata)
            {
                float r = Random.value;
                if (r < 0.5f)
                    casco = _cascosParaPNJ.Find(c => c.IdTipoCasco == 4) ?? (IBarco)_cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
                else if (r < 0.8f)
                    casco = _cascosParaPNJ.Find(c => c.IdTipoCasco == 1) ?? (IBarco)_cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
                else
                    casco = _cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
            }
            else
            {
                float r = Random.value;
                if (r < 0.4f)
                    casco = _cascosParaPNJ.Find(c => c.IdTipoCasco == 2) ?? (IBarco)_cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
                else if (r < 0.7f)
                    casco = _cascosParaPNJ.Find(c => c.IdTipoCasco == 3) ?? (IBarco)_cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
                else if (r < 0.9f)
                    casco = _cascosParaPNJ.Find(c => c.IdTipoCasco == 1) ?? (IBarco)_cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
                else
                    casco = _cascosParaPNJ[Random.Range(0, _cascosParaPNJ.Count)];
            }
            var barco = new BarcoJugador(idBase + i, $"Barco_{idBase + i}", casco);
            barcos.Add(barco);
        }
        return barcos;
    }

    /// <summary>
    /// Calcula stats agregadas de una lista de BarcoJugador y las aplica
    /// al FlotaRuntimeData. Si la lista está vacía no modifica el runtime.
    /// </summary>
    private void AplicarStatsBarcos(FlotaRuntimeData runtime, List<BarcoJugador> barcos)
    {
        if (barcos == null || barcos.Count == 0) return;

        float vidaMax     = 0f;
        float fuerza      = 0f;
        float velMin      = float.MaxValue;
        int   tripulacion = 0;

        foreach (BarcoJugador barco in barcos)
        {
            vidaMax     += barco.VidaTotal;
            fuerza      += barco.FuerzaCombateTotal;
            tripulacion += barco.Tripulacion;
            if (barco.VelocidadTotal < velMin)
                velMin = barco.VelocidadTotal;
        }

        runtime.VidaMax        = vidaMax;
        runtime.VidaActual     = vidaMax;
        runtime.FuerzaCanhones = fuerza;
        runtime.VelocidadFlota = velMin == float.MaxValue ? 3f : velMin;
        runtime.NumBarcos      = barcos.Count;
        runtime.Tripulacion    = tripulacion;
    }

    /// <summary>
    /// Aplica stats de combate a una flota PNJ. Usa barcos reales del patrón Decorator
    /// si _cascosParaPNJ está asignado; si no, usa valores aleatorios como fallback.
    /// Llamar justo después de crear el FlotaRuntimeData y antes de registrarlo.
    /// </summary>
    private void AleatoriarStatsFlota(FlotaRuntimeData flota)
    {
        List<BarcoJugador> barcos = GenerarBarcosAleatorios(flota.IsPirata);
        if (barcos.Count > 0)
        {
            flota.BarcosFlota = barcos;
            AplicarStatsBarcos(flota, barcos);
            return;
        }

        // Fallback si _cascosParaPNJ no está asignado en el Inspector
        if (flota.IsPirata)
        {
            flota.VidaMax        = Random.Range(80f,  150f);
            flota.VidaActual     = flota.VidaMax;
            flota.FuerzaCanhones = Random.Range(15f,  35f);
            flota.VelocidadFlota = Random.Range(3f,   6f);
            flota.NumBarcos      = Random.Range(2,    5);
            flota.Tripulacion    = Random.Range(30,   80);
        }
        else
        {
            flota.VidaMax        = Random.Range(60f,  120f);
            flota.VidaActual     = flota.VidaMax;
            flota.FuerzaCanhones = Random.Range(3f,   12f);
            flota.VelocidadFlota = Random.Range(2f,   5f);
            flota.NumBarcos      = Random.Range(1,    4);
            flota.Tripulacion    = Random.Range(15,   50);
        }
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
        if (flota == null) return;

        flota.EstadoActual = nuevoEstado;
    }

    private void OnDestroy()
    {
        SimulacionTiempo.OnNuevoDia -= TickTodosLosControladores;
    }
}
