using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla una fila de la pantalla de mercado.
/// Muestra el nombre del bien, el stock de la ciudad, el stock en la bodega del jugador,
/// el precio actual y los botones de compra/venta. Reacciona a los cambios del mercado
/// suscribiéndose al evento <see cref="MarketManager.OnMercadoActualizado"/>.
/// </summary>
public class MarketRowUI : MonoBehaviour
{
    // ─── Referencias UI ──────────────────────────────────────────────────────

    [Header("Etiquetas de texto")]
    /// <summary>Muestra el nombre del bien (p. ej. "Grano").</summary>
    [SerializeField] private TextMeshProUGUI _textoNombre;

    /// <summary>Muestra las unidades disponibles en el mercado de la ciudad.</summary>
    [SerializeField] private TextMeshProUGUI _textoStockCiudad;

    /// <summary>Muestra las unidades que el jugador tiene en su bodega.</summary>
    [SerializeField] private TextMeshProUGUI _textoStockAlmacen;

    /// <summary>Muestra el precio actual del bien en monedas de oro.</summary>
    [SerializeField] private TextMeshProUGUI _textoPrecio;

    [Header("Indicador de precio")]
    /// <summary>
    /// Imagen cuyo color cambia según el nivel de stock:
    /// verde (abundancia), amarillo (normal) o rojo (escasez).
    /// </summary>
    [SerializeField] private Image _indicadorColor;

    [Header("Botones — Comprar")]
    /// <summary>Botón para comprar 1 unidad del bien.</summary>
    [SerializeField] private Button _btnComprar1;
    /// <summary>Botón para comprar 10 unidades del bien.</summary>
    [SerializeField] private Button _btnComprar10;
    /// <summary>Botón para comprar 100 unidades del bien.</summary>
    [SerializeField] private Button _btnComprar100;

    [Header("Botones — Vender")]
    /// <summary>Botón para vender 1 unidad del bien.</summary>
    [SerializeField] private Button _btnVender1;
    /// <summary>Botón para vender 10 unidades del bien.</summary>
    [SerializeField] private Button _btnVender10;
    /// <summary>Botón para vender 100 unidades del bien.</summary>
    [SerializeField] private Button _btnVender100;

    // ─── Colores de indicador ────────────────────────────────────────────────

    /// <summary>Color del indicador cuando el stock supera el 66 % del máximo (precio bajo).</summary>
    private static readonly Color ColorStockAlto    = new Color(0.18f, 0.80f, 0.44f); // verde
    /// <summary>Color del indicador cuando el stock está entre el 33 % y el 66 % del máximo.</summary>
    private static readonly Color ColorStockNormal  = new Color(0.95f, 0.77f, 0.06f); // amarillo
    /// <summary>Color del indicador cuando el stock cae por debajo del 33 % del máximo (precio alto).</summary>
    private static readonly Color ColorStockBajo    = new Color(0.91f, 0.30f, 0.24f); // rojo

    // ─── Estado interno ──────────────────────────────────────────────────────

    /// <summary>Bien que representa esta fila en el mercado.</summary>
    private BienData _bien;

    /// <summary>Referencia al gestor del mercado de la ciudad actual.</summary>
    private MarketManager _marketManager;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la fila con el bien y el gestor de mercado correspondientes.
    /// Registra los listeners de los botones y se suscribe al evento de actualización
    /// del mercado para mantener los datos en pantalla sincronizados.
    /// Debe llamarse una vez justo después de instanciar el prefab.
    /// </summary>
    /// <param name="bien">Bien que representa esta fila.</param>
    /// <param name="marketManager">Gestor del mercado de la ciudad activa.</param>
    public void Inicializar(BienData bien, MarketManager marketManager)
    {
        _bien          = bien;
        _marketManager = marketManager;

        // Nombre del bien (estático, no cambia en runtime)
        _textoNombre.text = bien.nombre;

        // Listeners de compra
        _btnComprar1.onClick.AddListener(() => EjecutarCompra(1));
        _btnComprar10.onClick.AddListener(() => EjecutarCompra(10));
        _btnComprar100.onClick.AddListener(() => EjecutarCompra(100));

        // Listeners de venta
        _btnVender1.onClick.AddListener(() => EjecutarVenta(1));
        _btnVender10.onClick.AddListener(() => EjecutarVenta(10));
        _btnVender100.onClick.AddListener(() => EjecutarVenta(100));

        // Suscribirse al evento del mercado para actualizar cuando cambie el stock
        _marketManager.OnMercadoActualizado += OnMercadoActualizado;

        // Pintar datos iniciales
        Refrescar();
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar referencias a objetos destruidos
        if (_marketManager != null)
            _marketManager.OnMercadoActualizado -= OnMercadoActualizado;
    }

