using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador centralizado de navegación entre escenas.
/// Expone métodos estáticos para que cualquier botón de la UI pueda
/// desencadenar una transición sin acoplarse al resto del sistema.
/// </summary>
/// <remarks>
/// Flujo de escenas (beta):
/// MenuPrincipal → Mapamundi → Ciudad → Mercado → (vuelta al mapa)
/// Los nombres de escena deben coincidir exactamente con los de Build Settings.
/// </remarks>
public class SceneController : MonoBehaviour
{
    // ─── Nombres de escena ────────────────────────────────────────────────────
    // Centralizar los literales aquí evita typos repartidos por el proyecto.

    private const string EscenaMenuPrincipal = "MenuPrincipal";
    private const string EscenaMapamundi     = "Mapamundi";
    private const string EscenaCiudad        = "Ciudad";
    private const string EscenaMercado       = "Mercado";

    // ─── Navegación general ───────────────────────────────────────────────────

    /// <summary>
    /// Carga el Menú Principal y reinicia el estado del <see cref="GameManager"/>.
    /// Útil para el botón "Nueva partida" o "Volver al menú".
    /// </summary>
    public static void IrAMenuPrincipal()
    {
        Debug.Log("[SceneController] Navegando a: MenuPrincipal");
        SceneManager.LoadScene(EscenaMenuPrincipal);
    }

    /// <summary>
    /// Carga el Mapamundi. Punto de retorno tras visitar una ciudad.
    /// </summary>
    public static void IrAMapamundi()
    {
        Debug.Log("[SceneController] Navegando a: Mapamundi");
        SceneManager.LoadScene(EscenaMapamundi);
    }

    /// <summary>
    /// Carga la escena de Ciudad y registra en el <see cref="GameManager"/>
    /// cuál es la ciudad visitada.
    /// </summary>
    /// <param name="nombreCiudad">Nombre de la ciudad destino (p.ej. "Lübeck").</param>
    public static void IrACiudad(string nombreCiudad)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetCiudadActual(nombreCiudad);

        Debug.Log($"[SceneController] Navegando a Ciudad: {nombreCiudad}");
        SceneManager.LoadScene(EscenaCiudad);
    }

    /// <summary>
    /// Carga la escena de Mercado de la ciudad actual.
    /// Requiere que <see cref="GameManager.CiudadActual"/> esté establecido.
    /// </summary>
    public static void IrAMercado()
    {
        Debug.Log($"[SceneController] Navegando a Mercado ({GameManager.Instance?.CiudadActual})");
        SceneManager.LoadScene(EscenaMercado);
    }

    // ─── Utilidad ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Recarga la escena activa. Útil para reiniciar estados locales en tests.
    /// </summary>
    public static void RecargarEscenaActual()
    {
        string escenaActual = SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneController] Recargando escena: {escenaActual}");
        SceneManager.LoadScene(escenaActual);
    }

    /// <summary>
    /// Pausa o reanuda el juego modificando <see cref="Time.timeScale"/>.
    /// </summary>
    /// <param name="pausado"><c>true</c> para pausar; <c>false</c> para reanudar.</param>
    public static void SetPausa(bool pausado)
    {
        Time.timeScale = pausado ? 0f : 1f;
        Debug.Log($"[SceneController] Juego {(pausado ? "pausado" : "reanudado")}");
    }

    /// <summary>
    /// Alterna el estado de pausa respecto al valor actual de <see cref="Time.timeScale"/>.
    /// </summary>
    public static void TogglePausa()
    {
        SetPausa(Time.timeScale > 0f);
    }
}
