using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Representa el estado del mercado de una ciudad concreta de la Liga Hanseática.
/// Gestiona el stock disponible de cada bien, calcula precios dinámicos según la
/// fórmula de oferta y demanda, y ejecuta las operaciones de compra y venta del jugador.
/// </summary>
/// <remarks>
/// Fórmula de precio:
/// <c>precio_actual = precioBase × (stockMaximo / Mathf.Max(stockActual, 1))</c>
/// — a menor stock, mayor precio; a mayor stock, menor precio.
/// </remarks>
public class MarketManager : MonoBehaviour
{
    // ─── Configuración ───────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject con el nombre de la ciudad y la lista de bienes del mercado.
    /// Si está asignado, <c>Start</c> inicializa el mercado desde aquí en lugar del array
    /// configurado manualmente en el Inspector.
    /// </summary>
    [Header("Datos de ciudad")]
    public CiudadData DatosCiudad;

    /// <summary>
    /// Lista de entradas del mercado: un registro por cada bien que se comercia en esta ciudad.
    /// Se puede configurar manualmente desde el Inspector o se sobreescribe desde
    /// <see cref="DatosCiudad"/> al inicio.
    /// </summary>
    [Header("Bienes del mercado")]
    [SerializeField] private List<EntradaMercado> _entradas = new List<EntradaMercado>();

    // ─── Límites de precio ───────────────────────────────────────────────────

    // Precio mínimo: 50% del precio base (mercado saturado)
    private const float MultiplicadorPrecioMinimo = 0.5f;
    // Precio máximo: 500% del precio base (escasez extrema)
    private const float MultiplicadorPrecioMaximo = 5.0f;

    // ─── Índice interno ──────────────────────────────────────────────────────

    /// <summary>
    /// Mapa de acceso rápido para consultar el estado de cada bien sin recorrer la lista completa.
    /// Se construye al inicio a partir de <see cref="_entradas"/>.
    /// </summary>
    private Dictionary<BienData, EntradaMercado> _indice;

    // ─── Eventos ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Se lanza cada vez que el stock o el precio de cualquier bien cambia.
    /// La interfaz del mercado se suscribe a este evento para refrescar las filas afectadas.
    /// El parámetro es el bien cuyo estado ha cambiado.
    /// </summary>
    public event Action<BienData> OnMercadoActualizado;

    // ─────────────────────────────────────────────────────────────────────────

    // Fallback: si nadie llama a InicializarConCiudad antes, se inicializa con el asset del Inspector
    private void Start()
    {
        InicializarConCiudad(DatosCiudad);
    }

    /// <summary>
    /// Inicializa el mercado con los datos de la ciudad indicada.
    /// Debe llamarse desde <see cref="CiudadController"/> después de que este haya
    /// resuelto qué ciudad corresponde a la sesión actual, para evitar que el orden
    /// de ejecución de <c>Start()</c> entre MonoBehaviours cargue la ciudad incorrecta.
    /// Si se llama con <c>null</c>, opera con las entradas ya configuradas en el Inspector.
    /// </summary>
    /// <param name="datosCiudad">ScriptableObject de la ciudad cuyo mercado se debe cargar.</param>
    public void InicializarConCiudad(CiudadData datosCiudad)
    {
        // Sobreescribir referencia y copiar entradas si se proporcionan datos nuevos
        if (datosCiudad != null)
        {
            DatosCiudad = datosCiudad;
            _entradas = new List<EntradaMercado>(datosCiudad.Mercado.Count);
            foreach (EntradaMercado origen in datosCiudad.Mercado)
            {
                _entradas.Add(new EntradaMercado
                {
                    Bien             = origen.Bien,
                    StockActual      = origen.StockActual,
                    StockMax         = origen.StockMax,
                    ProduccionDiaria = origen.ProduccionDiaria,
                    ConsumoDiario    = origen.ConsumoDiario
                });
            }
        }

        // Construir índice para acceso O(1)
        string nombreCiudad = DatosCiudad != null ? DatosCiudad.NombreCiudad : "?";
        _indice = new Dictionary<BienData, EntradaMercado>(_entradas.Count);

        foreach (EntradaMercado entrada in _entradas)
        {
            if (entrada.Bien == null)
            {
                Debug.LogWarning($"[MarketManager:{nombreCiudad}] Entrada sin BienData asignado; se ignora.");
                continue;
            }

            if (_indice.ContainsKey(entrada.Bien))
            {
                Debug.LogWarning($"[MarketManager:{nombreCiudad}] Bien duplicado '{entrada.Bien.nombre}'; se usa la primera entrada.");
                continue;
            }

            // Inicializar precio actual según stock de partida
            entrada.PrecioActual = CalcularPrecio(entrada.Bien, entrada.StockActual);
            _indice[entrada.Bien] = entrada;
        }

        Debug.Log($"[MarketManager] Mercado de {nombreCiudad} listo con {_indice.Count} bienes.");
    }

