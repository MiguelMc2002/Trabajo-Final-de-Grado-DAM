using UnityEngine;

/// <summary>
/// Gestiona el menú de pausa del juego.
/// Añadir este componente a un GameObject persistente en cada escena jugable
/// y asignar el panel de pausa en el Inspector.
/// La tecla Escape alterna la visibilidad del panel y congela/reanuda el tiempo.
/// </summary>
public class MenuPausa : MonoBehaviour
{
    // ─── Referencias UI ───────────────────────────────────────────────────────

    /// <summary>Panel de UI que contiene los botones del menú de pausa.</summary>
    [SerializeField] private GameObject _panelPausa;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_panelPausa == null)
        {
            Debug.LogWarning("[MenuPausa] _panelPausa no asignado en el Inspector.");
            return;
        }

        // Asegurar que el panel empiece oculto al cargar la escena
        _panelPausa.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_panelPausa == null) return;

        if (_panelPausa.activeSelf)
            Continuar();
        else
            Pausar();
    }

    // ─── Lógica de pausa ──────────────────────────────────────────────────────

    private void Pausar()
    {
        _panelPausa.SetActive(true);
        Time.timeScale = 0f;
    }

    // ─── API pública para botones del panel ───────────────────────────────────

    /// <summary>
    /// Oculta el panel de pausa y reanuda el tiempo del juego.
    /// Asignar al botón "Continuar" del panel de pausa.
    /// </summary>
    public void Continuar()
    {
        if (_panelPausa == null) return;

        _panelPausa.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Reanuda el tiempo y carga el Menú Principal, abandonando la partida en curso.
    /// Asignar al botón "Menú Principal" del panel de pausa.
    /// </summary>
    public void IrAMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneController.IrAMenuPrincipal();
    }

    /// <summary>
    /// Reanuda el tiempo y cierra la aplicación.
    /// Asignar al botón "Salir" del panel de pausa.
    /// En el editor detiene el modo Play en lugar de cerrar.
    /// </summary>
    public void SalirAlEscritorio()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
