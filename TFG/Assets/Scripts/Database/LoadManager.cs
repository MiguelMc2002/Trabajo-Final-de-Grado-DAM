using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEngine;

/// <summary>
/// Singleton persistente que coordina la carga de una partida guardada desde SQLite.
/// Restaura el estado del mundo en el orden correcto: primero el tiempo de juego,
/// luego el inventario del jugador y finalmente el mercado activo en escena.
/// </summary>
public class LoadManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor de carga activo.</summary>
    public static LoadManager Instance { get; private set; }

    // ─── DAOs ─────────────────────────────────────────────────────────────────

    private EstadoJuegoDAO         _estadoJuegoDAO;
    private BienDAO                _bienDAO;
    private EstadoMercadoCiudadDAO _mercadoDAO;
    private AlmacenJugadorDAO      _almacenJugadorDAO;
    private AlmacenCiudadDAO       _almacenCiudadDAO;

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
    /// Carga la partida guardada en el slot indicado y restaura el estado del mundo.
    /// El proceso sigue este orden para respetar las dependencias entre sistemas:
    /// 1) abre el fichero de base de datos, 2) restaura la fecha y velocidad de juego,
    /// 3) limpia el inventario del jugador, 4) restaura el mercado activo en escena.
    /// Si la base de datos del slot está vacía (partida nueva sin guardar) cada paso
    /// se omite con un aviso en consola sin provocar errores.
    /// </summary>
    /// <param name="slotIndex">Número de slot de guardado a leer (1 a 5).</param>
    public void CargarPartida(int slotIndex)
    {
        Debug.Log($"[LoadManager] Iniciando carga desde slot {slotIndex}...");

        DatabaseManager db = DatabaseManager.Instance;
        if (db == null)
        {
            Debug.LogError("[LoadManager] DatabaseManager no encontrado. No se puede cargar.");
            return;
        }

        db.InicializarSlot(slotIndex);
        InicializarDAOs(db);

        // Paso 1 — Tiempo de juego
        EstadoJuegoData estadoJuego = _estadoJuegoDAO.Cargar();
        if (estadoJuego != null)
            RestaurarSimulacionTiempo(estadoJuego);
        else
            Debug.LogWarning("[LoadManager] No se encontró estado de juego guardado; se mantiene la fecha por defecto.");

        // Paso 2 — Dinero del jugador
        if (estadoJuego != null)
            RestaurarDineroJugador(estadoJuego.DineroJugador);

        // Paso 3 — Limpiar almacén antes de repoblarlo
        LimpiarAlmacenJugador();

        // Paso 4 — Restaurar almacén del jugador desde BD
        RestaurarAlmacenJugador();

        // Paso 5 — Mercado activo en escena
        RestaurarMercados();

        // Paso 6 — Almacén de ciudad del jugador
        GameManager.Instance?.LimpiarAlmacenCiudades();
        GameManager.Instance?.CargarAlmacenCiudadesDesdeDAO();
        Debug.Log("[LoadManager] Almacén de ciudad restaurado.");

        // Paso 7 — Flotas PNJ
        CargarFlotasPNJ();

        Debug.Log($"[LoadManager] Carga desde slot {slotIndex} completada.");
    }

    // ─── Privados ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia los DAOs necesarios para la carga usando la conexión ya abierta
    /// por <see cref="DatabaseManager"/> para este slot.
    /// </summary>
    /// <param name="db">Gestor de base de datos activo.</param>
    private void InicializarDAOs(DatabaseManager db)
    {
        _estadoJuegoDAO    = new EstadoJuegoDAO(db);
        _bienDAO           = new BienDAO(db);
        _mercadoDAO        = new EstadoMercadoCiudadDAO(db);
        _almacenJugadorDAO = new AlmacenJugadorDAO(db);
        _almacenCiudadDAO  = new AlmacenCiudadDAO(db);
        GameManager.Instance?.InyectarAlmacenCiudadDAO(_almacenCiudadDAO);
    }

    /// <summary>
    /// Paso 7: carga todas las flotas PNJ guardadas en FlotaPNJ/CargaFlotaPNJ y las registra
    /// en FlotaManager. Las flotas ya registradas se sobreescriben (RegistrarFlota es idempotente).
    /// RutaActualTilemap e IndiceWaypointActual no se restauran; se recalculan al entrar al mapamundi.
    /// </summary>
    private void CargarFlotasPNJ()
    {
        if (FlotaManager.Instance == null)
        {
            Debug.LogWarning("[LoadManager] FlotaManager.Instance es null; se omite la carga de flotas PNJ.");
            return;
        }

        Mono.Data.Sqlite.SqliteConnection conexion = DatabaseManager.Instance.Conexion;
        int cargadas = 0;

        const string sqlFlotas = @"
            SELECT id, nombre_propietario, ciudad_origen_id, ciudad_destino_id, estado,
                   posicion_actual_x, posicion_actual_y,
                   casilla_destino_x, casilla_destino_y, casilla_destino_z
            FROM FlotaPNJ;";

        var idsLeidos = new System.Collections.Generic.List<int>();
        var flotasLeidas = new System.Collections.Generic.List<FlotaRuntimeData>();

        try
        {
            using (Mono.Data.Sqlite.SqliteCommand cmd = conexion.CreateCommand())
            {
                cmd.CommandText = sqlFlotas;
                using (Mono.Data.Sqlite.SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int    id             = reader.GetInt32(0);
                        string nombre         = reader.GetString(1);
                        int    origenId       = reader.GetInt32(2);
                        int    destinoId      = reader.GetInt32(3);
                        string estadoTexto    = reader.GetString(4);
                        float  posX           = reader.GetFloat(5);
                        float  posY           = reader.GetFloat(6);
                        int    cDestX         = reader.GetInt32(7);
                        int    cDestY         = reader.GetInt32(8);
                        int    cDestZ         = reader.GetInt32(9);

                        var flota = new FlotaRuntimeData(id, nombre);
                        flota.CiudadOrigenId  = origenId;
                        flota.CiudadDestinoId = destinoId;

                        try
                        {
                            flota.EstadoActual = (EstadoFlotaPNJ)System.Enum.Parse(
                                typeof(EstadoFlotaPNJ), estadoTexto);
                        }
                        catch
                        {
                            flota.EstadoActual = EstadoFlotaPNJ.EnPuerto;
                        }

                        flota.PosicionActual       = new UnityEngine.Vector2(posX, posY);
                        flota.CasillaDestino       = new UnityEngine.Vector3Int(cDestX, cDestY, cDestZ);
                        flota.RutaActualTilemap    = new System.Collections.Generic.List<UnityEngine.Vector3Int>();
                        flota.IndiceWaypointActual = 0;

                        idsLeidos.Add(id);
                        flotasLeidas.Add(flota);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadManager] Error al leer FlotaPNJ: {ex}");
            return;
        }

        // Cargar carga de cada flota
        const string sqlCarga = "SELECT id_bien, cantidad FROM CargaFlotaPNJ WHERE id_flota = @id;";
        for (int i = 0; i < flotasLeidas.Count; i++)
        {
            FlotaRuntimeData flota = flotasLeidas[i];
            try
            {
                using (Mono.Data.Sqlite.SqliteCommand cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = sqlCarga;
                    cmd.Parameters.AddWithValue("@id", flota.Id);
                    using (Mono.Data.Sqlite.SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int idBien   = reader.GetInt32(0);
                            int cantidad = reader.GetInt32(1);
                            flota.Carga[idBien] = cantidad;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LoadManager] Error al leer CargaFlotaPNJ para flota id={flota.Id}: {ex}");
            }

            FlotaManager.Instance.RegistrarFlota(flota);
            cargadas++;
        }

        Debug.Log($"[LoadManager] {cargadas} flotas PNJ cargadas desde FlotaPNJ.");
    }

    /// <summary>
    /// Restaura la fecha y la velocidad de simulación en <see cref="SimulacionTiempo"/>
    /// con los valores leídos desde la tabla estadoJuego. La velocidad se guardó en
    /// centésimas (p. ej. 100 = 1x, 25 = 0.25x), por lo que se convierte a float
    /// antes de pasarla al simulador.
    /// </summary>
    /// <param name="datos">Estado leído desde la base de datos.</param>
    private void RestaurarSimulacionTiempo(EstadoJuegoData datos)
    {
        SimulacionTiempo sim = SimulacionTiempo.Instance;
        if (sim == null)
        {
            Debug.LogWarning("[LoadManager] SimulacionTiempo no encontrado; no se restaura la fecha de juego.");
            return;
        }

        float velocidad = datos.VelocidadTiempo / 100f;
        sim.SetEstado(datos.DiaJuego, datos.MesJuego, datos.AñoJuego, velocidad);
        Debug.Log($"[LoadManager] SimulacionTiempo restaurado: {datos.DiaJuego}/{datos.MesJuego}/{datos.AñoJuego} vel={velocidad}x");
    }

    /// <summary>
    /// Vacía por completo el inventario del jugador antes de aplicar el estado guardado.
    /// Es necesario porque <see cref="GameManager.ModificarCantidadBien"/> solo suma o resta;
    /// no hay un método de reinicio directo, así que se resta la cantidad actual de cada bien.
    /// </summary>
    private void LimpiarAlmacenJugador()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[LoadManager] GameManager no encontrado; no se limpia el almacén.");
            return;
        }

        // Copiar el diccionario antes de iterar para evitar modificar la colección en curso
        var almacenActual = new Dictionary<BienData, int>(gm.GetAlmacen());
        foreach (KeyValuePair<BienData, int> par in almacenActual)
        {
            if (par.Value > 0)
                gm.ModificarCantidadBien(par.Key, -par.Value);
        }

        Debug.Log("[LoadManager] Almacén del jugador vaciado.");
    }

    /// <summary>
    /// Asigna el dinero guardado al jugador usando <see cref="GameManager.SetDinero"/>.
    /// </summary>
    /// <param name="dinero">Monedas de oro leídas desde la tabla estadoJuego.</param>
    private void RestaurarDineroJugador(long dinero)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LoadManager] GameManager.Instance es null; no se restaura el dinero.");
            return;
        }

        GameManager.Instance.SetDinero(dinero);
        Debug.Log($"[LoadManager] Dinero del jugador restaurado: {dinero:N0}");
    }

    /// <summary>
    /// Repuebla el almacén del jugador con los datos guardados en AlmacenJugador.
    /// Para cada entrada resuelve el <see cref="BienData"/> a través de
    /// <see cref="GameManager.GetBienPorNombre"/> y aplica la cantidad con
    /// <see cref="GameManager.ModificarCantidadBien"/>. Las entradas sin BienData
    /// reconocible se omiten con un aviso.
    /// </summary>
    private void RestaurarAlmacenJugador()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LoadManager] GameManager.Instance es null; se omite la restauración del almacén.");
            return;
        }

        Dictionary<int, int> almacenBD = _almacenJugadorDAO.ObtenerAlmacen();
        if (almacenBD.Count == 0)
        {
            Debug.Log("[LoadManager] AlmacenJugador vacío en BD; el almacén queda sin repoblar.");
            return;
        }

        int restauradas = 0;

        foreach (KeyValuePair<int, int> par in almacenBD)
        {
            BienDto dto = _bienDAO.ObtenerBienPorId(par.Key);
            if (dto == null)
            {
                Debug.LogWarning($"[LoadManager] BienDTO null para id_bien={par.Key}; se omite.");
                continue;
            }

            BienData bienData = GameManager.Instance.GetBienPorNombre(dto.Nombre);
            if (bienData == null)
            {
                Debug.LogWarning($"[LoadManager] BienData null para '{dto.Nombre}'; se omite.");
                continue;
            }

            GameManager.Instance.ModificarCantidadBien(bienData, par.Value);
            restauradas++;
        }

        Debug.Log($"[LoadManager] Restauradas {restauradas} entradas en el almacén del jugador.");
    }

    /// <summary>
    /// Restaura el estado de mercado de todas las ciudades guardadas en la BD dentro de
    /// <see cref="GameManager.MercadosPorCiudad"/>. El proceso es:
    /// 1) limpia el diccionario de GameManager, 2) obtiene los IDs de ciudad con datos
    /// guardados, 3) por cada ciudad convierte los <see cref="EstadoMercadoDto"/> en
    /// <see cref="EntradaMercado"/> resolviendo el <see cref="BienData"/> por nombre
    /// entre los assets en memoria, 4) registra la lista en GameManager.
    /// Cuando MarketManager entre en escena leerá el estado ya restaurado sin sobreescribirlo.
    /// </summary>
    private void RestaurarMercados()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LoadManager] GameManager.Instance es null; se omite la restauración de mercados.");
            return;
        }

        // Construir mapa id_bien → nombre usando la tabla Bien de la BD
        List<BienDto> bienDtos = _bienDAO.ObtenerTodosLosBienes();
        var nombrePorIdBien = new Dictionary<int, string>(bienDtos.Count);
        foreach (BienDto dto in bienDtos)
            nombrePorIdBien[dto.IdBien] = dto.Nombre;

        // Vaciar el diccionario central antes de repoblarlo
        GameManager.Instance.LimpiarMercados();

        List<int> idsCiudades = _mercadoDAO.ObtenerIdsCiudadesConMercado();
        if (idsCiudades.Count == 0)
        {
            Debug.LogWarning("[LoadManager] No hay mercados guardados en la BD; se omite la restauración.");
            return;
        }

        int ciudadesRestauradas = 0;

        foreach (int idCiudad in idsCiudades)
        {
            List<EstadoMercadoDto> dtos = _mercadoDAO.CargarEstadoMercado(idCiudad);
            if (dtos == null || dtos.Count == 0) continue;

            var entradas = new List<EntradaMercado>(dtos.Count);

            foreach (EstadoMercadoDto dto in dtos)
            {
                if (!nombrePorIdBien.TryGetValue(dto.IdBien, out string nombreBien))
                {
                    Debug.LogWarning($"[LoadManager] No se encontró nombre para id_bien={dto.IdBien} en ciudad {idCiudad}; se omite.");
                    continue;
                }

                BienData bienData = GameManager.Instance.GetBienPorNombre(nombreBien);
                if (bienData == null)
                {
                    Debug.LogError($"[LoadManager] GameManager.GetBienPorNombre devolvió null para '{nombreBien}' en ciudad {idCiudad}; se omite la entrada.");
                    continue;
                }

                entradas.Add(new EntradaMercado
                {
                    Bien             = bienData,
                    StockActual      = dto.Stock,
                    StockMax         = bienData.stockMaximo,
                    ProduccionDiaria = dto.Produccion,
                    ConsumoDiario    = dto.Consumo,
                    PrecioActual     = (float)dto.PrecioActual
                });
            }

            GameManager.Instance.RegistrarMercadoCiudad(idCiudad, entradas);
            ciudadesRestauradas++;
        }

        Debug.Log($"[LoadManager] Restaurados {ciudadesRestauradas} mercados en GameManager.MercadosPorCiudad.");
    }
}
