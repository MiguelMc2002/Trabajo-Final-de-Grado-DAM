using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Singleton persistente que coordina el guardado completo de una partida en SQLite.
/// Orquesta los DAOs respetando el orden de dependencias de claves foráneas: primero
/// los catálogos (Ciudad, Bien, TipoEdificio, TipoCasco), luego el estado económico
/// (EstadoMercadoCiudad) y finalmente los edificios de cada ciudad.
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor de guardado activo.</summary>
    public static SaveManager Instance { get; private set; }

    // ─── DAOs ─────────────────────────────────────────────────────────────────

    private EstadoJuegoDAO          _estadoJuegoDAO;
    private CiudadDAO               _ciudadDAO;
    private BienDAO                 _bienDAO;
    private EdificiosCiudadDAO      _edificiosDAO;
    private BarcoDAO                _barcoDAO;
    private EstadoMercadoCiudadDAO  _mercadoDAO;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ejecuta el guardado completo de la partida en el slot indicado.
    /// Abre (o crea) el fichero de base de datos correspondiente a través de
    /// <see cref="DatabaseManager"/>, instancia todos los DAOs y los invoca
    /// en el orden correcto para respetar la integridad referencial de SQLite.
    /// Si no hay ningún <see cref="MarketManager"/> activo en escena se omite
    /// el guardado del mercado y se registra un aviso en consola.
    /// </summary>
    /// <param name="slotIndex">Número de slot de guardado (1 a 5).</param>
    public void GuardarPartida(int slotIndex)
    {
        Debug.Log($"[SaveManager] Iniciando guardado en slot {slotIndex}...");

        DatabaseManager db = DatabaseManager.Instance;
        if (db == null)
        {
            Debug.LogError("[SaveManager] DatabaseManager no encontrado. No se puede guardar.");
            return;
        }

        db.InicializarSlot(slotIndex);
        InicializarDAOs(db);

        // Paso 1 — Estado de juego (tabla independiente, sin FKs)
        GuardarEstadoJuego();

        // Pasos 2-5 — Catálogos (INSERT OR IGNORE / INSERT OR REPLACE seguros)
        GuardarCatalogos();

        // Paso 6 — Estado económico del mercado de cada ciudad
        GuardarEstadoEconomico();

        // Paso 7 — Edificios de cada ciudad
        GuardarEdificios();

        Debug.Log($"[SaveManager] Guardado en slot {slotIndex} completado.");
    }

    // ─── Privados ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia todos los DAOs necesarios usando la conexión del
    /// <see cref="DatabaseManager"/> ya inicializada para este slot.
    /// </summary>
    /// <param name="db">Gestor de base de datos activo tras llamar a InicializarSlot.</param>
    private void InicializarDAOs(DatabaseManager db)
    {
        _estadoJuegoDAO = new EstadoJuegoDAO(db);
        _ciudadDAO      = new CiudadDAO(db);
        _bienDAO        = new BienDAO(db);
        _edificiosDAO   = new EdificiosCiudadDAO(db);
        _barcoDAO       = new BarcoDAO(db);
        _mercadoDAO     = new EstadoMercadoCiudadDAO(db);
    }

    /// <summary>
    /// Paso 1: persiste la fecha de juego y la velocidad de simulación actuales
    /// en la tabla estadoJuego. Siempre se sobrescribe la fila única (id_estado = 1).
    /// </summary>
    private void GuardarEstadoJuego()
    {
        SimulacionTiempo sim = SimulacionTiempo.Instance;
        if (sim == null)
        {
            Debug.LogWarning("[SaveManager] SimulacionTiempo no encontrado; se guarda estado de juego con valores por defecto.");
            _estadoJuegoDAO.Guardar(1, 1, 1290, 1);
            return;
        }

        int velocidadEntero = Mathf.RoundToInt(sim.VelocidadActual * 100f);
        _estadoJuegoDAO.Guardar(sim.DiaActual, sim.MesActual, sim.AñoActual, velocidadEntero);
        Debug.Log($"[SaveManager] EstadoJuego guardado — {sim.DiaActual}/{sim.MesActual}/{sim.AñoActual} vel={sim.VelocidadActual}x");
    }

    /// <summary>
    /// Pasos 2-5: inserta los catálogos de Ciudad, Bien, TipoEdificio y TipoCasco.
    /// Todas las operaciones usan INSERT OR IGNORE / INSERT OR REPLACE, por lo que
    /// es seguro llamarlas tanto en partidas nuevas como al sobreescribir un slot.
    /// </summary>
    private void GuardarCatalogos()
    {
        // Paso 2 — Ciudades
        _ciudadDAO.InsertarCiudadesIniciales();
        Debug.Log("[SaveManager] Catálogo de ciudades guardado.");

        // Paso 3 — Bienes
        _bienDAO.InsertarBienesIniciales();
        Debug.Log("[SaveManager] Catálogo de bienes guardado.");

        // Paso 4 — Tipos de edificio
        _edificiosDAO.InsertarTiposEdificioSiNoExisten();
        Debug.Log("[SaveManager] Catálogo de tipos de edificio guardado.");

        // Paso 5 — Tipos de casco
        _barcoDAO.InsertarTiposCascoSiNoExisten();
        Debug.Log("[SaveManager] Catálogo de tipos de casco guardado.");
    }

    /// <summary>
    /// Paso 6: persiste el estado del mercado de la ciudad actualmente cargada en escena.
    /// Solo hay un <see cref="MarketManager"/> activo a la vez, por lo que únicamente se
    /// guarda el mercado de su ciudad asignada. Si no hay MarketManager o no tiene
    /// <see cref="CiudadData"/>, omite el paso con un aviso.
    /// </summary>
    private void GuardarEstadoEconomico()
    {
        MarketManager market = FindAnyObjectByType<MarketManager>();
        if (market == null)
        {
            Debug.LogWarning("[SaveManager] MarketManager no encontrado en escena; se omite el guardado del mercado.");
            return;
        }

        CiudadData ciudad = market.DatosCiudad;
        if (ciudad == null)
        {
            Debug.LogWarning("[SaveManager] El MarketManager en escena no tiene CiudadData asignada; se omite el guardado del mercado.");
            return;
        }

        _mercadoDAO.GuardarTodoElMercado(ciudad, market);
        Debug.Log($"[SaveManager] Mercado de '{ciudad.NombreCiudad}' guardado.");
    }

    /// <summary>
    /// Paso 7: recorre todos los <see cref="CiudadData"/> del proyecto e inserta
    /// los edificios iniciales de cada ciudad que todavía no los tenga registrados.
    /// La operación es idempotente gracias al INSERT OR REPLACE interno del DAO.
    /// </summary>
    private void GuardarEdificios()
    {
        CiudadData[] ciudades = Resources.FindObjectsOfTypeAll<CiudadData>();
        if (ciudades == null || ciudades.Length == 0)
        {
            Debug.LogWarning("[SaveManager] No se encontró ningún CiudadData en el proyecto; se omite el guardado de edificios.");
            return;
        }

        foreach (CiudadData ciudad in ciudades)
        {
            _edificiosDAO.InsertarEdificiosInicialesCiudad(ciudad.IdCiudad);
            Debug.Log($"[SaveManager] Edificios de '{ciudad.NombreCiudad}' guardados.");
        }
    }
}