    // ─── Consulta ────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la lista completa de entradas del mercado.
    /// Útil para que la interfaz construya todas las filas al abrir la pantalla de mercado.
    /// </summary>
    /// <returns>Lista de entradas del mercado (solo lectura).</returns>
    public IReadOnlyList<EntradaMercado> GetEntradas()
    {
        return _entradas;
    }

    /// <summary>
    /// Devuelve el nombre de la ciudad cuyo mercado gestiona este componente.
    /// </summary>
    /// <returns>Nombre de la ciudad, o cadena vacía si no hay datos asignados.</returns>
    public string GetNombreCiudad()
    {
        return DatosCiudad != null ? DatosCiudad.NombreCiudad : string.Empty;
    }

    /// <summary>
    /// Devuelve el stock actual de un bien en este mercado.
    /// </summary>
    /// <param name="bien">Bien que se quiere consultar.</param>
    /// <returns>Unidades disponibles en la ciudad, o 0 si el bien no existe en este mercado.</returns>
    public int GetStockActual(BienData bien)
    {
        return _indice.TryGetValue(bien, out EntradaMercado entrada) ? entrada.StockActual : 0;
    }

    /// <summary>
    /// Devuelve el precio actual de un bien aplicando la fórmula de oferta y demanda.
    /// El precio está precalculado en <see cref="EntradaMercado.PrecioActual"/> y se actualiza
    /// cada vez que el stock cambia.
    /// </summary>
    /// <param name="bien">Bien cuyo precio se consulta.</param>
    /// <returns>Precio en monedas de oro, o 0 si el bien no existe en este mercado.</returns>
    public float GetPrecioActual(BienData bien)
    {
        return _indice.TryGetValue(bien, out EntradaMercado entrada) ? entrada.PrecioActual : 0f;
    }

    // ─── Operaciones de comercio ─────────────────────────────────────────────

