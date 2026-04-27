using UnityEngine;

/// <summary>
/// Gestiona el menú de pausa del juego.
/// Añadir este componente a un GameObject persistente en cada escena jugable
/// y asignar el panel de pausa en el Inspector.
/// La tecla Escape alterna la visibilidad del panel. La gestión del tiempo
/// se delega en <see cref="SimulacionTiempo"/> (si existe en la escena) mediante
/// <see cref="SimulacionTiempo.PausarPorMenu"/> y <see cref="SimulacionTiempo.ReanudarDesdMenu"/>.
/// En escenas sin simulación (p. ej. Menú Principal) el panel abre y cierra sin tocar el tiempo.
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
        SimulacionTiempo.Instance?.PausarPorMenu();
    }

    // ─── API pública para botones del panel ───────────────────────────────────

    /// <summary>
    /// Oculta el panel de pausa y reanuda la simulación de tiempo.
    /// Asignar al botón "Continuar" del panel de pausa.
    /// </summary>
    public void Continuar()
    {
        if (_panelPausa == null) return;

        _panelPausa.SetActive(false);
        SimulacionTiempo.Instance?.ReanudarDesdMenu();
    }

    /// <summary>
    /// Reanuda la simulación y carga el Menú Principal, abandonando la partida en curso.
    /// Asignar al botón "Menú Principal" del panel de pausa.
    /// </summary>
    public void IrAMenuPrincipal()
    {
        SimulacionTiempo.Instance?.ReanudarDesdMenu();
        SceneController.IrAMenuPrincipal();
    }

    /// <summary>
    /// Reanuda la simulación y cierra la aplicación.
    /// Asignar al botón "Salir" del panel de pausa.
    /// En el editor detiene el modo Play en lugar de cerrar.
    /// </summary>
    public void SalirAlEscritorio()
    {
        SimulacionTiempo.Instance?.ReanudarDesdMenu();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
