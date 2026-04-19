using UnityEngine;

/// <summary>
/// Controla la lógica del menú principal: iniciar partida, cargar y salir.
/// Gestiona la visibilidad del panel de selección de ciudad de inicio.
/// </summary>
public class MenuPrincipalUI : MonoBehaviour
{
    /// <summary>Panel con los botones de ciudad para comenzar una nueva partida.</summary>
    public GameObject panelSeleccionCiudad;

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
    /// Reservado para la funcionalidad de carga de partida guardada (post-beta).
    /// Llamado por el botón "Cargar Partida".
    /// </summary>
    public void CargarPartida()
    {
        Debug.Log("[MenuPrincipalUI] Funcionalidad de carga pendiente post-beta");
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
