using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel lateral en la escena Ciudad que muestra los datos del barco seleccionado,
/// permite navegar entre barcos con flechas y consultar la bodega del jugador.
/// </summary>
public class PanelFlotaUI : MonoBehaviour
{
    // ─── Paneles ──────────────────────────────────────────────────────────────
    [SerializeField] private GameObject _panelFlota;
    [SerializeField] private GameObject _panelInfoBarco;
    [SerializeField] private GameObject _panelBodega;

    // ─── Textos — info barco ──────────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI _textoNombreBarco;
    [SerializeField] private TextMeshProUGUI _txtIndiceBarco;
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

    // ─── Navegación entre barcos ──────────────────────────────────────────────
    [SerializeField] private Button _btnAnterior;
    [SerializeField] private Button _btnSiguiente;

    // ─── Botones de acción ────────────────────────────────────────────────────
    /// <summary>Botón que cierra el panel de flota.</summary>
    [SerializeField] private Button _btnCerrar;
    [SerializeField] private Button _btnVerBodega;

    // ─── Toggle Modo Pirata ───────────────────────────────────────────────────
    [SerializeField] private Toggle _toggleModoPirata;

    // ─── Subpanel bodega ──────────────────────────────────────────────────────
    [SerializeField] private Transform _contenedorBodega;
    [SerializeField] private Button    _btnVolver;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private BarcoJugador _barcoSeleccionado;
    private int          _indiceBarcoActual;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_btnCerrar    != null) _btnCerrar.onClick.AddListener(OcultarPanel);
        if (_btnAnterior  != null) _btnAnterior.onClick.AddListener(() => CiclarBarco(-1));
        if (_btnSiguiente != null) _btnSiguiente.onClick.AddListener(() => CiclarBarco(+1));
        if (_btnVolver    != null) _btnVolver.onClick.AddListener(OnVolverDesdeBodega);
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PanelFlotaUI] GameManager.Instance es null en Start.");
            return;
        }

        if (_panelFlota  != null) _panelFlota.SetActive(false);
        if (_panelBodega != null) _panelBodega.SetActive(false);

        if (_btnVerBodega     != null) _btnVerBodega.onClick.AddListener(OnVerBodega);
        if (_toggleModoPirata != null) _toggleModoPirata.onValueChanged.AddListener(OnToggleModoPirata);
    }

    private void Update()
    {
        // Tecla F: toggle del panel. Se ignora si el subpanel de bodega está abierto.
        if (!Input.GetKeyDown(KeyCode.F)) return;
        if (_panelBodega != null && _panelBodega.activeSelf) return;

        if (_panelFlota != null && _panelFlota.activeSelf)
            OcultarPanel();
        else
            AbrirPanel();
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>Abre el panel de flota como modal, cerrando el resto de paneles de ciudad.</summary>
    public void AbrirPanel()
    {
        CiudadController ciudad = FindFirstObjectByType<CiudadController>();
        if (ciudad != null)
            ciudad.CerrarTodosPaneles();

        // Auto-seleccionar primer barco si ninguno estaba seleccionado
        if (_barcoSeleccionado == null && GameManager.Instance != null)
        {
            var barcos = GameManager.Instance.FlotaJugador.Barcos;
            if (barcos.Count > 0)
            {
                _indiceBarcoActual = 0;
                _barcoSeleccionado = barcos[0];
            }
        }

        if (_panelFlota     != null) _panelFlota.SetActive(true);
        if (_panelInfoBarco != null) _panelInfoBarco.SetActive(true);
        if (_panelBodega    != null) _panelBodega.SetActive(false);
        RefrescarUI();
    }

    /// <summary>
    /// Muestra el panel con los datos del barco indicado.
    /// </summary>
    /// <param name="barco">Barco cuyos datos se mostrarán.</param>
    public void MostrarBarco(BarcoJugador barco)
    {
        _barcoSeleccionado = barco;
        if (_panelFlota     != null) _panelFlota.SetActive(true);
        if (_panelInfoBarco != null) _panelInfoBarco.SetActive(true);
        if (_panelBodega    != null) _panelBodega.SetActive(false);
        RefrescarUI();
    }

    /// <summary>Oculta el panel de flota completo.</summary>
    public void OcultarPanel()
    {
        if (_panelFlota != null) _panelFlota.SetActive(false);
    }

    /// <summary>
    /// Refresca el panel tras una operación en el astillero (compra, venta, modificación, reparación).
    /// Si el barco seleccionado ya no existe en la flota, cambia al primero disponible.
    /// </summary>
    public void RefrescarPanel()
    {
        if (GameManager.Instance == null) return;

        var barcos = GameManager.Instance.FlotaJugador.Barcos;

        // Detectar si el barco fue eliminado de la flota
        if (_barcoSeleccionado != null)
        {
            bool sigueEnFlota = false;
            foreach (BarcoJugador b in barcos)
                if (b == _barcoSeleccionado) { sigueEnFlota = true; break; }
            if (!sigueEnFlota)
            {
                _indiceBarcoActual = 0;
                _barcoSeleccionado = barcos.Count > 0 ? barcos[0] : null;
            }
        }
        else if (barcos.Count > 0)
        {
            _indiceBarcoActual = 0;
            _barcoSeleccionado = barcos[0];
        }

        RefrescarUI();
    }

    /// <summary>
    /// Actualiza todos los textos con los datos del barco seleccionado.
    /// Si la flota está vacía, muestra el estado vacío y desactiva los botones.
    /// </summary>
    public void RefrescarUI()
    {
        if (GameManager.Instance == null ||
            _barcoSeleccionado == null ||
            GameManager.Instance.FlotaJugador.Barcos.Count == 0)
        {
            if (_textoNombreBarco != null) _textoNombreBarco.text = "Sin barcos en la flota";
            if (_txtIndiceBarco   != null) _txtIndiceBarco.text   = "0 / 0";
            if (_textoCasco       != null) _textoCasco.text       = "";
            if (_textoVida        != null) _textoVida.text        = "";
            if (_textoVelocidad   != null) _textoVelocidad.text   = "";
            if (_textoManiobra    != null) _textoManiobra.text    = "";
            if (_textoCarga       != null) _textoCarga.text       = "";
            if (_textoFuerza      != null) _textoFuerza.text      = "";
            if (_textoTripulacion != null) _textoTripulacion.text = "";
            if (_textoModulos     != null) _textoModulos.text     = "";
            if (_textoCapitan     != null) _textoCapitan.text     = "";
            if (_textoConvoy      != null) _textoConvoy.text      = "";
            if (_btnVerBodega     != null) _btnVerBodega.interactable  = false;
            if (_btnAnterior      != null) _btnAnterior.interactable   = false;
            if (_btnSiguiente     != null) _btnSiguiente.interactable  = false;
            return;
        }

        var barcos = GameManager.Instance.FlotaJugador.Barcos;

        // Sincronizar índice con el barco seleccionado
        for (int i = 0; i < barcos.Count; i++)
            if (barcos[i] == _barcoSeleccionado) { _indiceBarcoActual = i; break; }

        // Flechas de navegación: desactivar con un solo barco
        bool variosBarcos = barcos.Count > 1;
        if (_btnAnterior  != null) _btnAnterior.interactable  = variosBarcos;
        if (_btnSiguiente != null) _btnSiguiente.interactable = variosBarcos;

        // Índice textual
        if (_txtIndiceBarco != null)
            _txtIndiceBarco.text = $"Barco {_indiceBarcoActual + 1} / {barcos.Count}";

        BarcoJugador b = _barcoSeleccionado;

        if (_textoNombreBarco != null) _textoNombreBarco.text = b.Nombre;
        if (_textoCasco       != null) _textoCasco.text       = b.CascoBase.NombreCasco;
        if (_textoVida        != null) _textoVida.text        = $"Vida: {b.VidaActual}/{b.VidaTotal}";
        if (_textoVelocidad   != null) _textoVelocidad.text   = $"Velocidad: {b.VelocidadTotal}";
        if (_textoManiobra    != null) _textoManiobra.text    = $"Maniobrabilidad: {b.ManiobrabilidadTotal}";
        if (_textoCarga       != null) _textoCarga.text       = $"Carga máxima: {b.CargaMaximaTotal}";
        if (_textoFuerza      != null) _textoFuerza.text      = $"Fuerza de combate: {b.FuerzaCombateTotal}";
        if (_textoTripulacion != null) _textoTripulacion.text = $"Tripulación: {b.Tripulacion}/{b.CascoBase.CapacidadTripulacion}";

        // Módulos instalados
        if (_textoModulos != null)
        {
            if (b.ModulosInstalados.Count == 0)
            {
                _textoModulos.text = "Módulos: Vacío";
            }
            else
            {
                var sb = new System.Text.StringBuilder("Módulos: ");
                for (int i = 0; i < b.ModulosInstalados.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(b.ModulosInstalados[i].nombreModulo);
                }
                _textoModulos.text = sb.ToString();
            }
        }

        // Capitán
        if (_textoCapitan != null)
        {
            CapitanData cap = TabernaManager.Instance?.GetCapitanDeBarco(b.IdBarco);
            _textoCapitan.text = cap != null
                ? $"Capitán: {cap.Nombre}  Nav:{cap.HabilidadNavegacion:F1}  Com:{cap.HabilidadCombate:F1}"
                : "Sin capitán";
        }

        // Convoy (solo texto informativo, sin botones de acción)
        if (_textoConvoy != null)
        {
            ConvoyData convoy = ConvoyManager.Instance?.GetConvoyDeBarco(b);
            _textoConvoy.text = convoy != null ? $"Convoy: {convoy.NombreConvoy}" : "Sin convoy";
        }

        // Toggle Modo Pirata — sin disparar el listener
        if (_toggleModoPirata != null)
            _toggleModoPirata.SetIsOnWithoutNotify(GameManager.Instance.FlotaJugador.ModoPirata);

        if (_btnVerBodega != null) _btnVerBodega.interactable = true;
    }

    // ─── Navegación entre barcos ──────────────────────────────────────────────

    private void CiclarBarco(int dir)
    {
        var barcos = GameManager.Instance?.FlotaJugador?.Barcos;
        if (barcos == null || barcos.Count == 0) return;

        // Wrap circular
        _indiceBarcoActual = ((_indiceBarcoActual + dir) % barcos.Count + barcos.Count) % barcos.Count;
        _barcoSeleccionado = barcos[_indiceBarcoActual];
        RefrescarUI();
    }

    // ─── Bodega del jugador ───────────────────────────────────────────────────

    private void OnVerBodega()
    {
        if (_panelInfoBarco != null) _panelInfoBarco.SetActive(false);
        if (_panelBodega    != null) _panelBodega.SetActive(true);
        MostrarBodega();
    }

    /// <summary>
    /// Puebla el subpanel de bodega con una fila por bien almacenado.
    /// Lee directamente <see cref="GameManager.GetAlmacen"/> — no itera barcos individuales.
    /// Muestra "Bodega vacía" si no hay mercancía con cantidad mayor que cero.
    /// </summary>
    private void MostrarBodega()
    {
        if (_contenedorBodega == null || GameManager.Instance == null) return;

        // Limpiar filas previas
        for (int i = _contenedorBodega.childCount - 1; i >= 0; i--)
            Destroy(_contenedorBodega.GetChild(i).gameObject);

        IReadOnlyDictionary<BienData, int> almacen = GameManager.Instance.GetAlmacen();
        bool hayCarga = false;

        foreach (KeyValuePair<BienData, int> par in almacen)
        {
            if (par.Value <= 0) continue;
            hayCarga = true;

            // Fila: nombre izquierda — cantidad derecha
            GameObject fila = new GameObject("FilaBodega", typeof(RectTransform));
            fila.transform.SetParent(_contenedorBodega, false);

            HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4f;

            LayoutElement leRow = fila.AddComponent<LayoutElement>();
            leRow.preferredHeight = 22f;
            leRow.flexibleWidth   = 1f;

            // Nombre del bien
            GameObject txtNombreGO = new GameObject("TxtNombre", typeof(RectTransform));
            txtNombreGO.transform.SetParent(fila.transform, false);
            TextMeshProUGUI txtNombre = txtNombreGO.AddComponent<TextMeshProUGUI>();
            txtNombre.text      = par.Key.nombre;
            txtNombre.fontSize  = 12;
            txtNombre.color     = Color.white;
            txtNombre.alignment = TextAlignmentOptions.MidlineLeft;
            txtNombreGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Cantidad
            GameObject txtCantGO = new GameObject("TxtCantidad", typeof(RectTransform));
            txtCantGO.transform.SetParent(fila.transform, false);
            TextMeshProUGUI txtCant = txtCantGO.AddComponent<TextMeshProUGUI>();
            txtCant.text      = par.Value.ToString("N0");
            txtCant.fontSize  = 12;
            txtCant.color     = Color.white;
            txtCant.alignment = TextAlignmentOptions.MidlineRight;
            txtCantGO.AddComponent<LayoutElement>().preferredWidth = 60f;
        }

        if (!hayCarga)
        {
            GameObject txtVacioGO = new GameObject("TxtVacio", typeof(RectTransform));
            txtVacioGO.transform.SetParent(_contenedorBodega, false);
            TextMeshProUGUI txtVacio = txtVacioGO.AddComponent<TextMeshProUGUI>();
            txtVacio.text      = "Bodega vacía";
            txtVacio.fontSize  = 13;
            txtVacio.color     = new Color(0.7f, 0.7f, 0.7f, 1f);
            txtVacio.alignment = TextAlignmentOptions.Center;
            LayoutElement le   = txtVacioGO.AddComponent<LayoutElement>();
            le.preferredHeight = 30f;
            le.flexibleWidth   = 1f;
        }
    }

    private void OnVolverDesdeBodega()
    {
        if (_panelBodega    != null) _panelBodega.SetActive(false);
        if (_panelInfoBarco != null) _panelInfoBarco.SetActive(true);
        RefrescarUI();
    }

    // ─── Toggle Modo Pirata ───────────────────────────────────────────────────

    private void OnToggleModoPirata(bool valor)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.FlotaJugador.ModoPirata = valor;
        Debug.Log($"[PanelFlotaUI] Modo Pirata: {valor}");
        RefrescarUI();
    }
}
