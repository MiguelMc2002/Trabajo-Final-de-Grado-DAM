using UnityEngine;

/// <summary>
/// Componente adjunto a los botones de ciudad en el menú principal.
/// Al pulsarlo, establece la ciudad seleccionada y navega a la pantalla de ciudad.
/// </summary>
public class SeleccionCiudadUI : MonoBehaviour
{
    /// <summary>Ciudad asociada a este botón, asignable desde el Inspector.</summary>
    public CiudadData datosCiudad;

    /// <summary>
    /// Abre la base de datos de la partida en curso, inicializa los mercados y
    /// navega a la pantalla de ciudad seleccionada.
    /// Llamado por el evento OnClick del botón de ciudad en el menú principal.
    /// </summary>
    public void SeleccionarCiudad()
    {
        if (datosCiudad == null)
        {
            Debug.LogWarning("[SeleccionCiudadUI] datosCiudad no asignado en el Inspector.");
            return;
        }

        // Slot 0: base de datos temporal de la partida en curso.
        // Si el jugador guarda en un slot concreto (1-5), SaveManager
        // llamará a InicializarSlot con el número correcto y lo sobreescribirá.
        DatabaseManager.Instance.InicializarSlot(0);

        if (GameManager.Instance != null && GameManager.Instance.CiudadesDisponibles != null)
            GameManager.Instance.InicializarMercadosDesdeAssets(GameManager.Instance.CiudadesDisponibles);

        GameManager.Instance.EstablecerCiudadActual(datosCiudad);
        SceneController.IrACiudad();
    }
}