    /// <summary>
    /// Ejecuta la compra de un bien por parte del jugador en este mercado.
    /// Descuenta el coste del tesoro del jugador, reduce el stock de la ciudad
    /// y carga las unidades en la bodega del jugador.
    /// La operación falla si la ciudad no tiene stock suficiente o el jugador
    /// no tiene dinero ni espacio en bodega.
    /// </summary>
    /// <param name="bien">Bien que se quiere comprar.</param>
    /// <param name="cantidad">Unidades a comprar (debe ser mayor que 0).</param>
    /// <returns><c>true</c> si la compra se realizó correctamente; <c>false</c> en caso contrario.</returns>
    public bool Comprar(BienData bien, int cantidad)
    {
        string nombreCiudad = DatosCiudad != null ? DatosCiudad.NombreCiudad : "?";

        if (cantidad <= 0)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] Cantidad de compra inválida: {cantidad}.");
            return false;
        }

        if (!_indice.TryGetValue(bien, out EntradaMercado entrada))
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] El bien '{bien.nombre}' no está disponible en este mercado.");
            return false;
        }

        if (entrada.StockActual < cantidad)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] Stock insuficiente de '{bien.nombre}'. Disponible: {entrada.StockActual}, solicitado: {cantidad}.");
            return false;
        }

        long costeTotal = (long)Mathf.Ceil(entrada.PrecioActual * cantidad);
        if (!GameManager.Instance.ModificarDinero(-costeTotal))
            return false;

        if (!GameManager.Instance.ModificarCantidadBien(bien, cantidad))
        {
            GameManager.Instance.ModificarDinero(costeTotal);
            return false;
        }

        entrada.StockActual -= cantidad;
        ActualizarPrecio(bien, entrada);
        Debug.Log($"[MarketManager:{nombreCiudad}] Compra: {cantidad}× '{bien.nombre}' por {costeTotal} monedas.");
        return true;
    }

    /// <summary>
    /// Ejecuta la venta de un bien del jugador en este mercado.
    /// Ingresa el precio en el tesoro del jugador, aumenta el stock de la ciudad
    /// y retira las unidades de la bodega del jugador.
    /// La operación falla si el jugador no tiene suficiente cantidad del bien en bodega.
    /// </summary>
    /// <param name="bien">Bien que se quiere vender.</param>
    /// <param name="cantidad">Unidades a vender (debe ser mayor que 0).</param>
    /// <returns><c>true</c> si la venta se realizó correctamente; <c>false</c> en caso contrario.</returns>
    public bool Vender(BienData bien, int cantidad)
    {
        string nombreCiudad = DatosCiudad != null ? DatosCiudad.NombreCiudad : "?";

        if (cantidad <= 0)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] Cantidad de venta inválida: {cantidad}.");
            return false;
        }

        if (!_indice.TryGetValue(bien, out EntradaMercado entrada))
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] El bien '{bien.nombre}' no se puede vender en este mercado.");
            return false;
        }

        if (!GameManager.Instance.ModificarCantidadBien(bien, -cantidad))
            return false;

        long ingresoTotal = (long)Mathf.Floor(entrada.PrecioActual * cantidad);
        GameManager.Instance.ModificarDinero(ingresoTotal);

        entrada.StockActual = Mathf.Min(entrada.StockActual + cantidad, bien.stockMaximo);
        ActualizarPrecio(bien, entrada);
        Debug.Log($"[MarketManager:{nombreCiudad}] Venta: {cantidad}× '{bien.nombre}' por {ingresoTotal} monedas.");
        return true;
    }

    // ─── Precio ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula el precio de un bien en función de su stock actual usando la fórmula
    /// <c>precio = precioBase × (stockMaximo / max(stockActual, 1))</c>.
    /// </summary>
    /// <param name="bien">Datos estáticos del bien (precio base y stock máximo).</param>
    /// <param name="stockActual">Unidades disponibles en la ciudad en este momento.</param>
    /// <returns>Precio en monedas de oro.</returns>
    private float CalcularPrecio(BienData bien, int stockActual)
    {
        float precio = bien.precioBase * ((float)bien.stockMaximo / Mathf.Max(stockActual, 1));
        return Mathf.Clamp(precio, bien.precioBase * MultiplicadorPrecioMinimo, bien.precioBase * MultiplicadorPrecioMaximo);
    }

    /// <summary>
    /// Recalcula el precio de la entrada indicada y notifica a los suscriptores del evento
    /// <see cref="OnMercadoActualizado"/> para que la interfaz refresque la fila correspondiente.
    /// </summary>
    /// <param name="bien">Bien cuyo precio debe actualizarse.</param>
    /// <param name="entrada">Entrada del mercado que contiene el stock actual del bien.</param>
    private void ActualizarPrecio(BienData bien, EntradaMercado entrada)
    {
        entrada.PrecioActual = CalcularPrecio(bien, entrada.StockActual);
        OnMercadoActualizado?.Invoke(bien);
    }
}
