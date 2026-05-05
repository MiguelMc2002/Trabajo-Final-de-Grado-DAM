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
    /// Establece la ciudad actual en el GameManager y carga la escena de ciudad.
    /// Llamado por el evento OnClick del botón.
    /// </summary>
    public void SeleccionarCiudad()
    {
        if (datosCiudad == null)
        {
            Debug.LogWarning("[SeleccionCiudadUI] datosCiudad no asignado en el Inspector.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CiudadesDisponibles != null)
            GameManager.Instance.InicializarMercadosDesdeAssets(GameManager.Instance.CiudadesDisponibles);

        GameManager.Instance.EstablecerCiudadActual(datosCiudad);
        SceneController.IrACiudad();
    }
}
