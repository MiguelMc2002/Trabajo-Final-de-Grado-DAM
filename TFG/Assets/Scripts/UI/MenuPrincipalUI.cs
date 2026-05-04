using UnityEngine;

/// <summary>
/// Controla la lógica del menú principal: iniciar partida, cargar y salir.
/// Gestiona la visibilidad del panel de selección de ciudad de inicio.
/// </summary>
public class MenuPrincipalUI : MonoBehaviour
{
    /// <summary>Panel con los botones de ciudad para comenzar una nueva partida.</summary>
    public GameObject panelSeleccionCiudad;

    [SerializeField] private PantallaSlotsUI _pantallaSlotsUI;
    [SerializeField] private GameObject _panelMenuPrincipal;

    private void Start()
    {
        // Ocultar el panel de selección hasta que el jugador pulse "Nueva Partida"
        if (panelSeleccionCiudad != null)
            panelSeleccionCiudad.SetActive(false);
    }

    /// <summary>
    /// Muestra el panel de selección de ciudad para comenzar una nueva partida.
    /// Llamado por el botón "Nueva Partida".
    /// </summary>
    private void Update()
    {
        // Cerrar el panel de selección al pulsar Escape, igual que el botón "Atrás"
        if (Input.GetKeyDown(KeyCode.Escape) && panelSeleccionCiudad != null && panelSeleccionCiudad.activeSelf)
            CerrarPanelSeleccion();
    }

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

        _pantallaSlotsUI.Abrir(SlotModo.Cargar);
    }

    /// <summary>
    /// Llamado por PantallaSlotsUI al cerrar el panel, para volver a mostrar el menú principal.
    /// </summary>
    public void MostrarMenuPrincipal()
    {
        if (_pantallaSlotsUI != null)
            _pantallaSlotsUI.gameObject.SetActive(false);

        if (_panelMenuPrincipal != null)
            _panelMenuPrincipal.SetActive(true);
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
