using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            TextMeshProUGUI[] tv = filaVacia.GetComponentsInChildren<TextMeshProUGUI>();
            if (tv.Length > 0) tv[0].text = "Sin datos de barcos";
            gameObject.SetActive(true);
            return;
        }

        foreach (BarcoJugador barco in flota.BarcosFlota)
        {
            GameObject fila = Instantiate(prefabFila, contenedorFilas);
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
