using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Interfaz de la taberna con tres subpaneles: Menú principal, Contratar Marineros
/// y Contratar Capitán. Solo un subpanel es visible a la vez.
/// Adjuntar a un GameObject en la escena Ciudad y cablear todos los campos desde el Inspector.
/// </summary>
public class TabernaUI : MonoBehaviour
{
    // ─── Panel raíz ───────────────────────────────────────────────────────────
    [SerializeField] private GameObject _panelTaberna;

    // ─── Subpaneles ───────────────────────────────────────────────────────────
    [SerializeField] private GameObject _panelMenu;
    [SerializeField] private GameObject _panelMarineros;
    [SerializeField] private GameObject _panelCapitan;

    // ─── Panel Marineros ──────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoBarcoMarineros;
    [SerializeField] private TextMeshProUGUI _textoTripulacion;
    [SerializeField] private TextMeshProUGUI _textoMarinerosDisponibles;
    [SerializeField] private TextMeshProUGUI _textoCantidad;
    [SerializeField] private TextMeshProUGUI _textoCosteMar;
    [SerializeField] private Button          _btnBarcoMarIzq;
    [SerializeField] private Button          _btnBarcoMarDer;
    [SerializeField] private Button          _btnCantidadMas;
    [SerializeField] private Button          _btnCantidadMenos;
    [SerializeField] private Button          _btnContratarMarineros;
    [SerializeField] private Button          _btnVolverDesdeMarineros;

    // ─── Panel Capitán ────────────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoBarcoCapitan;
    [SerializeField] private TextMeshProUGUI _textoCapitanActual;
    [SerializeField] private Button          _btnBarcoCaplzq;
    [SerializeField] private Button          _btnBarcoCapDer;
    [SerializeField] private Transform       _contenedorListaCapitanes;
    [SerializeField] private GameObject      _prefabFilaCapitan;
    [SerializeField] private Button          _btnVolverDesdeCapitan;

    // ─── Índices de selección ─────────────────────────────────────────────────
    private int _indiceBarcoMar    = 0;
    private int _indiceBarcoCap    = 0;
    private int _cantidadMarineros = 1;

