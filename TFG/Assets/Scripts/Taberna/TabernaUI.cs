using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    private int _indiceBarcoMar = 0;
    private int _indiceBarcoCap = 0;
    private int _cantidadMarineros = 1;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _panelTaberna.SetActive(false);

        // Marineros
        _btnBarcoMarIzq        .onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMar, -1, RefrescarPanelMarineros));
        _btnBarcoMarDer        .onClick.AddListener(() => CiclarBarco(ref _indiceBarcoMar, +1, RefrescarPanelMarineros));
        _btnCantidadMas        .onClick.AddListener(() => CambiarCantidad(+1));
        _btnCantidadMenos      .onClick.AddListener(() => CambiarCantidad(-1));
        _btnContratarMarineros .onClick.AddListener(OnContratarMarineros);
        _btnVolverDesdeMarineros.onClick.AddListener(() => MostrarPanel(0));

        // Capitán
        _btnBarcoCaplzq       .onClick.AddListener(() => CiclarBarco(ref _indiceBarcoCap, -1, RefrescarPanelCapitan));
        _btnBarcoCapDer       .onClick.AddListener(() => CiclarBarco(ref _indiceBarcoCap, +1, RefrescarPanelCapitan));
        _btnVolverDesdeCapitan.onClick.AddListener(() => MostrarPanel(0));
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

    /// <summary>Cierra el panel de la taberna.</summary>
    public void CerrarTaberna()
    {
        _panelTaberna.SetActive(false);
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

    // ─── Navegación de subpaneles ─────────────────────────────────────────────

    /// <summary>Muestra el subpanel indicado y oculta los demás.</summary>
    /// <param name="indice">0=Menú, 1=Marineros, 2=Capitán.</param>
    public void MostrarPanel(int indice)
    {
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

        int disponibles = GetMarinerosDisponibles();
        int hueco       = barco.CascoBase.CapacidadTripulacion - barco.Tripulacion;
        int maximo      = Mathf.Max(1, Mathf.Min(disponibles, hueco));

        _cantidadMarineros = Mathf.Clamp(_cantidadMarineros + dir, 1, maximo);
        RefrescarPanelMarineros();
    }

    // ─── Refresco por panel ───────────────────────────────────────────────────

    private void RefrescarPanelMarineros()
    {
        BarcoJugador barco = ObtenerBarco(_indiceBarcoMar);
        if (barco == null)
        {
            _textoBarcoMarineros      .text = "Sin barcos en la flota";
            _textoTripulacion         .text = "";
            _textoMarinerosDisponibles.text = "";
            _textoCantidad            .text = "0";
            _textoCosteMar            .text = "";
            _btnContratarMarineros.interactable = false;
            return;
        }

        int disponibles = GetMarinerosDisponibles();
        int hueco       = barco.CascoBase.CapacidadTripulacion - barco.Tripulacion;
        int maximo      = Mathf.Max(1, Mathf.Min(disponibles, hueco));
        _cantidadMarineros = Mathf.Clamp(_cantidadMarineros, 1, maximo);

        _textoBarcoMarineros      .text = barco.Nombre;
        _textoTripulacion         .text = $"Tripulación: {barco.Tripulacion}/{barco.CascoBase.CapacidadTripulacion}";
        _textoMarinerosDisponibles.text = $"Marineros disponibles: {disponibles}";
        _textoCantidad            .text = _cantidadMarineros.ToString();
        _textoCosteMar            .text = $"Coste: {_cantidadMarineros * 5} oro";

        bool hayHueco   = hueco > 0 && disponibles > 0;
        bool hayDinero  = GameManager.Instance.Dinero >= _cantidadMarineros * 5L;
        _btnContratarMarineros.interactable = hayHueco && hayDinero;
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

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private BarcoJugador ObtenerBarco(int indice)
    {
        var barcos = GameManager.Instance.FlotaJugador.Barcos;
        if (barcos == null || barcos.Count == 0) return null;
        return barcos[Modulo(indice, barcos.Count)];
    }

    private int GetMarinerosDisponibles()
    {
        int idCiudad = GameManager.Instance.CiudadActual?.IdCiudad ?? -1;
        if (idCiudad < 0) return 0;
        return TabernaManager.Instance?.GetMarinerosDisponibles(idCiudad) ?? 0;
    }

    private static int Modulo(int valor, int total)
        => total <= 0 ? 0 : ((valor % total) + total) % total;
}
