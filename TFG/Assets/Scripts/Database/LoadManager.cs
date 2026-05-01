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

        // Paso 2 — Inventario del jugador
        LimpiarAlmacenJugador();

        // Paso 3 — Mercado activo en escena
        RestaurarMercados();

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
        _estadoJuegoDAO = new EstadoJuegoDAO(db);
        _bienDAO        = new BienDAO(db);
        _mercadoDAO     = new EstadoMercadoCiudadDAO(db);
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
    /// Restaura el estado del mercado de la ciudad activa en escena con los datos
    /// leídos desde <c>EstadoMercadoCiudad</c>. Si no hay ningún
    /// <see cref="MarketManager"/> en escena (p. ej. estamos en el mapamundi) el paso
    /// se omite con un aviso. Los bienes se emparejan por nombre entre el
    /// <see cref="BienData"/> en memoria y los registros de la base de datos.
    /// </summary>
    private void RestaurarMercados()
    {
        MarketManager market = FindAnyObjectByType<MarketManager>();
        if (market == null)
        {
            Debug.LogWarning("[LoadManager] MarketManager no encontrado en escena; se omite la restauración del mercado.");
            return;
        }

        CiudadData ciudad = market.DatosCiudad;
        if (ciudad == null)
        {
            Debug.LogWarning("[LoadManager] El MarketManager en escena no tiene CiudadData asignada; se omite la restauración del mercado.");
            return;
        }

        // Leer el estado guardado de esta ciudad
        List<EstadoMercadoDto> estadoGuardado = _mercadoDAO.CargarEstadoMercado(ciudad.IdCiudad);
        if (estadoGuardado == null || estadoGuardado.Count == 0)
        {
            Debug.LogWarning($"[LoadManager] No hay datos de mercado guardados para '{ciudad.NombreCiudad}'; se omite.");
            return;
        }

        // Construir mapa id_bien → dto para acceso rápido
        var estadoPorId = new Dictionary<int, EstadoMercadoDto>(estadoGuardado.Count);
        foreach (EstadoMercadoDto dto in estadoGuardado)
            estadoPorId[dto.IdBien] = dto;

        // Construir mapa nombre_bien → id_bien usando los registros de la tabla Bien
        List<BienDto> bienDtos = _bienDAO.ObtenerTodosLosBienes();
        var idPorNombreBien = new Dictionary<string, int>(bienDtos.Count);
        foreach (BienDto dto in bienDtos)
            idPorNombreBien[dto.Nombre] = dto.IdBien;

        // Actualizar cada entrada del mercado en memoria con los valores guardados
        IReadOnlyList<EntradaMercado> entradas = market.GetEntradas();
        int restaurados = 0;

        foreach (EntradaMercado entrada in entradas)
        {
            if (entrada.Bien == null) continue;

            if (!idPorNombreBien.TryGetValue(entrada.Bien.nombre, out int idBien)) continue;
            if (!estadoPorId.TryGetValue(idBien, out EstadoMercadoDto estado)) continue;

            entrada.StockActual      = estado.Stock;
            entrada.ProduccionDiaria = estado.Produccion;
            entrada.ConsumoDiario    = estado.Consumo;
            entrada.PrecioActual     = (float)estado.PrecioActual;
            restaurados++;
        }

        Debug.Log($"[LoadManager] Mercado de '{ciudad.NombreCiudad}' restaurado: {restaurados}/{entradas.Count} bienes actualizados.");
    }
}
