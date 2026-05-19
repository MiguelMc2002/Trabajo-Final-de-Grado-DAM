using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel lateral siempre visible en Ciudad y Mapamundi que muestra los datos del barco
/// seleccionado, su capitán y convoy, y permite acciones rápidas sobre la flota.
/// Adjuntar a un GameObject permanente y cablear todos los campos desde el Inspector.
/// </summary>
public class PanelFlotaUI : MonoBehaviour
{
    // ─── Paneles ──────────────────────────────────────────────────────────────
    [SerializeField] private GameObject _panelFlota;
    [SerializeField] private GameObject _panelInfoBarco;
    [SerializeField] private GameObject _panelUnirseConvoy;

    // ─── Textos — info barco ──────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoNombreBarco;
    [SerializeField] private TextMeshProUGUI _textoCasco;
    [SerializeField] private TextMeshProUGUI _textoVida;
    [SerializeField] private TextMeshProUGUI _textoVelocidad;
    [SerializeField] private TextMeshProUGUI _textoManiobra;
    [SerializeField] private TextMeshProUGUI _textoCarga;
    [SerializeField] private TextMeshProUGUI _textoFuerza;
    [SerializeField] private TextMeshProUGUI _textoTripulacion;
    [SerializeField] private TextMeshProUGUI _textoModulos;
    [SerializeField] private TextMeshProUGUI _textoCapitan;
    [SerializeField] private TextMeshProUGUI _textoConvoy;

    // ─── Botones de acción ────────────────────────────────────────────────────
    /// <summary>Botón que cierra el panel de flota.</summary>
    [SerializeField] private Button _btnCerrar;
    [SerializeField] private Button _btnVerBodega;
    [SerializeField] private Button _btnFormarConvoy;
    [SerializeField] private Button _btnUnirseConvoy;
    [SerializeField] private Button _btnVolverConvoy;

    // ─── Toggle Modo Pirata ───────────────────────────────────────────────────
    [SerializeField] private Toggle _toggleModoPirata;

