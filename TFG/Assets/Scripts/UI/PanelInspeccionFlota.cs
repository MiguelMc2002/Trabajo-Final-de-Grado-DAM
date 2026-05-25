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

    private void Awake()
    {
        btnCerrar.onClick.AddListener(Ocultar);
    }

    public void Mostrar(FlotaRuntimeData flota)
    {
        foreach (Transform hijo in contenedorFilas)
            Destroy(hijo.gameObject);

        if (txtTituloFlota != null)
            txtTituloFlota.text = $"{flota.NombrePropietario} ({(flota.IsPirata ? "Pirata" : "Comerciante")})";

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

    public void Ocultar()
    {
        gameObject.SetActive(false);
    }
}
