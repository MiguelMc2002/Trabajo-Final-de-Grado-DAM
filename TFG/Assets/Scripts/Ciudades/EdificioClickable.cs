using UnityEngine;

/// <summary>
/// Hace que el sprite de un edificio responda al click del jugador.
/// Al pulsarlo notifica al <see cref="CiudadController"/> de la escena para que
/// abra el servicio correspondiente (mercado, astillero o taberna).
/// Añadir este componente a cada GameObject de edificio en la escena Ciudad.
/// IMPORTANTE: este GameObject necesita un Collider2D para que OnMouseDown funcione.
/// </summary>
public class EdificioClickable : MonoBehaviour
{
    // ─── Configuración ───────────────────────────────────────────────────────

    /// <summary>
    /// Tipo de edificio que representa este sprite.
    /// Determina qué acción ejecuta <see cref="CiudadController.AbrirEdificio"/> al pulsarlo.
    /// </summary>
    [SerializeField] private TipoEdificio _tipo;

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

    private void OnMouseDown()
    {
        if (_ciudadController == null)
        {
            Debug.LogWarning($"[EdificioClickable:{name}] CiudadController no disponible; no se puede abrir '{_tipo}'.");
            return;
        }

        _ciudadController.AbrirEdificio(_tipo);
    }
}
