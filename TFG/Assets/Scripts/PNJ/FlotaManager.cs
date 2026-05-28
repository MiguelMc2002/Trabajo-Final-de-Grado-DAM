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

    private int  _diasDesdeUltimoReabastecimientoPirata = 0;
    private bool _respawnPendiente                      = false;
    private bool _respawnPirataPendiente                = false;

    private const int MaxComerciantesActivos = 20;
    private const int MaxPiratasActivos      = 3;

    /// <summary>
    /// Cascos del patrón Decorator disponibles para generar flotas PNJ.
    /// Asignar los mismos 4 assets CascoDecorador que usa AstilleroManager
    /// (CascoCog, CascoHulk, CascoCarraca, CascoGalera) desde el Inspector.
    /// </summary>
    [SerializeField] private List<CascoDecorador>    _cascosParaPNJ;

    /// <summary>
    /// Módulos disponibles para instalar aleatoriamente en los barcos PNJ.
    /// Asignar desde el Inspector con todos los ModuloBarcoData del proyecto.
    /// </summary>
    [SerializeField] private List<ModuloBarcoData> _modulosParaPNJ = new List<ModuloBarcoData>();

    // ─── Pools de nombres ────────────────────────────────────────────────────

    private static readonly string[] _nombresComerciantesPool = new string[]
    {
        // Alemanes (Haus / Kontor)
        "Haus Becker", "Haus Richter", "Kontor Bremen", "Haus Steinhoff",
        "Kontor Lübeck", "Haus Meissner", "Kontor Danzig", "Haus Braun",
        "Kontor Hamburg", "Haus Schreiber", "Kontor Rostock", "Haus Vogel",
        // Españoles (Casa / Compañía)
        "Casa Mendoza", "Compañía Castilla", "Casa Fernández", "Compañía del Mar",
        "Casa Gutiérrez", "Compañía Ibérica", "Casa Álvarez", "Compañía Aragón",
        "Casa Morales", "Compañía de Levante", "Casa Herrero", "Compañía del Norte",
        // Italianos (Compagnia / Banco / Casa)
        "Compagnia Rossi", "Banco Fiorentino", "Casa Lombardi", "Compagnia del Porto",
        "Banco Veneziano", "Casa Genovese", "Compagnia Marinara", "Banco Adriatico",
        "Casa Toscana", "Compagnia Medici", "Banco Ligure", "Casa Visconti",
        // Holandeses (Huis / Maatschappij)
        "Huis Van der Berg", "Maatschappij Noord", "Huis De Groot", "Maatschappij Zeeland",
        "Huis Janssen", "Maatschappij Holland", "Huis Vermeer", "Maatschappij Vlissingen",
        "Huis De Vries", "Maatschappij Brugge", "Huis Baaker", "Maatschappij Antwerpen",
        // Franceses (Maison / Compagnie / Société)
        "Maison Dupont", "Compagnie du Nord", "Société Maritime", "Maison Leblanc",
        "Compagnie de Rouen", "Maison Bernard", "Société des Mers", "Compagnie Normande",
        "Maison Girard", "Société Atlantique", "Maison Fournier", "Compagnie du Ponant",
    };

    private static readonly string[] _nombresPiratasPool = new string[]
    {
        "Pirata Störtebeker", "Pirata Gödeke Michels", "Pirata Klaus Scheld",
        "Pirata Klaus Mewes", "Pirata Heinrich Garvermann", "Pirata Simon de Utrecht",
        "Pirata Arend Dikke", "Pirata Wigbold von Ruinen", "Pirata Herman de Spelde",
        "Pirata Albert van der Vere", "Pirata Cord Widenfelt", "Pirata Hannes von Rostock",
        "Pirata Niklaus van der Berg", "Pirata Johann Moltke", "Pirata Bernd Hollenbeke",
        "Pirata Friedrich von Hagen", "Pirata Wilhelm Schulte", "Pirata Dietrich der Schwarze",
    };

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

        if (_cascosParaPNJ == null || _cascosParaPNJ.Count == 0)
            Debug.LogError("[FlotaManager] _cascosParaPNJ no asignado — hay que asignarlos en el Inspector de MenuPrincipal.");

        if (_modulosParaPNJ == null || _modulosParaPNJ.Count == 0)
            Debug.LogError("[FlotaManager] _modulosParaPNJ no asignado — hay que asignarlos en el Inspector de MenuPrincipal.");
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
    /// Elimina todas las flotas PNJ, sus controladores y sus brains del registro activo.
    /// Llamar exclusivamente desde <see cref="LoadManager"/> antes de restaurar
    /// las flotas desde la base de datos, para evitar flotas fantasma cuando el singleton
    /// persiste entre cargas.
    /// </summary>
    public void LimpiarTodasLasFlotas()
    {
        FlotasPorId.Clear();
        _controladores.Clear();
        _controladores_pirata.Clear();
        _brainsPirata.Clear();
    }

    /// <summary>
    /// Restablece los stats de combate de una flota cargada desde BD llamando al mismo
    /// proceso de aleatorización que se usa al crear flotas nuevas. Llamar desde
    /// <see cref="LoadManager"/> tras registrar cada flota para evitar que las flotas
    /// cargadas tengan siempre los valores por defecto del constructor.
    /// </summary>
    /// <param name="flota">Flota recién registrada cuyas stats se van a restablecer.</param>
    public void RestablecerStatsParaCarga(FlotaRuntimeData flota)
    {
        if (flota != null)
            AleatoriarStatsFlota(flota);
    }

    /// <summary>
    /// Elimina una flota del registro activo, limpia su icono en el mapamundi
    /// y borra sus filas de la base de datos. Si la flota eliminada era un comerciante,
    /// activa el proceso de respawn de comerciantes; si era un pirata, activa el de piratas.
    /// </summary>
    /// <param name="idFlota">Identificador de la flota a eliminar.</param>
    public void EliminarFlota(int idFlota)
    {
        if (!FlotasPorId.TryGetValue(idFlota, out FlotaRuntimeData flota)) return;

        bool eraComerciant = !flota.IsPirata;

        MapamundiController.Instance?.DesactivarIconoFlota(idFlota);

        if (DatabaseManager.Instance?.Conexion != null)
        {
            try
            {
                using (var cmd = DatabaseManager.Instance.Conexion.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM MemoriaComercialPNJ WHERE id_flota = @id;";
                    cmd.Parameters.Add(new Mono.Data.Sqlite.SqliteParameter("@id", idFlota));
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = DatabaseManager.Instance.Conexion.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CargaFlotaPNJ WHERE id_flota = @id;";
                    cmd.Parameters.Add(new Mono.Data.Sqlite.SqliteParameter("@id", idFlota));
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = DatabaseManager.Instance.Conexion.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FlotaPNJ WHERE id = @id;";
                    cmd.Parameters.Add(new Mono.Data.Sqlite.SqliteParameter("@id", idFlota));
                    cmd.ExecuteNonQuery();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FlotaManager] Error al limpiar BD de flota {idFlota}: {ex.Message}");
            }
        }

        FlotasPorId.Remove(idFlota);
        _controladores.Remove(idFlota);
        _controladores_pirata.Remove(idFlota);
        _brainsPirata.Remove(idFlota);

        if (eraComerciant)
            RespawnComercianteSiNecesario();
        else
            RespawnPirataSiNecesario();
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
    /// Crea y registra los 20 comerciantes y 3 piratas PNJ iniciales al comenzar una partida nueva.
    /// Garantiza que las ciudades tienen mercado inicializado antes de crear las flotas.
    /// Los IDs de comerciantes van del 1001 al 1020; los de piratas del 2001 al 2003.
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
            (1019, "Comerciante Burkhard",  0),
            (1020, "Comerciante Volker",    1),
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

    // ─── Respawn de comerciantes ──────────────────────────────────────────────

    private int ContarComerciantesActivos()
    {
        int count = 0;
        foreach (FlotaRuntimeData flota in FlotasPorId.Values)
            if (!flota.IsPirata && flota.Id != -1)
                count++;
        return count;
    }

    private void RespawnComercianteSiNecesario()
    {
        if (ContarComerciantesActivos() >= MaxComerciantesActivos) return;

        if (!CombateEventos.CombateJugadorEnCurso)
        {
            SpawnComercianteAleatorio();
        }
        else if (!_respawnPendiente)
        {
            _respawnPendiente = true;
            CombateEventos.OnCombateTerminado += EjecutarRespawnDiferido;
        }
    }

    private void EjecutarRespawnDiferido()
    {
        CombateEventos.OnCombateTerminado -= EjecutarRespawnDiferido;
        _respawnPendiente = false;
        SpawnComercianteAleatorio();
    }

    private void SpawnComercianteAleatorio()
    {
        IReadOnlyList<CiudadData> ciudades = GameManager.Instance?.CiudadesDisponibles;
        if (ciudades == null || ciudades.Count == 0) return;

        // Nuevo ID en rango de comerciantes (1001-1999), por encima del máximo existente en ese rango
        int nuevoId = 1001;
        foreach (int id in FlotasPorId.Keys)
            if (id < 2000 && id >= nuevoId)
                nuevoId = id + 1;

        if (nuevoId > 1999)
        {
            Debug.LogWarning("[FlotaManager] Rango de IDs de comerciantes agotado (1001-1999).");
            return;
        }

        CiudadData ciudad = ciudades[Random.Range(0, ciudades.Count)];
        string nombre = GenerarNombreComercianteUnico(nuevoId);

        FlotaRuntimeData flota = new FlotaRuntimeData(nuevoId, nombre);
        flota.CiudadOrigenId = ciudad.IdCiudad;
        AleatoriarStatsFlota(flota);
        RegistrarFlota(flota);
        MapamundiController.Instance?.SpawnIconoFlotaPNJ(flota);

        Debug.Log($"[FlotaManager] Respawn comerciante: {nombre} (id={nuevoId}) en {ciudad.NombreCiudad}.");
    }

    private string GenerarNombreComercianteUnico(int idFallback)
    {
        var nombresUsados = new HashSet<string>();
        foreach (FlotaRuntimeData f in FlotasPorId.Values)
            nombresUsados.Add(f.NombrePropietario);

        for (int i = 0; i < 15; i++)
        {
            string candidato = _nombresComerciantesPool[Random.Range(0, _nombresComerciantesPool.Length)];
            if (!nombresUsados.Contains(candidato))
                return candidato;
        }
        return "Mercader_" + idFallback;
    }

    // ─── Respawn de piratas ───────────────────────────────────────────────────

    private int ContarPiratasActivos()
    {
        int count = 0;
        foreach (FlotaRuntimeData flota in FlotasPorId.Values)
            if (flota.IsPirata)
                count++;
        return count;
    }

    private void RespawnPirataSiNecesario()
    {
        if (ContarPiratasActivos() >= MaxPiratasActivos) return;

        if (!CombateEventos.CombateJugadorEnCurso)
        {
            SpawnPirataAleatorio();
        }
        else if (!_respawnPirataPendiente)
        {
            _respawnPirataPendiente = true;
            CombateEventos.OnCombateTerminado += EjecutarRespawnPirataDiferido;
        }
    }

    private void EjecutarRespawnPirataDiferido()
    {
        CombateEventos.OnCombateTerminado -= EjecutarRespawnPirataDiferido;
        _respawnPirataPendiente = false;
        SpawnPirataAleatorio();
    }

    private void SpawnPirataAleatorio()
    {
        // Nuevo ID en rango de piratas (2001-2999)
        int nuevoId = 2001;
        foreach (int id in FlotasPorId.Keys)
            if (id >= 2001 && id < 3000 && id >= nuevoId)
                nuevoId = id + 1;

        if (nuevoId >= 3000)
        {
            Debug.LogWarning("[FlotaManager] Rango de IDs de piratas agotado (2001-2999).");
            return;
        }

        string nombre = GenerarNombrePirataUnico(nuevoId);

        FlotaRuntimeData flota = new FlotaRuntimeData(nuevoId, nombre, esPirata: true);
        flota.CiudadOrigenId  = -1;
        flota.CiudadDestinoId = -1;
        flota.EstadoActual    = EstadoFlotaPNJ.Patrullando;

        // Posicionar en casilla de mar válida; fallback a posición aleatoria simple
        if (MapamundiController.Instance != null)
        {
            Vector2 posMar = MapamundiController.Instance.ObtenerPosicionMarAleatoria();
            flota.PosicionActual = posMar != Vector2.zero
                ? posMar
                : new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
        }
        else
        {
            flota.PosicionActual = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
        }

        AleatoriarStatsFlota(flota);
        RegistrarFlota(flota);
        MapamundiController.Instance?.SpawnIconoFlotaPNJ(flota);
        PirataBrainBootstrapper.Instance?.CrearBrainParaPirata(flota);

        Debug.Log($"[FlotaManager] Respawn pirata: {nombre} (id={nuevoId}) en {flota.PosicionActual}.");
    }

    private string GenerarNombrePirataUnico(int idFallback)
    {
        var nombresUsados = new HashSet<string>();
        foreach (FlotaRuntimeData f in FlotasPorId.Values)
            nombresUsados.Add(f.NombrePropietario);

        for (int i = 0; i < 15; i++)
        {
            string candidato = _nombresPiratasPool[Random.Range(0, _nombresPiratasPool.Length)];
            if (!nombresUsados.Contains(candidato))
                return candidato;
        }
        return "Pirata_" + idFallback;
    }

    // ─── Generación de flotas ─────────────────────────────────────────────────

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

        string[] nombresPirata = {
            "Der Rabe", "Schwarzer Tod", "Die Klinge", "Meereswolf", "Sturmvogel",
            "Der Geier", "Blutmond", "Eisenfaust", "Der Schrecken", "Dunkle Woge",
            "Klaus Störtebeker", "Gödeke Michels", "Der Pirat", "Todesvogel", "Rote Hand"
        };
        string[] nombresComercio = {
            "Der Adler", "Hansekogge", "Die Möwe", "Lubecker Bär", "Nordstern",
            "Gott Mit Uns", "Das Einhorn", "Silberfisch", "Meereswind", "Eisvogel",
            "Santa María", "San Juan", "La Esperanza", "El Halcón", "Mar del Norte",
            "La Fortuna", "Viento del Sur", "San Cristóbal", "La Paloma", "Stella Maris",
            "De Gouden Leeuw", "Het Anker", "De Zeemeeuw", "L'Étoile du Nord", "Le Lion d'Or"
        };
        string[] nombresLista = esPirata ? nombresPirata : nombresComercio;

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
            string nombre = nombresLista[Random.Range(0, nombresLista.Length)];
            var barco = new BarcoJugador(idBase + i, nombre, casco);
            // Piratas arman todos sus barcos; comerciantes solo tienen armamento el 20% de las veces
            if (esPirata || Random.value < 0.20f)
                InstalarModuloAleatorioSiCabe(barco, TipoModulo.Armamento);
            InstalarModuloAleatorioSiCabe(barco, TipoModulo.Velas);
            InstalarModuloAleatorioSiCabe(barco, TipoModulo.Bodega);

            // Asignar tripulación: piratas al máximo, comerciantes entre 30% y 80%
            barco.Tripulacion = esPirata
                ? barco.TripulacionMaxima
                : Mathf.RoundToInt(barco.TripulacionMaxima * Random.Range(0.3f, 0.8f));

            barcos.Add(barco);
        }
        return barcos;
    }

    private void InstalarModuloAleatorioSiCabe(BarcoJugador barco, TipoModulo tipo)
    {
        if (_modulosParaPNJ == null || _modulosParaPNJ.Count == 0) return;

        int anioActual = SimulacionTiempo.Instance != null
            ? SimulacionTiempo.Instance.AñoActual
            : ModuloBarcoData.AnioDesbloqueoPolvoraJuego - 1;

        // Un solo módulo por tipo por barco
        foreach (ModuloBarcoData instalado in barco.ModulosInstalados)
            if (instalado.tipoModulo == tipo) return;

        var candidatos = new List<ModuloBarcoData>();
        foreach (ModuloBarcoData m in _modulosParaPNJ)
        {
            if (m == null) continue;
            if (m.tipoModulo != tipo) continue;
            if (m.slotsCosto > barco.SlotsDisponibles) continue;
            if (m.requierePolvora && anioActual < ModuloBarcoData.AnioDesbloqueoPolvoraJuego) continue;
            candidatos.Add(m);
        }

        if (candidatos.Count == 0) return;
        barco.InstalarModulo(candidatos[Random.Range(0, candidatos.Count)]);
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
            flota.FuerzaCanhones = Random.Range(0f,   2f);
            flota.VelocidadFlota = Random.Range(2f,   5f);
            flota.NumBarcos      = Random.Range(1,    4);
            flota.Tripulacion    = Random.Range(15,   50);
        }
    }

    // ─── Internos ─────────────────────────────────────────────────────────────

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

    private void OnDestroy()
    {
        SimulacionTiempo.OnNuevoDia -= TickTodosLosControladores;

        if (_respawnPendiente)
            CombateEventos.OnCombateTerminado -= EjecutarRespawnDiferido;

        if (_respawnPirataPendiente)
            CombateEventos.OnCombateTerminado -= EjecutarRespawnPirataDiferido;
    }
}
