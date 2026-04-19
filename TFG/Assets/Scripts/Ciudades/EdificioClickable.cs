using UnityEngine;

/// <summary>
/// Componente de botón UI para edificios en la escena Ciudad.
/// Conectar el evento OnClick() del Button de Unity UI a <see cref="OnClick"/>.
/// Al pulsarlo notifica al <see cref="CiudadController"/> de la escena para que
/// abra el servicio correspondiente (mercado, astillero o taberna).
/// </summary>
public class EdificioClickable : MonoBehaviour
{
    // ─── Configuración ───────────────────────────────────────────────────────

    /// <summary>
    /// Tipo de edificio que representa este botón.
    /// Determina qué acción ejecuta <see cref="CiudadController.AbrirEdificio"/> al pulsarlo.
    /// </summary>
    public TipoEdificio Tipo;

    // ─── Referencias ─────────────────────────────────────────────────────────

    /// <summary>Referencia cacheada al coordinador de la escena Ciudad.</summary>
    private CiudadController _ciudadController;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _ciudadController = FindAnyObjectByType<CiudadController>();

        if (_ciudadController == null)
            Debug.LogWarning($"[EdificioClickable:{name}] No se encontró CiudadController en la escena.");
    }

    // ─── Interacción ─────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el panel del edificio correspondiente a <see cref="Tipo"/>.
    /// Asignar este método al evento OnClick() del componente Button en el Inspector.
    /// </summary>
    public void OnClick()
    {
        if (_ciudadController == null)
        {
            Debug.LogWarning($"[EdificioClickable:{name}] CiudadController no disponible; no se puede abrir '{Tipo}'.");
            return;
        }

        _ciudadController.AbrirEdificio(Tipo);
    }
}
