using System.Collections.Generic;
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

    [Header("Columnas ciclables")]
    /// <summary>Etiqueta de la columna izquierda: "Mercado ciudad", "Almacén ciudad" o "Bodega barco".</summary>
    [SerializeField] private TextMeshProUGUI _textoEtiquetaIzq;
    /// <summary>Etiqueta de la columna derecha.</summary>
    [SerializeField] private TextMeshProUGUI _textoEtiquetaDer;
    /// <summary>Flecha que cicla la columna izquierda.</summary>
    [SerializeField] private Button _btnCiclarIzq;
    /// <summary>Flecha que cicla la columna derecha.</summary>
    [SerializeField] private Button _btnCiclarDer;

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

    /// <summary>
    /// Intermediario de comercio que valida y ejecuta las operaciones de compra y venta.
    /// Todas las acciones de los botones pasan por aquí en lugar de llamar al mercado directamente.
    /// </summary>
    private OficinaComercial _oficina;

    private OficinaComercial.OrigenDestino _columnaIzq = OficinaComercial.OrigenDestino.Mercado;
    private OficinaComercial.OrigenDestino _columnaDer = OficinaComercial.OrigenDestino.BodegaBarco;
    private int _idCiudad;
    private int _idBien;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la fila con el bien, el gestor de mercado y la oficina comercial.
    /// Limpia y registra los listeners de los 6 botones, y se suscribe al evento de
    /// actualización del mercado para mantener los datos en pantalla sincronizados.
    /// Puede llamarse varias veces de forma segura: los listeners previos se eliminan
    /// antes de añadir los nuevos para evitar duplicados.
    /// </summary>
    /// <param name="bien">Bien que representa esta fila.</param>
    /// <param name="marketManager">Gestor del mercado de la ciudad activa.</param>
    /// <param name="oficina">Intermediario de comercio que valida y ejecuta las operaciones.</param>
    public void Inicializar(BienData bien, MarketManager marketManager, OficinaComercial oficina)
    {
        _bien          = bien;
        _marketManager = marketManager;
        _oficina       = oficina;

        // Nombre del bien (estático, no cambia en runtime)
        _textoNombre.text = bien.nombre;

        // Resolver _idCiudad e _idBien
        _idCiudad = GameManager.Instance?.CiudadActual?.IdCiudad ?? -1;
        _idBien   = -1;
        if (GameManager.Instance != null)
        {
            IReadOnlyList<BienData> catalogo = GameManager.Instance.CatalogoBienes;
            for (int i = 0; i < catalogo.Count; i++)
            {
                if (catalogo[i] == bien) { _idBien = i + 1; break; }
            }
        }

        // Limpiar listeners previos para evitar duplicados si Inicializar se llama más de una vez
        _btnComprar1.onClick.RemoveAllListeners();
        _btnComprar10.onClick.RemoveAllListeners();
        _btnComprar100.onClick.RemoveAllListeners();
        _btnVender1.onClick.RemoveAllListeners();
        _btnVender10.onClick.RemoveAllListeners();
        _btnVender100.onClick.RemoveAllListeners();

        if (_btnCiclarIzq != null) { _btnCiclarIzq.onClick.RemoveAllListeners(); _btnCiclarIzq.onClick.AddListener(CiclarColumnaIzq); }
        if (_btnCiclarDer != null) { _btnCiclarDer.onClick.RemoveAllListeners(); _btnCiclarDer.onClick.AddListener(CiclarColumnaDer); }

        // Listeners de compra
        _btnComprar1.onClick.AddListener(() => EjecutarCompra(1));
        Debug.Log($"[MarketRowUI] Listener registrado: BtnComprar1 para {_bien?.nombre}");
        _btnComprar10.onClick.AddListener(() => EjecutarCompra(10));
        Debug.Log($"[MarketRowUI] Listener registrado: BtnComprar10 para {_bien?.nombre}");
        _btnComprar100.onClick.AddListener(() => EjecutarCompra(100));
        Debug.Log($"[MarketRowUI] Listener registrado: BtnComprar100 para {_bien?.nombre}");

        // Listeners de venta
        _btnVender1.onClick.AddListener(() => EjecutarVenta(1));
        Debug.Log($"[MarketRowUI] Listener registrado: BtnVender1 para {_bien?.nombre}");
        _btnVender10.onClick.AddListener(() => EjecutarVenta(10));
        Debug.Log($"[MarketRowUI] Listener registrado: BtnVender10 para {_bien?.nombre}");
        _btnVender100.onClick.AddListener(() => EjecutarVenta(100));
        Debug.Log($"[MarketRowUI] Listener registrado: BtnVender100 para {_bien?.nombre}");

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
    /// Actualiza todos los elementos visuales de la fila: stocks de las dos columnas ciclables,
    /// etiquetas de columna, precio (solo si alguna columna es Mercado) e indicador de color.
    /// </summary>
    private void Refrescar()
    {
        if (_bien == null)
        {
            Debug.LogWarning("[MarketRowUI] _bien es null; se omite el refresco.");
            return;
        }

        if (_marketManager == null)
        {
            Debug.LogWarning($"[MarketRowUI] _marketManager es null en fila '{_bien.nombre}'; se omite el refresco.");
            return;
        }

        if (_oficina == null)
        {
            Debug.LogWarning($"[MarketRowUI] _oficina es null en fila '{_bien.nombre}'; se omite el refresco.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MarketRowUI] GameManager.Instance es null; se omite el refresco.");
            return;
        }

        if (_textoStockCiudad == null || _textoStockAlmacen == null || _textoPrecio == null)
        {
            Debug.LogWarning($"[MarketRowUI] Una o más referencias de TextMeshProUGUI no están asignadas en fila '{_bien.nombre}'.");
            return;
        }

        // Stocks de las dos columnas ciclables
        _textoStockCiudad.text  = GetStock(_columnaIzq).ToString("N0");
        _textoStockAlmacen.text = GetStock(_columnaDer).ToString("N0");

        // Etiquetas de columna
        if (_textoEtiquetaIzq != null) _textoEtiquetaIzq.text = EtiquetaColumna(_columnaIzq);
        if (_textoEtiquetaDer != null) _textoEtiquetaDer.text = EtiquetaColumna(_columnaDer);

        // Precio: solo si alguna columna es Mercado
        bool hayMercado = _columnaIzq == OficinaComercial.OrigenDestino.Mercado
                       || _columnaDer == OficinaComercial.OrigenDestino.Mercado;
        float precioActual = _marketManager.GetPrecioActual(_bien);
        _textoPrecio.text = hayMercado ? $"{precioActual:N0} g" : "—";

        // Botones comprar/vender: activos solo cuando el Mercado está en alguna columna
        bool comprarActivo = _columnaIzq == OficinaComercial.OrigenDestino.Mercado;
        bool venderActivo  = _columnaDer == OficinaComercial.OrigenDestino.Mercado;
        _btnComprar1.interactable   = comprarActivo;
        _btnComprar10.interactable  = comprarActivo;
        _btnComprar100.interactable = comprarActivo;
        _btnVender1.interactable    = venderActivo;
        _btnVender10.interactable   = venderActivo;
        _btnVender100.interactable  = venderActivo;

        if (hayMercado) ActualizarIndicadorColor(precioActual);

        Debug.Log($"[MarketRowUI] Refrescando '{_bien.nombre}': izq={_columnaIzq}({GetStock(_columnaIzq)}), der={_columnaDer}({GetStock(_columnaDer)})");
    }

    // ─── Columnas ciclables ──────────────────────────────────────────────────

    /// <summary>Cicla la columna izquierda por los tres valores del enum y refresca la fila.</summary>
    private void CiclarColumnaIzq()
    {
        _columnaIzq = (OficinaComercial.OrigenDestino)(((int)_columnaIzq + 1) % 3);
        Refrescar();
    }

    /// <summary>Cicla la columna derecha por los tres valores del enum y refresca la fila.</summary>
    private void CiclarColumnaDer()
    {
        _columnaDer = (OficinaComercial.OrigenDestino)(((int)_columnaDer + 1) % 3);
        Refrescar();
    }

    /// <summary>Devuelve la etiqueta legible de una columna para mostrar al jugador.</summary>
    private string EtiquetaColumna(OficinaComercial.OrigenDestino col) => col switch
    {
        OficinaComercial.OrigenDestino.Mercado       => "Mercado ciudad",
        OficinaComercial.OrigenDestino.AlmacenCiudad => "Almacén ciudad",
        OficinaComercial.OrigenDestino.BodegaBarco   => "Bodega barco",
        _                                             => string.Empty
    };

    /// <summary>Devuelve el stock actual de la columna indicada.</summary>
    private int GetStock(OficinaComercial.OrigenDestino col)
    {
        if (GameManager.Instance == null) return 0;
        return col switch
        {
            OficinaComercial.OrigenDestino.Mercado       => _marketManager.GetStockActual(_bien),
            OficinaComercial.OrigenDestino.AlmacenCiudad => GameManager.Instance.GetCantidadAlmacenCiudad(_idCiudad, _idBien),
            OficinaComercial.OrigenDestino.BodegaBarco   => GameManager.Instance.GetCantidadBien(_bien),
            _                                             => 0
        };
    }

    /// <summary>
    /// Cambia el color del indicador comparando el precio actual con el precio base del bien:
    /// verde si el precio está por debajo del 80 % del base (stock alto, buen momento para vender aquí),
    /// rojo si supera el 120 % (stock bajo, buen momento para comprar aquí),
    /// amarillo en el tramo intermedio (precio normal).
    /// </summary>
    /// <param name="precioActual">Precio de mercado actual del bien.</param>
    private void ActualizarIndicadorColor(float precioActual)
    {
        if (_indicadorColor == null) return;

        float precioBase = _bien.precioBase;

        if (precioActual <= precioBase * 1.0f)
            // Stock igual o mayor al máximo → precio igual o menor al base → buen momento para vender aquí
            _indicadorColor.color = Color.green;
        else if (precioActual <= precioBase * 2.0f)
            // Stock entre 50% y 100% del máximo → precio normal
            _indicadorColor.color = Color.yellow;
        else
            // Stock por debajo del 50% del máximo → escasez → buen momento para comprar aquí
            _indicadorColor.color = Color.red;
    }

    // ─── Acciones de compra/venta ────────────────────────────────────────────

    /// <summary>
    /// Intenta comprar la cantidad indicada del bien a través de la oficina comercial.
    /// Si la operación falla, la interfaz permanece sin cambios y el motivo queda registrado
    /// en <see cref="OficinaComercial.UltimoMensaje"/>.
    /// </summary>
    /// <param name="cantidad">Unidades que se desean comprar.</param>
    private void EjecutarCompra(int cantidad)
    {
        Debug.Log($"[MarketRowUI] EjecutarCompra({cantidad}) para {_bien?.nombre}");
        if (_oficina == null)
        {
            Debug.LogWarning("[MarketRowUI] OficinaComercial no asignada; no se puede ejecutar la compra.");
            return;
        }

        if (_columnaIzq == OficinaComercial.OrigenDestino.Mercado)
            _oficina.Comprar(_bien, cantidad);
        else
            _oficina.Transferir(_bien, cantidad, _columnaIzq, _columnaDer);
        Refrescar();
    }

    /// <summary>
    /// Intenta vender la cantidad indicada del bien a través de la oficina comercial.
    /// Si el jugador no tiene suficiente mercancía en bodega, la operación no se realiza
    /// y el motivo queda registrado en <see cref="OficinaComercial.UltimoMensaje"/>.
    /// </summary>
    /// <param name="cantidad">Unidades que se desean vender.</param>
    private void EjecutarVenta(int cantidad)
    {
        Debug.Log($"[MarketRowUI] EjecutarVenta({cantidad}) para {_bien?.nombre}");
        if (_oficina == null)
        {
            Debug.LogWarning("[MarketRowUI] OficinaComercial no asignada; no se puede ejecutar la venta.");
            return;
        }

        if (_columnaDer == OficinaComercial.OrigenDestino.Mercado)
            _oficina.Vender(_bien, cantidad);
        else
            _oficina.Transferir(_bien, cantidad, _columnaIzq, _columnaDer);
        Refrescar();
    }
}
