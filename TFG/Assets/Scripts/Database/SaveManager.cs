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
    private FlotaDAO                _flotaDAO;
    private EstadoMercadoCiudadDAO  _mercadoDAO;
    private AlmacenJugadorDAO       _almacenJugadorDAO;
    private AlmacenCiudadDAO        _almacenCiudadDAO;
    private ModuloBarcoDAO          _moduloBarcoDAO;
    private CapitanDAO              _capitanDAO;

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

        // Paso 6b — Almacén de ciudad del jugador
        GuardarAlmacenCiudad();

        // Paso 6c — Flotas PNJ
        GuardarFlotasPNJ();

        // Paso 7 — Estado económico del mercado de cada ciudad
        GuardarEstadoEconomico();

        // Paso 8 — Edificios de cada ciudad
        GuardarEdificios();

        // Paso 9 — Flota del jugador (barcos + módulos)
        GuardarFlotaJugador();

        // Paso 10 — Capitanes contratados
        GuardarCapitanes();

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
        _flotaDAO          = new FlotaDAO(db);
        _mercadoDAO        = new EstadoMercadoCiudadDAO(db);
        _almacenJugadorDAO = new AlmacenJugadorDAO(db);
        _almacenCiudadDAO  = new AlmacenCiudadDAO(db);
        _moduloBarcoDAO    = new ModuloBarcoDAO(db);
        _capitanDAO        = new CapitanDAO(db);
        GameManager.Instance?.InyectarAlmacenCiudadDAO(_almacenCiudadDAO);
    }

    /// <summary>
    /// Paso 1: persiste la fecha de juego y la velocidad de simulación actuales
    /// en la tabla estadoJuego. Siempre se sobrescribe la fila única (id_estado = 1).
    /// </summary>
    private void GuardarEstadoJuego()
    {
        SimulacionTiempo sim = SimulacionTiempo.Instance;
        bool modoPirata = GameManager.Instance?.FlotaJugador?.ModoPirata ?? false;

        if (sim == null)
        {
            long dineroPorDefecto = GameManager.Instance != null ? GameManager.Instance.Dinero : 999_999_999L;
            Debug.LogWarning("[SaveManager] SimulacionTiempo no encontrado; se guarda estado de juego con valores por defecto.");
            _estadoJuegoDAO.Guardar(1, 1, 1290, 1, dineroPorDefecto, modoPirata);
            return;
        }

        int  velocidadEntero = Mathf.RoundToInt(sim.VelocidadActual * 100f);
        long dinero          = GameManager.Instance != null ? GameManager.Instance.Dinero : 999_999_999L;
        _estadoJuegoDAO.Guardar(sim.DiaActual, sim.MesActual, sim.AñoActual, velocidadEntero, dinero, modoPirata);
        Debug.Log($"[SaveManager] EstadoJuego guardado — {sim.DiaActual}/{sim.MesActual}/{sim.AñoActual} vel={sim.VelocidadActual}x dinero={dinero:N0} modoPirata={modoPirata}");
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
    /// Paso 6b: persiste el almacén de ciudad del jugador en AlmacenCiudadJugador.
    /// Itera todas las ciudades disponibles, obtiene el diccionario en memoria de cada una
    /// y llama a <see cref="AlmacenCiudadDAO.SetCantidad"/> por cada par id_bien/cantidad.
    /// Si GameManager no está disponible, omite el paso con un aviso.
    /// </summary>
    private void GuardarAlmacenCiudad()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[SaveManager] GameManager.Instance es null; se omite el guardado del almacén de ciudad.");
            return;
        }

        IReadOnlyList<CiudadData> ciudades = GameManager.Instance.CiudadesDisponibles;
        if (ciudades == null) return;

        foreach (CiudadData ciudad in ciudades)
        {
            if (ciudad == null) continue;

            // Borrar primero para que los bienes vaciados no queden como entradas huérfanas
            _almacenCiudadDAO.LimpiarCiudad(ciudad.IdCiudad);

            Dictionary<int, int> almacen = GameManager.Instance.GetAlmacenCiudad(ciudad.IdCiudad);
            foreach (KeyValuePair<int, int> par in almacen)
                _almacenCiudadDAO.SetCantidad(ciudad.IdCiudad, par.Key, par.Value);
        }

        Debug.Log("[SaveManager] Almacén de ciudad guardado.");
    }

    /// <summary>
    /// Paso 6c: persiste el estado de todas las flotas PNJ activas en las tablas
    /// <c>FlotaPNJ</c> y <c>CargaFlotaPNJ</c>. Primero borra las filas existentes
    /// de cada flota y luego las reinserta para garantizar consistencia.
    /// Si <see cref="FlotaManager"/> no está disponible, omite el paso con un aviso.
    /// </summary>
    private void GuardarFlotasPNJ()
    {
        if (FlotaManager.Instance == null)
        {
            Debug.LogWarning("[SaveManager] FlotaManager.Instance es null; se omite el guardado de flotas PNJ.");
            return;
        }

        Mono.Data.Sqlite.SqliteConnection conexion = DatabaseManager.Instance.Conexion;
        System.Collections.Generic.IReadOnlyCollection<FlotaRuntimeData> flotas =
            FlotaManager.Instance.ObtenerTodasLasFlotas();
        int guardadas = 0;

        // Vaciar las tablas antes de reescribirlas para que las flotas destruidas
        // entre guardados no resuciten en la siguiente carga
        try
        {
            using (Mono.Data.Sqlite.SqliteCommand cmd = conexion.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CargaFlotaPNJ; DELETE FROM FlotaPNJ;";
                cmd.ExecuteNonQuery();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveManager] Error al limpiar tablas FlotaPNJ/CargaFlotaPNJ: {ex}");
        }

        foreach (FlotaRuntimeData flota in flotas)
        {
            try
            {
                using (Mono.Data.Sqlite.SqliteCommand cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO FlotaPNJ
                            (id, nombre_propietario, ciudad_origen_id, ciudad_destino_id, estado,
                             posicion_actual_x, posicion_actual_y,
                             casilla_destino_x, casilla_destino_y, casilla_destino_z)
                        VALUES
                            (@id, @nombre, @origen, @destino, @estado,
                             @posX, @posY,
                             @cDestX, @cDestY, @cDestZ);";
                    cmd.Parameters.AddWithValue("@id",      flota.Id);
                    cmd.Parameters.AddWithValue("@nombre",  flota.NombrePropietario);
                    cmd.Parameters.AddWithValue("@origen",  flota.CiudadOrigenId);
                    cmd.Parameters.AddWithValue("@destino", flota.CiudadDestinoId);
                    cmd.Parameters.AddWithValue("@estado",  flota.EstadoActual.ToString());
                    cmd.Parameters.AddWithValue("@posX",    flota.PosicionActual.x);
                    cmd.Parameters.AddWithValue("@posY",    flota.PosicionActual.y);
                    cmd.Parameters.AddWithValue("@cDestX",  flota.CasillaDestino.x);
                    cmd.Parameters.AddWithValue("@cDestY",  flota.CasillaDestino.y);
                    cmd.Parameters.AddWithValue("@cDestZ",  flota.CasillaDestino.z);
                    cmd.ExecuteNonQuery();
                }

                foreach (System.Collections.Generic.KeyValuePair<int, int> par in flota.Carga)
                {
                    if (par.Value <= 0) continue;

                    using (Mono.Data.Sqlite.SqliteCommand cmd = conexion.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO CargaFlotaPNJ (id_flota, id_bien, cantidad)
                            VALUES (@idFlota, @idBien, @cantidad);";
                        cmd.Parameters.AddWithValue("@idFlota",  flota.Id);
                        cmd.Parameters.AddWithValue("@idBien",   par.Key);
                        cmd.Parameters.AddWithValue("@cantidad", par.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                guardadas++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] Error al guardar flota PNJ id={flota.Id}: {ex}");
            }
        }

        Debug.Log($"[SaveManager] {guardadas} flotas PNJ guardadas en FlotaPNJ/CargaFlotaPNJ.");
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
    /// Paso 9: persiste todos los barcos de la flota del jugador en las tablas Barco y ModuloBarco.
    /// Usa id_flota = 0 como convención para barcos del jugador.
    /// </summary>
    private void GuardarFlotaJugador()
    {
        FlotaJugador flota = GameManager.Instance?.FlotaJugador;
        if (flota == null || flota.Barcos.Count == 0)
        {
            Debug.Log("[SaveManager] Flota del jugador vacía; se omite el guardado.");
            return;
        }

        // Garantizar que existe la fila Flota con id=0 (jugador) antes de insertar barcos:
        // la tabla Barco tiene FK id_flota → Flota(id_flota), por lo que sin esta fila
        // SQLite lanza FOREIGN KEY constraint failed al hacer INSERT en Barco.
        int? idCiudadJugador = GameManager.Instance?.CiudadActual?.IdCiudad;
        _flotaDAO.InsertarFlota(0, "jugador", idCiudadJugador, 0f, 0f, null, "EnPuerto", null);

        // Borrar barcos y módulos anteriores del jugador para evitar barcos fantasma
        _barcoDAO.EliminarBarcosDeFlota(0);

        int guardados = 0;
        foreach (BarcoJugador barco in flota.Barcos)
        {
            // Protección ante CascoBase null: NRE antes de entrar en el try/catch del DAO
            if (barco.CascoBase == null)
            {
                Debug.LogError($"[SaveManager] Barco id={barco.IdBarco} ('{barco.Nombre}') no tiene CascoBase; se omite.");
                continue;
            }

            _barcoDAO.InsertarBarco(
                barco.IdBarco,
                barco.CascoBase.IdTipoCasco,
                barco.Nombre,
                barco.EsBarcosCombate,
                barco.VidaActual,
                barco.Tripulacion,
                barco.CascoBase.CapacidadTripulacion,
                idFlota: 0);

            _moduloBarcoDAO.GuardarModulosDeBarco(barco.IdBarco, barco.ModulosInstalados);
            guardados++;
        }

        Debug.Log($"[SaveManager] {guardados} barcos del jugador guardados en Barco/ModuloBarco.");
    }

    /// <summary>
    /// Paso 10: persiste todos los capitanes contratados por el jugador en la tabla Capitan.
    /// Borra primero las filas anteriores para garantizar consistencia.
    /// </summary>
    private void GuardarCapitanes()
    {
        if (TabernaManager.Instance == null)
        {
            Debug.Log("[SaveManager] TabernaManager no disponible; se omite el guardado de capitanes.");
            return;
        }

        _capitanDAO.EliminarTodosLosCapitanes();

        int guardados = 0;
        foreach (CapitanData capitan in TabernaManager.Instance.CapitanesContratados)
        {
            _capitanDAO.GuardarCapitan(capitan, capitan.IdBarcoAsignado);
            guardados++;
        }

        Debug.Log($"[SaveManager] {guardados} capitanes guardados en Capitan.");
    }

    /// <summary>
    /// Paso 7: recorre todos los <see cref="CiudadData"/> del proyecto e inserta
    /// los edificios iniciales de cada ciudad que todavía no los tenga registrados.
    /// La operación es idempotente gracias al INSERT OR REPLACE interno del DAO.
    /// </summary>
    private void GuardarEdificios()
    {
        IReadOnlyList<CiudadData> ciudades = GameManager.Instance?.CiudadesDisponibles;
        if (ciudades == null || ciudades.Count == 0)
        {
            Debug.LogWarning("[SaveManager] GameManager.CiudadesDisponibles vacío; se omite el guardado de edificios.");
            return;
        }

        foreach (CiudadData ciudad in ciudades)
        {
            _edificiosDAO.InsertarEdificiosInicialesCiudad(ciudad.IdCiudad);
            Debug.Log($"[SaveManager] Edificios de '{ciudad.NombreCiudad}' guardados.");
        }
    }
}
