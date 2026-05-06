using System.Collections.Generic;
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
    private AlmacenJugadorDAO       _almacenJugadorDAO;

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

        // Paso 6 — Almacén del jugador (FK a Bien, debe ir después de catálogos)
        GuardarAlmacenJugador();

        // Paso 7 — Estado económico del mercado de cada ciudad
        GuardarEstadoEconomico();

        // Paso 8 — Edificios de cada ciudad
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
        _estadoJuegoDAO    = new EstadoJuegoDAO(db);
        _ciudadDAO         = new CiudadDAO(db);
        _bienDAO           = new BienDAO(db);
        _edificiosDAO      = new EdificiosCiudadDAO(db);
        _barcoDAO          = new BarcoDAO(db);
        _mercadoDAO        = new EstadoMercadoCiudadDAO(db);
        _almacenJugadorDAO = new AlmacenJugadorDAO(db);
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
            long dineroPorDefecto = GameManager.Instance != null ? GameManager.Instance.Dinero : 999_999_999L;
            Debug.LogWarning("[SaveManager] SimulacionTiempo no encontrado; se guarda estado de juego con valores por defecto.");
            _estadoJuegoDAO.Guardar(1, 1, 1290, 1, dineroPorDefecto);
            return;
        }

        int  velocidadEntero = Mathf.RoundToInt(sim.VelocidadActual * 100f);
        long dinero          = GameManager.Instance != null ? GameManager.Instance.Dinero : 999_999_999L;
        _estadoJuegoDAO.Guardar(sim.DiaActual, sim.MesActual, sim.AñoActual, velocidadEntero, dinero);
        Debug.Log($"[SaveManager] EstadoJuego guardado — {sim.DiaActual}/{sim.MesActual}/{sim.AñoActual} vel={sim.VelocidadActual}x dinero={dinero:N0}");
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
    /// Paso 6: persiste el inventario del jugador en AlmacenJugador.
    /// Itera GameManager.GetAlmacen(), resuelve el id_bien de cada BienData
    /// usando el catálogo en memoria de BienDAO y delega en AlmacenJugadorDAO.
    /// Si GameManager no está disponible, omite el paso con un aviso.
    /// </summary>
    private void GuardarAlmacenJugador()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[SaveManager] GameManager.Instance es null; se omite el guardado del almacén.");
            return;
        }

        // Construir mapa nombre → id_bien a partir de los bienes ya insertados en BD
        List<BienDto> bienDtos = _bienDAO.ObtenerTodosLosBienes();
        var idPorNombre = new Dictionary<string, int>(bienDtos.Count);
        foreach (BienDto dto in bienDtos)
            idPorNombre[dto.Nombre] = dto.IdBien;

        IReadOnlyDictionary<BienData, int> almacen = GameManager.Instance.GetAlmacen();
        var almacenPorId = new Dictionary<int, int>(almacen.Count);

        foreach (KeyValuePair<BienData, int> par in almacen)
        {
            if (par.Key == null || par.Value <= 0) continue;

            if (!idPorNombre.TryGetValue(par.Key.nombre, out int idBien))
            {
                Debug.LogWarning($"[SaveManager] Bien '{par.Key.nombre}' no encontrado en BD; se omite del almacén guardado.");
                continue;
            }

            almacenPorId[idBien] = par.Value;
        }

        _almacenJugadorDAO.GuardarAlmacen(almacenPorId);
        Debug.Log($"[SaveManager] Guardadas {almacenPorId.Count} entradas en AlmacenJugador.");
    }

    /// <summary>
    /// Paso 7: persiste el estado del mercado de todas las ciudades registradas en
    /// <see cref="GameManager.MercadosPorCiudad"/>. Si GameManager no está disponible
    /// o el diccionario está vacío, omite el paso con un aviso.
    /// </summary>
    private void GuardarEstadoEconomico()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[SaveManager] GameManager.Instance es null; se omite el guardado de mercados.");
            return;
        }

        IReadOnlyDictionary<int, List<EntradaMercado>> mercados = GameManager.Instance.MercadosPorCiudad;
        if (mercados == null || mercados.Count == 0)
        {
            Debug.LogWarning("[SaveManager] No hay mercados registrados en GameManager; se omite el paso de mercados.");
            return;
        }

        int ciudadesGuardadas = 0;
        foreach (KeyValuePair<int, List<EntradaMercado>> kv in mercados)
        {
            _mercadoDAO.GuardarTodoElMercado(kv.Key, kv.Value);
            ciudadesGuardadas++;
        }

        Debug.Log($"[SaveManager] Guardados {ciudadesGuardadas} mercados en EstadoMercadoCiudad.");
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
