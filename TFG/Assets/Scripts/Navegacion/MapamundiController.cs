using UnityEngine;

/// <summary>
/// Controla el mapamundi: inicializa los marcadores de ciudad visibles en el mapa
/// y gestiona la navegación del jugador hacia un puerto o al menú principal.
/// En la beta el viaje es inmediato; en la release se animará la flota sobre el mapa.
/// </summary>
public class MapamundiController : MonoBehaviour
{
    // ─── Ciudades del mapa ────────────────────────────────────────────────────

    /// <summary>
    /// Marcadores de las ciudades que aparecen en el mapamundi.
    /// Asignar desde el Inspector: uno por cada puerto navegable de la beta.
    /// </summary>
    public MarcadorCiudad[] Ciudades;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        foreach (MarcadorCiudad marcador in Ciudades)
            marcador.Inicializar(this);
    }

    // ─── Input por teclado ────────────────────────────────────────────────────

    /// <summary>
    /// Detecta atajos de teclado del mapamundi.
    /// M → viaja directamente a la última ciudad visitada, si existe.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (GameManager.Instance != null && GameManager.Instance.UltimaCiudad != null)
                ViajarACiudad(GameManager.Instance.UltimaCiudad);
        }
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registra la ciudad de destino en el estado de la partida y carga
    /// la pantalla de ciudad para que el jugador interactúe con el puerto.
    /// En la beta el traslado es instantáneo; en la release incluirá animación.
    /// </summary>
    /// <param name="ciudadDestino">Datos del puerto al que viaja el jugador.</param>
    public void ViajarACiudad(CiudadData ciudadDestino)
    {
        if (ciudadDestino == null)
        {
            Debug.LogError("[MapamundiController] ViajarACiudad recibió un CiudadData nulo.");
            return;
        }

        Debug.Log($"[MapamundiController] Viajando a {ciudadDestino.NombreCiudad}...");
        GameManager.Instance.EstablecerCiudadActual(ciudadDestino);
        SceneController.IrACiudad();
    }

    /// <summary>
    /// Abandona la partida en curso y regresa al menú principal.
    /// </summary>
    public void IrAMenuPrincipal()
    {
        SceneController.IrAMenuPrincipal();
    }
}