    // ─── Lista de convoyes ────────────────────────────────────────────────────
    [SerializeField] private Transform  _contenedorListaConvoyes;
    [SerializeField] private GameObject _prefabFilaConvoy;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private BarcoJugador _barcoSeleccionado;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_btnCerrar != null)
            _btnCerrar.onClick.AddListener(OcultarPanel);
    }

    private void Start()
    {
        _panelFlota.SetActive(false);

        _btnVerBodega   .onClick.AddListener(OnVerBodega);
        _btnFormarConvoy.onClick.AddListener(OnFormarConvoy);
        _btnUnirseConvoy.onClick.AddListener(OnMostrarUnirseConvoy);
        _btnVolverConvoy.onClick.AddListener(OnVolverDesdeConvoy);

        _toggleModoPirata.onValueChanged.AddListener(OnToggleModoPirata);

        CiudadController ciudad = FindObjectOfType<CiudadController>();
        if (ciudad != null)
            ciudad.CerrarTodosPaneles();
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>Abre el panel de flota como modal, cerrando el resto de paneles de ciudad.</summary>
    public void AbrirPanel()
    {
        CiudadController ciudad = FindObjectOfType<CiudadController>();
        if (ciudad != null)
            ciudad.CerrarTodosPaneles();

        _panelFlota.SetActive(true);
        _panelInfoBarco   .SetActive(true);
        _panelUnirseConvoy.SetActive(false);
    }

    /// <summary>
    /// Muestra el panel con los datos del barco indicado.
    /// Activa <c>_panelInfoBarco</c> y oculta el subpanel de convoyes.
    /// </summary>
    /// <param name="barco">Barco cuyos datos se mostrarán.</param>
    public void MostrarBarco(BarcoJugador barco)
    {
        _barcoSeleccionado = barco;
        _panelFlota.SetActive(true);
        _panelInfoBarco   .SetActive(true);
        _panelUnirseConvoy.SetActive(false);
        RefrescarUI();
    }

    /// <summary>Oculta el panel de flota completo.</summary>
    public void OcultarPanel()
    {
        _panelFlota.SetActive(false);
    }

    /// <summary>
    /// Actualiza todos los textos con los datos del barco seleccionado actualmente.
    /// Si no hay barcos en la flota, desactiva los botones de acción.
    /// </summary>
    public void RefrescarUI()
    {
        if (_barcoSeleccionado == null ||
            GameManager.Instance.FlotaJugador.Barcos.Count == 0)
        {
            _textoNombreBarco.text = "Sin barcos en la flota";
            _textoCasco      .text = "";
            _textoVida       .text = "";
            _textoVelocidad  .text = "";
            _textoManiobra   .text = "";
            _textoCarga      .text = "";
            _textoFuerza     .text = "";
            _textoTripulacion.text = "";
            _textoModulos    .text = "";
            _textoCapitan    .text = "";
            _textoConvoy     .text = "";

            _btnVerBodega   .interactable = false;
            _btnFormarConvoy.interactable = false;
            _btnUnirseConvoy.interactable = false;
            return;
        }

        BarcoJugador b = _barcoSeleccionado;

        // Info barco
        _textoNombreBarco.text = b.Nombre;
        _textoCasco      .text = b.CascoBase.NombreCasco;
        _textoVida       .text = $"Vida: {b.VidaActual}/{b.VidaTotal}";
        _textoVelocidad  .text = $"Velocidad: {b.VelocidadTotal}";
        _textoManiobra   .text = $"Maniobrabilidad: {b.ManiobrabilidadTotal}";
        _textoCarga      .text = $"Carga máxima: {b.CargaMaximaTotal}";
        _textoFuerza     .text = $"Fuerza de combate: {b.FuerzaCombateTotal}";
        _textoTripulacion.text = $"Tripulación: {b.Tripulacion}/{b.CascoBase.CapacidadTripulacion}";

        // Módulos
        if (b.ModulosInstalados.Count == 0)
        {
            _textoModulos.text = "Módulos: Vacío";
        }
        else
        {
            var nombres = new System.Text.StringBuilder("Módulos: ");
            for (int i = 0; i < b.ModulosInstalados.Count; i++)
            {
                if (i > 0) nombres.Append(", ");
                nombres.Append(b.ModulosInstalados[i].nombreModulo);
            }
            _textoModulos.text = nombres.ToString();
        }

        // Capitán
        CapitanData cap = TabernaManager.Instance?.GetCapitanDeBarco(b.IdBarco);
        _textoCapitan.text = cap != null
            ? $"Capitán: {cap.Nombre}  Nav:{cap.HabilidadNavegacion:F1}  Com:{cap.HabilidadCombate:F1}"
            : "Sin capitán";

        // Convoy
        ConvoyData convoy = ConvoyManager.Instance?.GetConvoyDeBarco(b);
        _textoConvoy.text = convoy != null
            ? $"Convoy: {convoy.NombreConvoy}"
            : "Sin convoy";

        // Toggle Modo Pirata — sin disparar el listener
        _toggleModoPirata.SetIsOnWithoutNotify(GameManager.Instance.FlotaJugador.ModoPirata);

        // Botones
        _btnVerBodega   .interactable = true;
        _btnFormarConvoy.interactable = convoy == null;
        _btnUnirseConvoy.interactable = convoy == null && ConvoyManager.Instance != null
                                        && ConvoyManager.Instance.ConvoysActivos.Count > 0;
    }

    // ─── Operaciones ──────────────────────────────────────────────────────────

    private void OnVerBodega()
    {
        Debug.Log($"[PanelFlotaUI] Ver bodega de '{_barcoSeleccionado?.Nombre}' — pendiente de implementar UI.");
    }

    private void OnFormarConvoy()
    {
        if (_barcoSeleccionado == null) return;
        ConvoyManager.Instance.CrearConvoy(_barcoSeleccionado);
        RefrescarUI();
    }

    private void OnMostrarUnirseConvoy()
    {
        _panelInfoBarco   .SetActive(false);
        _panelUnirseConvoy.SetActive(true);
        ReconstruirListaConvoyes();
    }

    private void OnVolverDesdeConvoy()
    {
        _panelUnirseConvoy.SetActive(false);
        _panelInfoBarco   .SetActive(true);
        RefrescarUI();
    }

    private void OnToggleModoPirata(bool valor)
    {
        GameManager.Instance.FlotaJugador.ModoPirata = valor;
        Debug.Log($"[PanelFlotaUI] Modo Pirata: {valor}");
        RefrescarUI();
    }

    // ─── Lista dinámica de convoyes ───────────────────────────────────────────

    private void ReconstruirListaConvoyes()
    {
        if (_contenedorListaConvoyes == null) return;

        // Limpiar filas anteriores
        for (int i = _contenedorListaConvoyes.childCount - 1; i >= 0; i--)
            Destroy(_contenedorListaConvoyes.GetChild(i).gameObject);

        if (_prefabFilaConvoy == null || ConvoyManager.Instance == null) return;

        foreach (ConvoyData convoy in ConvoyManager.Instance.ConvoysActivos)
        {
            GameObject fila = Instantiate(_prefabFilaConvoy, _contenedorListaConvoyes);

            TextMeshProUGUI texto = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null)
                texto.text = $"{convoy.NombreConvoy}  ({convoy.Miembros.Count}/5 barcos)";

            Button btn = fila.GetComponentInChildren<Button>();
            if (btn != null)
            {
                ConvoyData convoyCapturado = convoy;
                btn.onClick.AddListener(() =>
                {
                    if (_barcoSeleccionado == null) return;
                    bool ok = ConvoyManager.Instance.UnirseAConvoy(convoyCapturado, _barcoSeleccionado);
                    if (!ok) Debug.LogWarning($"[PanelFlotaUI] No se pudo unir al convoy '{convoyCapturado.NombreConvoy}'.");
                    OnVolverDesdeConvoy();
                });
            }
        }
    }
}