    // ─── Aceleración botones +/- ──────────────────────────────────────────────
    private Coroutine _corrutinaCantidad;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_panelTaberna != null) _panelTaberna.SetActive(false);

        // Marineros — flechas barco y contratar usan onClick normal
        _btnBarcoMarIzq        ?.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMar, -1, RefrescarPanelMarineros));
        _btnBarcoMarDer        ?.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMar, +1, RefrescarPanelMarineros));
        _btnContratarMarineros ?.onClick.AddListener(OnContratarMarineros);
        _btnVolverDesdeMarineros?.onClick.AddListener(() => MostrarPanel(0));

        // +/- usan EventTrigger para detectar mantener pulsado con aceleración
        if (_btnCantidadMas   != null) AñadirEventosPulsacion(_btnCantidadMas,   +1);
        if (_btnCantidadMenos != null) AñadirEventosPulsacion(_btnCantidadMenos, -1);

        // Capitán
        _btnBarcoCaplzq       ?.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoCap, -1, RefrescarPanelCapitan));
        _btnBarcoCapDer       ?.onClick.AddListener(() => CiclarBarco(ref _indiceBarcoCap, +1, RefrescarPanelCapitan));
        _btnVolverDesdeCapitan?.onClick.AddListener(() => MostrarPanel(0));
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el panel de la taberna y muestra el menú principal.
    /// Refresca todos los datos antes de mostrar.
    /// </summary>
    public void AbrirTaberna()
    {
        _panelTaberna.SetActive(true);
        MostrarPanel(0);
        RefrescarUI();
    }

    /// <summary>Cierra el panel de la taberna y restaura el botón de mapa en la pantalla de ciudad.</summary>
    public void CerrarTaberna()
    {
        _panelTaberna.SetActive(false);
        FindFirstObjectByType<CiudadController>()?.CerrarTodosPaneles();
    }

    /// <summary>
    /// Actualiza todos los textos y estados según los índices actuales.
    /// Llamar siempre que cambie cualquier selector o se complete una operación.
    /// </summary>
    public void RefrescarUI()
    {
        RefrescarPanelMarineros();
        RefrescarPanelCapitan();
    }

    private void OnDisable() => DetenerAceleracion();

    // ─── Navegación de subpaneles ─────────────────────────────────────────────

    /// <summary>Muestra el subpanel indicado y oculta los demás.</summary>
    /// <param name="indice">0=Menú, 1=Marineros, 2=Capitán.</param>
    public void MostrarPanel(int indice)
    {
        // Cancelar aceleración si salimos del subpanel de marineros
        if (indice != 1) DetenerAceleracion();

        _panelMenu     .SetActive(indice == 0);
        _panelMarineros.SetActive(indice == 1);
        _panelCapitan  .SetActive(indice == 2);

        if (indice == 1) { _cantidadMarineros = 1; RefrescarPanelMarineros(); }
        if (indice == 2) RefrescarPanelCapitan();
    }

    // ─── Ciclar selectores ────────────────────────────────────────────────────

    private void CiclarBarco(ref int indice, int dir, System.Action postCiclo)
    {
        int count = GameManager.Instance.FlotaJugador.Barcos.Count;
        if (count == 0) return;
        indice = Modulo(indice + dir, count);
        postCiclo?.Invoke();
    }

    private void CambiarCantidad(int dir)
    {
        BarcoJugador barco = ObtenerBarco(_indiceBarcoMar);
        if (barco == null) return;

        // El máximo es el hueco del barco; la oferta de ciudad es ilimitada
        int hueco  = barco.TripulacionMaxima - barco.Tripulacion;
        int maximo = Mathf.Max(1, hueco);

        _cantidadMarineros = Mathf.Clamp(_cantidadMarineros + dir, 1, maximo);
        RefrescarPanelMarineros();
    }

    // ─── Refresco por panel ───────────────────────────────────────────────────

    private void RefrescarPanelMarineros()
    {
        BarcoJugador barco = ObtenerBarco(_indiceBarcoMar);
        if (barco == null)
        {
            if (_textoBarcoMarineros       != null) _textoBarcoMarineros.text       = "Sin barcos en la flota";
            if (_textoTripulacion          != null) _textoTripulacion.text          = "";
            if (_textoMarinerosDisponibles != null) _textoMarinerosDisponibles.text = "";
            if (_textoCantidad             != null) _textoCantidad.text             = "0";
            if (_textoCosteMar             != null) _textoCosteMar.text             = "";
            if (_btnContratarMarineros     != null) _btnContratarMarineros.interactable = false;
            return;
        }

        // El máximo es el hueco del barco; sin límite de oferta por ciudad
        int hueco  = barco.TripulacionMaxima - barco.Tripulacion;
        int maximo = Mathf.Max(1, hueco);
        _cantidadMarineros = Mathf.Clamp(_cantidadMarineros, 1, maximo);

        if (_textoBarcoMarineros       != null) _textoBarcoMarineros.text       = barco.Nombre;
        if (_textoTripulacion          != null) _textoTripulacion.text          = $"Tripulación: {barco.Tripulacion}/{barco.TripulacionMaxima}";
        if (_textoMarinerosDisponibles != null) _textoMarinerosDisponibles.text = $"Precio por marinero: {TabernaManager.PrecioMarinero} ƒ";
        if (_textoCantidad             != null) _textoCantidad.text             = _cantidadMarineros.ToString();
        if (_textoCosteMar             != null) _textoCosteMar.text             = $"Total: {(long)_cantidadMarineros * TabernaManager.PrecioMarinero} ƒ";

        bool hayDinero = GameManager.Instance.Dinero >= (long)_cantidadMarineros * TabernaManager.PrecioMarinero;
        if (_btnContratarMarineros != null)
            _btnContratarMarineros.interactable = hueco > 0 && hayDinero;
    }

    private void RefrescarPanelCapitan()
    {
        BarcoJugador barco = ObtenerBarco(_indiceBarcoCap);
        if (barco == null)
        {
            _textoBarcoCapitan .text = "Sin barcos en la flota";
            _textoCapitanActual.text = "";
            LimpiarListaCapitanes();
            return;
        }

        _textoBarcoCapitan.text = barco.Nombre;

        CapitanData capActual = TabernaManager.Instance?.GetCapitanDeBarco(barco.IdBarco);
        _textoCapitanActual.text = capActual != null
            ? $"Capitán: {capActual.Nombre}  Nav:{capActual.HabilidadNavegacion:F1}  Com:{capActual.HabilidadCombate:F1}"
            : "Sin capitán";

        LimpiarListaCapitanes();

        int idCiudad = GameManager.Instance.CiudadActual?.IdCiudad ?? -1;
        if (idCiudad < 0 || _prefabFilaCapitan == null || _contenedorListaCapitanes == null) return;

        List<CapitanData> disponibles = TabernaManager.Instance?.GetCapitanesDisponibles(idCiudad)
                                        ?? new List<CapitanData>();

        bool barcoLibre = capActual == null;

        foreach (CapitanData cap in disponibles)
        {
            GameObject fila = Instantiate(_prefabFilaCapitan, _contenedorListaCapitanes);

            TextMeshProUGUI texto = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null)
                texto.text = $"{cap.Nombre}  Nav:{cap.HabilidadNavegacion:F1}  Com:{cap.HabilidadCombate:F1}  —  500 oro";

            Button btn = fila.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.interactable = barcoLibre && GameManager.Instance.Dinero >= 500L;
                CapitanData capCapturado = cap;
                btn.onClick.AddListener(() =>
                {
                    ResultadoOperacion r = TabernaManager.Instance.ContratarCapitan(barco, capCapturado);
                    if (!r.Exito) Debug.LogWarning($"[TabernaUI] ContratarCapitan: {r.MensajeError}");
                    RefrescarUI();
                });
            }
        }
    }

    private void LimpiarListaCapitanes()
    {
        if (_contenedorListaCapitanes == null) return;
        for (int i = _contenedorListaCapitanes.childCount - 1; i >= 0; i--)
            Destroy(_contenedorListaCapitanes.GetChild(i).gameObject);
    }

    // ─── Operaciones ──────────────────────────────────────────────────────────

    private void OnContratarMarineros()
    {
        BarcoJugador barco = ObtenerBarco(_indiceBarcoMar);
        if (barco == null) return;

        ResultadoOperacion r = TabernaManager.Instance.ContratarMarineros(barco, _cantidadMarineros);
        if (!r.Exito) Debug.LogWarning($"[TabernaUI] ContratarMarineros: {r.MensajeError}");
        _cantidadMarineros = 1;
        RefrescarUI();
    }

    // ─── Aceleración botones +/- ──────────────────────────────────────────────

    /// <summary>
    /// Registra eventos PointerDown/PointerUp en el botón para activar la corrutina
    /// de aceleración. El primer cambio es inmediato; después escala de 1→5→10 por paso.
    /// </summary>
    /// <param name="btn">Botón al que añadir los eventos.</param>
    /// <param name="dir">Dirección del cambio: +1 para incrementar, -1 para decrementar.</param>
    private void AñadirEventosPulsacion(Button btn, int dir)
    {
        EventTrigger trigger = btn.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();

        var abajo = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        abajo.callback.AddListener(_ =>
        {
            if (_corrutinaCantidad != null) StopCoroutine(_corrutinaCantidad);
            _corrutinaCantidad = StartCoroutine(CorrutinaCantidad(dir));
        });
        trigger.triggers.Add(abajo);

        var arriba = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        arriba.callback.AddListener(_ => DetenerAceleracion());
        trigger.triggers.Add(arriba);
    }

    /// <summary>Detiene la corrutina de aceleración si está activa.</summary>
    private void DetenerAceleracion()
    {
        if (_corrutinaCantidad == null) return;
        StopCoroutine(_corrutinaCantidad);
        _corrutinaCantidad = null;
    }

    /// <summary>
    /// Corrutina de aceleración para los botones +/-:
    /// fase 1 (0-0.5 s): pasos de 1 cada 150 ms,
    /// fase 2 (0.5-2 s): pasos de 5 cada 100 ms,
    /// fase 3 (>2 s): pasos de 10 cada 80 ms.
    /// El clamp de CambiarCantidad garantiza que el valor no sale del rango [1, hueco].
    /// </summary>
    private IEnumerator CorrutinaCantidad(int dir)
    {
        // Primer cambio inmediato (equivale al click normal)
        CambiarCantidad(dir);

        float t0 = Time.unscaledTime;

        // Fase 1: pasos de 1 hasta 0.5 s
        while (Time.unscaledTime - t0 < 0.5f)
        {
            yield return new WaitForSecondsRealtime(0.15f);
            CambiarCantidad(dir);
        }

        // Fase 2: pasos de 5 hasta 2 s
        while (Time.unscaledTime - t0 < 2f)
        {
            yield return new WaitForSecondsRealtime(0.10f);
            CambiarCantidad(5 * dir);
        }

        // Fase 3: pasos de 10 indefinidamente
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.08f);
            CambiarCantidad(10 * dir);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private BarcoJugador ObtenerBarco(int indice)
    {
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        if (barcos == null || barcos.Count == 0) return null;
        return barcos[Modulo(indice, barcos.Count)];
    }

    private static int Modulo(int valor, int total)
        => total <= 0 ? 0 : ((valor % total) + total) % total;
}
