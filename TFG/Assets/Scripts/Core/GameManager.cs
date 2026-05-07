using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro central de la partida. Conserva el estado del comerciante
/// —tesoro, puerto actual, capacidad de bodega e inventario de mercancías—
/// mientras el jugador navega entre las distintas pantallas del juego.
/// En la beta los datos viven en memoria durante la sesión; en la release
/// se persistirán en la base de datos SQLite de la partida guardada.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    /// <summary>
    /// Punto de acceso global al estado de la partida activa.
    /// Permite a cualquier pantalla del juego consultar el tesoro
    /// o el puerto en el que se encuentra el jugador.
    /// </summary>
    public static GameManager Instance { get; private set; }

    // ─── Estado del jugador ──────────────────────────────────────────────────

    /// <summary>
    /// Monedas de oro en el cofre del comerciante.
    /// Sube al vender mercancía y baja al comprar en cualquier mercado de la Liga.
    /// </summary>
    public long Dinero { get; private set; }

    /// <summary>
    /// Puerto en el que está atracado el jugador en este momento.
    /// <c>null</c> mientras el jugador navega por el mapamundi entre ciudades.
    /// </summary>
    public CiudadData CiudadActual { get; private set; }

    /// <summary>
    /// Puerto en el que estuvo atracado el jugador antes del destino actual.
    /// Útil para ofrecer la opción de volver al puerto de origen tras un viaje.
    /// <c>null</c> si el jugador no ha visitado ninguna ciudad todavía.
    /// </summary>
    public CiudadData UltimaCiudad { get; private set; }

    // ─── Catálogo de ciudades ────────────────────────────────────────────────

    /// <summary>
    /// Lista de todas las ciudades del mundo de juego disponibles en esta versión.
    /// Asignar desde el Inspector con todos los <see cref="CiudadData"/> de la beta
    /// (Lübeck y Barcelona en la beta; las seis ciudades completas en la release).
    /// GameManager la usa en <see cref="InicializarMercadosDesdeAssets"/> para
    /// pre-registrar el mercado de cada ciudad al inicio de la partida.
    /// </summary>
    [Header("Catálogo de ciudades")]
    [SerializeField] private CiudadData[] _ciudadesDisponibles;

    /// <summary>
    /// Lista canónica de todos los <see cref="BienData"/> del juego.
    /// Asignar manualmente en el Inspector.
    /// Usado por <see cref="LoadManager"/> y futuros sistemas de PNJs y producción.
    /// </summary>
    [Header("Catálogo de bienes")]
    [Tooltip("Lista canónica de todos los BienData del juego. Asignar manualmente en el Inspector. Usado por LoadManager y futuros sistemas de PNJs y producción.")]
    [SerializeField] private List<BienData> _catalogoBienes;

    /// <summary>
    /// Vista de solo lectura del catálogo de ciudades del juego.
    /// Permite a otros sistemas (p. ej. <c>SeleccionCiudadUI</c>) iterar todas las
    /// ciudades sin poder modificar el array subyacente.
    /// </summary>
    public IReadOnlyList<CiudadData> CiudadesDisponibles => _ciudadesDisponibles;

    /// <summary>
    /// Vista de solo lectura del catálogo canónico de bienes del juego.
    /// Permite a <see cref="LoadManager"/> y otros sistemas resolver un <see cref="BienData"/>
    /// por nombre sin recurrir a <c>Resources.FindObjectsOfTypeAll</c>.
    /// </summary>
    public IReadOnlyList<BienData> CatalogoBienes => _catalogoBienes ?? new List<BienData>();

    /// <summary>
    /// Inventario de mercancías en la bodega del jugador.
    /// La clave es el <see cref="BienData"/> del bien; el valor, las unidades almacenadas.
    /// En la beta la capacidad es ilimitada (<see cref="CapacidadAlmacen"/> = <c>int.MaxValue</c>).
    /// </summary>
    private readonly Dictionary<BienData, int> _almacen = new Dictionary<BienData, int>();

    // ─── Estado de partida ───────────────────────────────────────────────────

    /// <summary>
    /// Contenedor con el estado completo de la partida en curso.
    /// Es la fuente de verdad de todos los datos dinámicos del mundo.
    /// </summary>
    private EstadoPartida _estadoPartida = new();

    /// <summary>Expone el estado de partida para sistemas externos como FlotaManager.</summary>
    public EstadoPartida EstadoPartida => _estadoPartida;

    /// <summary>
    /// Se dispara cuando el mercado de una ciudad cambia (compra, venta o tick diario).
    /// El primer parámetro es el <c>IdCiudad</c> de la ciudad afectada.
    /// El segundo parámetro es el bien concreto que cambió, o <c>null</c> si cambiaron todos
    /// los bienes a la vez (tick diario). MarketManager se suscribe para propagar el evento
    /// a la interfaz del mercado con la misma granularidad.
    /// </summary>
    public event Action<int, BienData> OnMercadoCiudadActualizado;

    // ─── Constantes de beta ──────────────────────────────────────────────────

    /// <summary>Caudal inicial de la beta: suficiente para explorar la reacción de precios sin restricciones económicas.</summary>
    private const long DineroBeta = 999_999_999L;

    /// <summary>
    /// Capacidad de bodega durante la beta: sin límite, para centrar las pruebas
    /// en la mecánica de precios sin preocuparse por el espacio de carga.
    /// En la release se sustituirá por la capacidad real del barco.
    /// </summary>
    public const int CapacidadAlmacen = int.MaxValue;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Patrón singleton: destruir duplicados y persistir entre escenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_catalogoBienes == null || _catalogoBienes.Count == 0)
            Debug.LogWarning("[GameManager] _catalogoBienes está vacío. Asignarlo en el Inspector antes de cargar una partida.");

        InicializarEstado();
        Debug.Log($"[GameManager] Inicializado como singleton persistente. Dinero: {Dinero:N0}");
    }

    /// <summary>
    /// Inicializa el estado de partida a los valores por defecto de la beta.
    /// </summary>
    private void InicializarEstado()
    {
        Dinero = DineroBeta;
        Debug.Log($"[GameManager] Partida iniciada. Dinero: {Dinero:N0}");
    }

    // ─── Catálogo de bienes ──────────────────────────────────────────────────

    /// <summary>
    /// Busca un <see cref="BienData"/> en el catálogo canónico por su nombre.
    /// La comparación es insensible a mayúsculas y minúsculas.
    /// </summary>
    /// <param name="nombre">Nombre del bien tal como aparece en el campo <c>nombre</c> del ScriptableObject.</param>
    /// <returns>
    /// El <see cref="BienData"/> cuyo nombre coincide, o <c>null</c> si el catálogo está vacío
    /// o no se encuentra ninguna coincidencia. No lanza excepciones.
    /// </returns>
    /// <remarks>
    /// Devuelve <c>null</c> en lugar de lanzar una excepción para que los sistemas de carga
    /// puedan omitir entradas no reconocidas sin interrumpir la restauración completa de la partida.
    /// </remarks>
    public BienData GetBienPorNombre(string nombre)
    {
        if (_catalogoBienes == null || _catalogoBienes.Count == 0)
        {
            Debug.LogWarning("[GameManager] GetBienPorNombre: el catálogo de bienes está vacío.");
            return null;
        }

        foreach (BienData bien in _catalogoBienes)
        {
            if (bien != null && string.Equals(bien.nombre, nombre, StringComparison.OrdinalIgnoreCase))
                return bien;
        }

        Debug.LogWarning($"[GameManager] GetBienPorNombre: no se encontró BienData con nombre '{nombre}'.");
        return null;
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Establece directamente el tesoro del jugador al valor indicado.
    /// Solo debe invocarlo <see cref="LoadManager"/> al restaurar una partida guardada;
    /// el resto del código debe usar <see cref="ModificarDinero"/> para mantener el
    /// historial de operaciones económicas.
    /// </summary>
    /// <param name="valor">Monedas de oro a asignar (debe ser ≥ 0).</param>
    public void SetDinero(long valor)
    {
        Dinero = valor >= 0 ? valor : 0;
        Debug.Log($"[GameManager] Dinero establecido directamente a {Dinero:N0}");
    }

    /// <summary>
    /// Registra un movimiento de dinero en el cofre del comerciante.
    /// Usar valor positivo al cobrar por una venta y negativo al pagar una compra.
    /// Si el tesoro no cubre el gasto, la operación no se realiza.
    /// </summary>
    /// <param name="cantidad">Monedas a ingresar (positivo) o gastar (negativo).</param>
    /// <returns><c>true</c> si el pago o cobro se realizó; <c>false</c> si el tesoro es insuficiente.</returns>
    public bool ModificarDinero(long cantidad)
    {
        if (Dinero + cantidad < 0)
        {
            Debug.LogWarning("[GameManager] Saldo insuficiente para realizar la operación.");
            return false;
        }

        Dinero += cantidad;
        Debug.Log($"[GameManager] Dinero actualizado: {Dinero:N0} ({(cantidad >= 0 ? "+" : string.Empty)}{cantidad:N0})");
        return true;
    }

    /// <summary>
    /// Registra el puerto en el que ha atracado el jugador.
    /// Invocado desde el mapamundi al hacer clic en un marcador de ciudad.
    /// </summary>
    /// <param name="ciudad"><see cref="CiudadData"/> del puerto al que llega el jugador.</param>
    public void EstablecerCiudadActual(CiudadData ciudad)
    {
        UltimaCiudad = CiudadActual;
        CiudadActual = ciudad;
        Debug.Log($"[GameManager] Ciudad actual: {ciudad.NombreCiudad}");
    }

    // ─── Almacén del jugador ─────────────────────────────────────────────────

    /// <summary>
    /// Devuelve las unidades del bien indicado que hay en la bodega del jugador.
    /// Si el bien no está en el inventario, retorna 0.
    /// </summary>
    /// <param name="bien">Bien cuya cantidad se quiere consultar.</param>
    /// <returns>Unidades disponibles en bodega (0 o más).</returns>
    public int GetCantidadBien(BienData bien)
    {
        return _almacen.TryGetValue(bien, out int cantidad) ? cantidad : 0;
    }

    /// <summary>
    /// Modifica la cantidad de un bien en la bodega del jugador.
    /// Usar valor positivo al cargar mercancía (compra) y negativo al descargarla (venta).
    /// La operación se rechaza si el resultado sería negativo o superaría <see cref="CapacidadAlmacen"/>.
    /// </summary>
    /// <param name="bien">Bien cuya cantidad se modifica.</param>
    /// <param name="cantidad">Unidades a añadir (positivo) o retirar (negativo).</param>
    /// <returns><c>true</c> si la operación se realizó; <c>false</c> si no hay suficiente stock o capacidad.</returns>
    public bool ModificarCantidadBien(BienData bien, int cantidad)
    {
        int actual = GetCantidadBien(bien);
        int nuevo = actual + cantidad;

        if (nuevo < 0)
        {
            Debug.LogWarning($"[GameManager] Stock insuficiente de '{bien.nombre}' para retirar {-cantidad} unidades (disponible: {actual}).");
            return false;
        }

        // Comprobación de capacidad total de bodega (para la release; en beta es int.MaxValue)
        int totalActual = GetTotalUnidadesAlmacen();
        if (cantidad > 0 && totalActual + cantidad > CapacidadAlmacen)
        {
            Debug.LogWarning($"[GameManager] Bodega llena. No se pueden cargar {cantidad} unidades de '{bien.nombre}'.");
            return false;
        }

        if (nuevo == 0)
            _almacen.Remove(bien);
        else
            _almacen[bien] = nuevo;

        Debug.Log($"[GameManager] Almacén '{bien.nombre}': {actual} → {nuevo}");
        return true;
    }

    /// <summary>
    /// Devuelve el total de unidades de todas las mercancías almacenadas en bodega.
    /// Se usa para comprobar si hay espacio disponible antes de cargar más mercancía.
    /// </summary>
    /// <returns>Suma de todas las unidades en bodega.</returns>
    public int GetTotalUnidadesAlmacen()
    {
        int total = 0;
        foreach (int cantidad in _almacen.Values)
            total += cantidad;
        return total;
    }

    /// <summary>
    /// Expone el inventario completo de bodega en modo de solo lectura.
    /// Útil para que la interfaz del almacén enumere todos los bienes cargados.
    /// </summary>
    /// <returns>Diccionario de solo lectura con cada bien y sus unidades.</returns>
    public IReadOnlyDictionary<BienData, int> GetAlmacen()
    {
        return _almacen;
    }

    // ─── API de mercados ─────────────────────────────────────────────────────

    /// <summary>
    /// Vista de solo lectura de todos los mercados activos indexados por <c>IdCiudad</c>.
    /// Permite a herramientas externas (SaveManager, depuración) enumerar el estado
    /// completo sin poder modificarlo directamente.
    /// </summary>
    public IReadOnlyDictionary<int, List<EntradaMercado>> MercadosPorCiudad
        => _estadoPartida.MercadosPorCiudad;

    /// <summary>
    /// Indica si el mercado de la ciudad indicada ya está registrado en el estado de partida.
    /// Útil para que MarketManager decida si debe inicializar el mercado desde los assets
    /// o simplemente reutilizar el estado ya existente.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad (campo <c>IdCiudad</c> de <see cref="CiudadData"/>).</param>
    /// <returns><c>true</c> si el mercado de esa ciudad ya está registrado; <c>false</c> en caso contrario.</returns>
    public bool TieneMercado(int idCiudad)
    {
        return _estadoPartida.MercadosPorCiudad.ContainsKey(idCiudad);
    }

    /// <summary>
    /// Devuelve la lista de entradas del mercado de la ciudad indicada en modo de solo lectura.
    /// Retorna <c>null</c> si la ciudad no tiene mercado registrado todavía.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <returns>Lista de entradas del mercado, o <c>null</c> si no existe.</returns>
    public IReadOnlyList<EntradaMercado> GetEntradasMercado(int idCiudad)
    {
        return _estadoPartida.MercadosPorCiudad.TryGetValue(idCiudad, out List<EntradaMercado> entradas)
            ? entradas
            : null;
    }

    /// <summary>
    /// Registra o sobreescribe el mercado de una ciudad en el estado de partida.
    /// Si ya existía un mercado para esa ciudad, lo reemplaza completamente.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad. Debe ser mayor que 0.</param>
    /// <param name="entradas">Lista de entradas del mercado. No puede ser <c>null</c>.</param>
    public void RegistrarMercadoCiudad(int idCiudad, List<EntradaMercado> entradas)
    {
        if (idCiudad <= 0)
        {
            Debug.LogError($"[GameManager] RegistrarMercadoCiudad: idCiudad inválido ({idCiudad}).");
            return;
        }

        if (entradas == null)
        {
            Debug.LogError($"[GameManager] RegistrarMercadoCiudad: entradas null para ciudad {idCiudad}.");
            return;
        }

        _estadoPartida.MercadosPorCiudad[idCiudad] = entradas;
        Debug.Log($"[GameManager] Mercado registrado para ciudad {idCiudad} con {entradas.Count} bienes.");
    }

    /// <summary>
    /// Vacía por completo el diccionario de mercados del estado de partida.
    /// Lo invoca LoadManager antes de repoblar los mercados desde la base de datos.
    /// </summary>
    public void LimpiarMercados()
    {
        _estadoPartida.MercadosPorCiudad.Clear();
        Debug.Log("[GameManager] Mercados limpiados.");
    }

    /// <summary>
    /// Inicializa los mercados de todas las ciudades indicadas copiando en profundidad
    /// las entradas de sus <c>ScriptableObject</c> y calculando el precio inicial.
    /// Solo registra ciudades cuyo mercado no esté ya presente (no sobreescribe).
    /// Llamar desde el inicio de partida nueva antes de cargar cualquier escena de ciudad.
    /// </summary>
    /// <param name="ciudades">Colección de <see cref="CiudadData"/> que se deben inicializar.</param>
    public void InicializarMercadosDesdeAssets(IEnumerable<CiudadData> ciudades)
    {
        if (ciudades == null)
        {
            Debug.LogError("[GameManager] InicializarMercadosDesdeAssets: colección de ciudades null.");
            return;
        }

        foreach (CiudadData ciudad in ciudades)
        {
            if (ciudad == null) continue;
            if (TieneMercado(ciudad.IdCiudad)) continue;

            List<EntradaMercado> copia = new List<EntradaMercado>(ciudad.Mercado.Count);
            foreach (EntradaMercado origen in ciudad.Mercado)
            {
                float precio = origen.Bien != null
                    ? origen.Bien.precioBase * ((float)origen.Bien.stockMaximo / Mathf.Max(origen.StockActual, 1))
                    : 0f;

                copia.Add(new EntradaMercado
                {
                    Bien             = origen.Bien,
                    StockActual      = origen.StockActual,
                    StockMax         = origen.StockMax,
                    ProduccionDiaria = origen.ProduccionDiaria,
                    ConsumoDiario    = origen.ConsumoDiario,
                    PrecioActual     = precio
                });
            }

            RegistrarMercadoCiudad(ciudad.IdCiudad, copia);
        }

        // Spawn de flotas PNJ de prueba al iniciar partida nueva
        if (FlotaManager.Instance != null)
            FlotaManager.Instance.SpawnFlotasPNJIniciales(_ciudadesDisponibles);
        else
            Debug.LogWarning("[GameManager] InicializarMercadosDesdeAssets: FlotaManager.Instance es null, flotas PNJ no creadas.");
    }

    /// <summary>
    /// Notifica a todos los suscriptores que el mercado de la ciudad indicada ha cambiado.
    /// Lo invoca MarketManager tras cada compra, venta o tick diario.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad cuyo mercado ha cambiado.</param>
    /// <param name="bien">Bien concreto que cambió, o <c>null</c> si cambiaron todos (tick diario).</param>
    public void NotificarMercadoActualizado(int idCiudad, BienData bien = null)
    {
        OnMercadoCiudadActualizado?.Invoke(idCiudad, bien);
    }
}
