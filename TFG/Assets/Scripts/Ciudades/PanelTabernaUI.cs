using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de UI de la taberna que se muestra sobre la escena Ciudad como un diálogo modal.
/// En la beta no ofrece funcionalidad; su implementación completa está prevista para la
/// versión release, donde permitirá contratar capitanes y tripulación.
/// </summary>
public class PanelTabernaUI : MonoBehaviour
{
    // ─── Botón de cierre ─────────────────────────────────────────────────────

    /// <summary>
    /// Botón que cierra el panel de la taberna y devuelve la vista a la ciudad.
    /// Debe asignarse desde el Inspector.
    /// </summary>
    public Button BotonCerrar;

    // ─── Referencias cacheadas ───────────────────────────────────────────────

    private CiudadController _ciudadController;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _ciudadController = FindAnyObjectByType<CiudadController>();

        if (_ciudadController == null)
            Debug.LogWarning("[PanelTabernaUI] No se encontró CiudadController en la escena.");

        if (BotonCerrar != null)
            BotonCerrar.onClick.AddListener(Cerrar);
        else
            Debug.LogWarning("[PanelTabernaUI] BotonCerrar no asignado en el Inspector.");
    }

    private void OnEnable()
    {
        Debug.Log("[Taberna] Disponible en la versión release.");
    }

    // ─── Control del panel ───────────────────────────────────────────────────

    /// <summary>
    /// Cierra el panel de la taberna delegando en <see cref="CiudadController.CerrarTodosPaneles"/>.
    /// Si la referencia al controlador es <c>null</c>, desactiva el panel directamente como fallback.
    /// Se registra como listener de <see cref="BotonCerrar"/> en <c>Awake</c>.
    /// </summary>
    public void Cerrar()
    {
        if (_ciudadController != null)
            _ciudadController.CerrarTodosPaneles();
        else
            gameObject.SetActive(false);
    }
}
