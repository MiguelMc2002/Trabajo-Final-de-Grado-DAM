using System;
using System.Collections.Generic;
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
    /// Nombre de la ciudad cuyo mercado gestiona este componente (p. ej. "Lübeck").
    /// Solo informativo; no se usa como clave en base de datos durante la beta.
    /// </summary>
    [Header("Ciudad")]
    [SerializeField] private string _nombreCiudad;

    /// <summary>
    /// Lista de entradas del mercado: un registro por cada bien que se comercia en esta ciudad.
    /// Se configura desde el Inspector de Unity añadiendo <see cref="BienData"/> y stock inicial.
    /// </summary>
    [Header("Bienes del mercado")]
    [SerializeField] private List<EntradaMercado> _entradas = new List<EntradaMercado>();

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

    private void Awake()
    {
        // Construir índice para acceso O(1)
        _indice = new Dictionary<BienData, EntradaMercado>(_entradas.Count);
        foreach (EntradaMercado entrada in _entradas)
        {
            if (entrada.bien == null)
            {
                Debug.LogWarning($"[MarketManager:{_nombreCiudad}] Entrada sin BienData asignado; se ignora.");
                continue;
            }

            if (_indice.ContainsKey(entrada.bien))
            {
                Debug.LogWarning($"[MarketManager:{_nombreCiudad}] Bien duplicado '{entrada.bien.nombre}'; se usa la primera entrada.");
                continue;
            }

            // Inicializar precio actual según stock de partida
            entrada.precioActual = CalcularPrecio(entrada.bien, entrada.stockActual);
            _indice[entrada.bien] = entrada;
        }

        Debug.Log($"[MarketManager] Mercado de {_nombreCiudad} listo con {_indice.Count} bienes.");
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
    /// <returns>Nombre de la ciudad.</returns>
    public string GetNombreCiudad()
    {
        return _nombreCiudad;
    }

    /// <summary>
    /// Devuelve el stock actual de un bien en este mercado.
    /// </summary>
    /// <param name="bien">Bien que se quiere consultar.</param>
    /// <returns>Unidades disponibles en la ciudad, o 0 si el bien no existe en este mercado.</returns>
    public int GetStockActual(BienData bien)
    {
        return _indice.TryGetValue(bien, out EntradaMercado entrada) ? entrada.stockActual : 0;
    }

    /// <summary>
    /// Devuelve el precio actual de un bien aplicando la fórmula de oferta y demanda.
    /// El precio está precalculado en <see cref="EntradaMercado.precioActual"/> y se actualiza
    /// cada vez que el stock cambia.
    /// </summary>
    /// <param name="bien">Bien cuyo precio se consulta.</param>
    /// <returns>Precio en monedas de oro, o 0 si el bien no existe en este mercado.</returns>
    public float GetPrecioActual(BienData bien)
    {
        return _indice.TryGetValue(bien, out EntradaMercado entrada) ? entrada.precioActual : 0f;
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
        if (cantidad <= 0)
        {
            Debug.LogWarning($"[MarketManager:{_nombreCiudad}] Cantidad de compra inválida: {cantidad}.");
            return false;
        }

        if (!_indice.TryGetValue(bien, out EntradaMercado entrada))
        {
            Debug.LogWarning($"[MarketManager:{_nombreCiudad}] El bien '{bien.nombre}' no está disponible en este mercado.");
            return false;
        }

        if (entrada.stockActual < cantidad)
        {
            Debug.LogWarning($"[MarketManager:{_nombreCiudad}] Stock insuficiente de '{bien.nombre}'. Disponible: {entrada.stockActual}, solicitado: {cantidad}.");
            return false;
        }

        long costeTotal = (long)Mathf.Ceil(entrada.precioActual * cantidad);
        if (!GameManager.Instance.ModificarDinero(-costeTotal))
        {
            // ModificarDinero ya registra el aviso de saldo insuficiente
            return false;
        }

        // El dinero se ha descontado; ahora actualizar stock y bodega
        if (!GameManager.Instance.ModificarCantidadBien(bien, cantidad))
        {
            // Bodega llena: devolver el dinero al jugador
            GameManager.Instance.ModificarDinero(costeTotal);
            return false;
        }

        entrada.stockActual -= cantidad;
        ActualizarPrecio(bien, entrada);
        Debug.Log($"[MarketManager:{_nombreCiudad}] Compra: {cantidad}× '{bien.nombre}' por {costeTotal} monedas.");
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
        if (cantidad <= 0)
        {
            Debug.LogWarning($"[MarketManager:{_nombreCiudad}] Cantidad de venta inválida: {cantidad}.");
            return false;
        }

        if (!_indice.TryGetValue(bien, out EntradaMercado entrada))
        {
            Debug.LogWarning($"[MarketManager:{_nombreCiudad}] El bien '{bien.nombre}' no se puede vender en este mercado.");
            return false;
        }

        // Retirar de la bodega primero; si falla, el jugador no tenía suficiente
        if (!GameManager.Instance.ModificarCantidadBien(bien, -cantidad))
            return false;

        long ingresoTotal = (long)Mathf.Floor(entrada.precioActual * cantidad);
        GameManager.Instance.ModificarDinero(ingresoTotal);

        entrada.stockActual = Mathf.Min(entrada.stockActual + cantidad, bien.stockMaximo);
        ActualizarPrecio(bien, entrada);
        Debug.Log($"[MarketManager:{_nombreCiudad}] Venta: {cantidad}× '{bien.nombre}' por {ingresoTotal} monedas.");
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
        return bien.precioBase * ((float)bien.stockMaximo / Mathf.Max(stockActual, 1));
    }

    /// <summary>
    /// Recalcula el precio de la entrada indicada y notifica a los suscriptores del evento
    /// <see cref="OnMercadoActualizado"/> para que la interfaz refresque la fila correspondiente.
    /// </summary>
    /// <param name="bien">Bien cuyo precio debe actualizarse.</param>
    /// <param name="entrada">Entrada del mercado que contiene el stock actual del bien.</param>
    private void ActualizarPrecio(BienData bien, EntradaMercado entrada)
    {
        entrada.precioActual = CalcularPrecio(bien, entrada.stockActual);
        OnMercadoActualizado?.Invoke(bien);
    }
}

/// <summary>
/// Agrupa el estado dinámico de un bien concreto dentro del mercado de una ciudad:
/// el bien de referencia, las unidades disponibles en la ciudad y el precio calculado.
/// Se serializa en el Inspector para poder configurar el stock inicial desde el editor.
/// </summary>
[Serializable]
public class EntradaMercado
{
    /// <summary>
    /// Referencia al <see cref="BienData"/> que define nombre, categoría y precio base.
    /// </summary>
    public BienData bien;

    /// <summary>
    /// Unidades del bien disponibles actualmente en el mercado de la ciudad.
    /// Se reduce al comprar y aumenta al vender (hasta <see cref="BienData.stockMaximo"/>).
    /// </summary>
    [Min(0)]
    public int stockActual;

    /// <summary>
    /// Precio calculado en tiempo de ejecución según la fórmula de oferta y demanda.
    /// No editable desde el Inspector (se inicializa en <c>Awake</c>).
    /// </summary>
    [HideInInspector]
    public float precioActual;
}
