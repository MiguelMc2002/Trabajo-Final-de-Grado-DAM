using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de UI que muestra el listado de barcos de una flota (jugador o PNJ).
/// Incluye columnas con nombre, casco, vida, velocidad, maniobrabilidad, carga y fuerza.
/// Para la flota del jugador expone además el botón de cambio de Modo Pirata.
/// </summary>
public class PanelInspeccionFlota : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtTituloFlota;
    [SerializeField] private Transform      contenedorFilas;
    [SerializeField] private GameObject     prefabFila;
    [SerializeField] private Button         btnCerrar;
    /// <summary>Botón toggle Modo Pirata. Solo visible al inspeccionar la flota del jugador.</summary>
    [SerializeField] private Button         _btnModoPirata;

    private bool _mostrandoJugador;

    private void Awake()
    {
        btnCerrar.onClick.AddListener(Ocultar);
        if (_btnModoPirata != null)
            _btnModoPirata.onClick.AddListener(OnToggleModoPirata);
    }

    /// <summary>
    /// Muestra el panel con los datos de la flota indicada.
    /// </summary>
    /// <param name="flota">Datos de la flota a inspeccionar.</param>
    /// <param name="esJugador">
    /// Si es <c>true</c> se muestra el botón Modo Pirata y el título indica "Tu flota".
    /// </param>
    public void Mostrar(FlotaRuntimeData flota, bool esJugador = false)
    {
        _mostrandoJugador = esJugador;

        foreach (Transform hijo in contenedorFilas)
            Destroy(hijo.gameObject);

        // Título diferenciado: "Tu flota" para el jugador, nombre+tipo para PNJ
        if (txtTituloFlota != null)
        {
            if (esJugador && GameManager.Instance != null)
                txtTituloFlota.text = $"Tu flota — {(GameManager.Instance.FlotaJugador.ModoPirata ? "Pirata" : "Comerciante")}";
            else
                txtTituloFlota.text = $"{flota.NombrePropietario} ({(flota.IsPirata ? "Pirata" : "Comerciante")})";
        }

        // Botón Modo Pirata: visible solo para el jugador
        if (_btnModoPirata != null)
        {
            _btnModoPirata.gameObject.SetActive(esJugador);
            if (esJugador) ActualizarTextoBtnPirata();
        }

        if (flota.BarcosFlota == null || flota.BarcosFlota.Count == 0)
        {
            GameObject filaVacia = Instantiate(prefabFila, contenedorFilas);
            HorizontalLayoutGroup hlgVacia = filaVacia.GetComponent<HorizontalLayoutGroup>();
            if (hlgVacia != null) { hlgVacia.childControlWidth = false; hlgVacia.childForceExpandWidth = false; }

            TextMeshProUGUI[] tv = filaVacia.GetComponentsInChildren<TextMeshProUGUI>();
            float[] anchos = { 100f, 60f, 65f, 45f, 45f, 50f, 45f };
            for (int i = 0; i < tv.Length && i < anchos.Length; i++)
            {
                tv[i].fontSize = 18f;
                tv[i].alignment = TextAlignmentOptions.MidlineLeft;
                tv[i].overflowMode = TextOverflowModes.Ellipsis;
                tv[i].enableWordWrapping = false;
                tv[i].margin = new Vector4(4f, 0f, 0f, 0f);
                LayoutElement le = tv[i].GetComponent<LayoutElement>() ?? tv[i].gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = anchos[i]; le.flexibleWidth = 0f;
            }

            if (!esJugador && tv.Length >= 7)
            {
                // Mostrar stats agregados actualizados de la flota PNJ (reflejan el daño del combate)
                tv[0].text = $"{flota.NumBarcos} barcos";
                tv[1].text = "-";
                tv[2].text = $"{flota.VidaActual:F0}/{flota.VidaMax:F0}";
                tv[3].text = $"{flota.VelocidadFlota:F1}";
                tv[4].text = $"{flota.ManiobrabilidadFlota:F1}";
                tv[5].text = "-";
                tv[6].text = $"{flota.FuerzaCanhones:F0}";
            }
            else if (tv.Length > 0)
            {
                tv[0].text = "Sin barcos en la flota";
            }

            gameObject.SetActive(true);
            return;
        }

        // Anchos fijos idénticos a las cabeceras: Nombre, Casco, Vida, Velocidad, Maniobra, Carga, Fuerza
        float[] anchosCeldas = { 100f, 60f, 65f, 45f, 45f, 50f, 45f };

        foreach (BarcoJugador barco in flota.BarcosFlota)
        {
            GameObject fila = Instantiate(prefabFila, contenedorFilas);

            // El HLG debe respetar LayoutElement; desactivar expansión automática
            HorizontalLayoutGroup hlg = fila.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childControlWidth     = false;
                hlg.childForceExpandWidth = false;
            }

            TextMeshProUGUI[] t = fila.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (t.Length >= 7)
            {
                t[0].text = barco.Nombre;
                t[1].text = barco.CascoBase.NombreCasco;
                t[2].text = $"{barco.VidaActual}/{barco.VidaTotal}";
                t[3].text = barco.VelocidadTotal.ToString();
                t[4].text = barco.ManiobrabilidadTotal.ToString();
                t[5].text = barco.CargaMaximaTotal.ToString();
                t[6].text = barco.FuerzaCombateTotal.ToString();

                // Alineación, desbordamiento, padding izquierdo y ancho fijo en cada celda
                for (int i = 0; i < 7; i++)
                {
                    t[i].fontSize           = 18f;
                    t[i].alignment          = TextAlignmentOptions.MidlineLeft;
                    t[i].overflowMode       = TextOverflowModes.Ellipsis;
                    t[i].enableWordWrapping = false;
                    t[i].margin             = new Vector4(4f, 0f, 0f, 0f);
                    LayoutElement le = t[i].GetComponent<LayoutElement>() ?? t[i].gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = anchosCeldas[i];
                    le.flexibleWidth  = 0f;
                }
            }
        }

        gameObject.SetActive(true);
    }

    /// <summary>Oculta el panel de inspección.</summary>
    public void Ocultar()
    {
        gameObject.SetActive(false);
    }

    private void OnToggleModoPirata()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.FlotaJugador.ModoPirata = !GameManager.Instance.FlotaJugador.ModoPirata;
        // Refrescar título y texto del botón tras el cambio
        if (txtTituloFlota != null)
            txtTituloFlota.text = $"Tu flota — {(GameManager.Instance.FlotaJugador.ModoPirata ? "Pirata" : "Comerciante")}";
        ActualizarTextoBtnPirata();
    }

    private void ActualizarTextoBtnPirata()
    {
        if (_btnModoPirata == null || GameManager.Instance == null) return;
        TextMeshProUGUI txt = _btnModoPirata.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = GameManager.Instance.FlotaJugador.ModoPirata ? "Abandonar Piratería" : "Hacerse Pirata";
    }

}
