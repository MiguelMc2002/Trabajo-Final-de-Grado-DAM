using UnityEngine;

/// <summary>
/// Controla la lógica del menú principal: iniciar partida, cargar y salir.
/// Gestiona la visibilidad de los paneles excluyentes mediante un enum centralizado.
/// </summary>
public class MenuPrincipalUI : MonoBehaviour
{
    /// <summary>Panel con los botones de ciudad para comenzar una nueva partida.</summary>
    public GameObject panelSeleccionCiudad;

    [SerializeField] private PantallaSlotsUI _pantallaSlotsUI;
    [SerializeField] private GameObject _panelMenuPrincipal;

    /// <summary>Identifica cada panel excluyente del menú principal.</summary>
    private enum PanelMenuPrincipal
    {
        /// <summary>Panel con los botones Nueva Partida, Cargar Partida y Salir.</summary>
        MenuPrincipal,
        /// <summary>Panel de slots de guardado/carga en modo pantalla completa.</summary>
        PantallaSlots
    }

    private void Start()
    {
        if (panelSeleccionCiudad != null)
            panelSeleccionCiudad.SetActive(false);

        MostrarPanel(PanelMenuPrincipal.MenuPrincipal);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && panelSeleccionCiudad != null && panelSeleccionCiudad.activeSelf)
            CerrarPanelSeleccion();
    }

    /// <summary>
    /// Activa únicamente el panel indicado y desactiva el resto.
    /// Es null-safe: omite la operación si algún panel no está asignado en el Inspector.
    /// </summary>
    /// <param name="panel">Panel que debe quedar visible.</param>
    private void MostrarPanel(PanelMenuPrincipal panel)
    {
        if (_panelMenuPrincipal == null)
        {
            Debug.LogWarning("[MenuPrincipalUI] _panelMenuPrincipal no asignado en el Inspector.");
            return;
        }

        if (_pantallaSlotsUI == null)
        {
            Debug.LogWarning("[MenuPrincipalUI] _pantallaSlotsUI no asignado en el Inspector.");
            return;
        }

        switch (panel)
        {
            case PanelMenuPrincipal.MenuPrincipal:
                _panelMenuPrincipal.SetActive(true);
                _pantallaSlotsUI.gameObject.SetActive(false);
                break;
            case PanelMenuPrincipal.PantallaSlots:
                _panelMenuPrincipal.SetActive(false);
                _pantallaSlotsUI.gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Muestra el panel de selección de ciudad para comenzar una nueva partida.
    /// Llamado por el botón "Nueva Partida".
    /// </summary>
    public void IniciarNuevaPartida()
    {
        if (panelSeleccionCiudad == null)
        {
            Debug.LogWarning("[MenuPrincipalUI] panelSeleccionCiudad no asignado en el Inspector.");
            return;
        }

        panelSeleccionCiudad.SetActive(true);
        Debug.Log("[MenuPrincipalUI] Panel de selección activado");
    }

    /// <summary>
    /// Oculta el panel de selección de ciudad y vuelve al menú principal.
    /// Llamado por el botón "Atrás" del panel.
    /// </summary>
    public void CerrarPanelSeleccion()
    {
        if (panelSeleccionCiudad == null)
        {
            Debug.LogWarning("[MenuPrincipalUI] panelSeleccionCiudad no asignado en el Inspector.");
            return;
        }

        panelSeleccionCiudad.SetActive(false);
        Debug.Log("[MenuPrincipalUI] Panel de selección cerrado");
    }

    /// <summary>
    /// Abre el panel de slots en modo Cargar para que el jugador elija una partida guardada.
    /// Llamado por el botón "Cargar Partida".
    /// </summary>
    public void CargarPartida()
    {
        if (_pantallaSlotsUI == null)
        {
            Debug.LogWarning("[MenuPrincipalUI] _pantallaSlotsUI no asignado en el Inspector.");
            return;
        }

        MostrarPanel(PanelMenuPrincipal.PantallaSlots);
        _pantallaSlotsUI.Abrir(SlotModo.Cargar);
    }

    /// <summary>
    /// Llamado por PantallaSlotsUI al cerrar el panel, para volver a mostrar el menú principal.
    /// </summary>
    public void MostrarMenuPrincipal()
    {
        MostrarPanel(PanelMenuPrincipal.MenuPrincipal);
    }

    /// <summary>
    /// Cierra la aplicación.
    /// Llamado por el botón "Salir".
    /// </summary>
    public void Salir()
    {
        Debug.Log("[MenuPrincipalUI] Cerrando aplicación");
        Application.Quit();
    }
}