    // ─── Actualización de la interfaz ────────────────────────────────────────

    /// <summary>
    /// Callback del evento <see cref="MarketManager.OnMercadoActualizado"/>.
    /// Solo refresca esta fila si el bien afectado es el que representa.
    /// </summary>
    /// <param name="bienActualizado">Bien cuyo estado ha cambiado en el mercado.</param>
    private void OnMercadoActualizado(BienData bienActualizado)
    {
        if (bienActualizado == _bien)
            Refrescar();
    }

    /// <summary>
    /// Actualiza todos los elementos visuales de la fila con los datos actuales del mercado
    /// y de la bodega del jugador: stock de ciudad, stock en almacén, precio e indicador de color.
    /// </summary>
    private void Refrescar()
    {
        int stockCiudad   = _marketManager.GetStockActual(_bien);
        int stockAlmacen  = GameManager.Instance.GetCantidadBien(_bien);
        float precioActual = _marketManager.GetPrecioActual(_bien);

        _textoStockCiudad.text  = stockCiudad.ToString("N0");
        _textoStockAlmacen.text = stockAlmacen.ToString("N0");
        _textoPrecio.text       = $"{precioActual:N0} ✦";

        ActualizarIndicadorColor(stockCiudad);
    }

    /// <summary>
    /// Cambia el color del indicador según el nivel de stock de la ciudad en relación
    /// al stock máximo definido en el <see cref="BienData"/>:
    /// verde si supera el 66 %, rojo si cae por debajo del 33 %, amarillo en el tramo intermedio.
    /// </summary>
    /// <param name="stockActual">Unidades disponibles actualmente en la ciudad.</param>
    private void ActualizarIndicadorColor(int stockActual)
    {
        if (_indicadorColor == null) return;

        float porcentaje = (float)stockActual / Mathf.Max(_bien.stockMaximo, 1);

        if (porcentaje > 0.66f)
            _indicadorColor.color = ColorStockAlto;
        else if (porcentaje < 0.33f)
            _indicadorColor.color = ColorStockBajo;
        else
            _indicadorColor.color = ColorStockNormal;
    }

    // ─── Acciones de compra/venta ────────────────────────────────────────────

    /// <summary>
    /// Intenta comprar la cantidad indicada del bien en el mercado actual.
    /// Si la operación falla (stock insuficiente, dinero insuficiente o bodega llena)
    /// el mercado y la interfaz permanecen sin cambios.
    /// </summary>
    /// <param name="cantidad">Unidades que se desean comprar.</param>
    private void EjecutarCompra(int cantidad)
    {
        _marketManager.Comprar(_bien, cantidad);
        // La interfaz se refresca automáticamente vía OnMercadoActualizado
    }

    /// <summary>
    /// Intenta vender la cantidad indicada del bien en el mercado actual.
    /// Si el jugador no tiene suficiente stock en bodega, la operación no se realiza.
    /// </summary>
    /// <param name="cantidad">Unidades que se desean vender.</param>
    private void EjecutarVenta(int cantidad)
    {
        _marketManager.Vender(_bien, cantidad);
        // La interfaz se refresca automáticamente vía OnMercadoActualizado
    }
}
