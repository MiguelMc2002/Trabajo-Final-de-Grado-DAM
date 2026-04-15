using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona todos los cambios de pantalla del juego.
/// Desde aquí se ordena pasar del mapa al mercado, atracar en una ciudad
/// o volver al menú principal, manteniendo el flujo de juego coherente.
/// </summary>
/// <remarks>
/// Flujo de pantallas (beta):
/// Menú Principal → Mapamundi → Ciudad → Mercado → (vuelta al mapa)
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
    /// Lleva al jugador de vuelta al Menú Principal, abandonando la partida en curso.
    /// Se usa tanto al iniciar una nueva partida como al salir desde la pantalla de pausa.
    /// </summary>
    public static void IrAMenuPrincipal()
    {
        Debug.Log("[SceneController] Navegando a: MenuPrincipal");
        SceneManager.LoadScene(EscenaMenuPrincipal);
    }

    /// <summary>
    /// Muestra el mapamundi para que el jugador elija su próximo destino.
    /// Se invoca al salir de una ciudad o al zarpar desde el puerto.
    /// </summary>
    public static void IrAMapamundi()
    {
        Debug.Log("[SceneController] Navegando a: Mapamundi");
        SceneManager.LoadScene(EscenaMapamundi);
    }

    /// <summary>
    /// Atraca la flota en el puerto indicado y abre la pantalla de ciudad,
    /// desde donde el jugador puede acceder al mercado, el astillero o la taberna.
    /// </summary>
    /// <param name="nombreCiudad">Nombre del puerto de destino (p.ej. "Lübeck").</param>
    public static void IrACiudad(string nombreCiudad)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetCiudadActual(nombreCiudad);

        Debug.Log($"[SceneController] Navegando a Ciudad: {nombreCiudad}");
        SceneManager.LoadScene(EscenaCiudad);
    }

    /// <summary>
    /// Abre el mercado del puerto en el que está atracado el jugador,
    /// donde podrá comprar y vender mercancías.
    /// Solo es válido si el jugador se encuentra dentro de una ciudad.
    /// </summary>
    public static void IrAMercado()
    {
        Debug.Log($"[SceneController] Navegando a Mercado ({GameManager.Instance?.CiudadActual})");
        SceneManager.LoadScene(EscenaMercado);
    }

    // ─── Utilidad ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Reinicia la pantalla actual a su estado inicial.
    /// Permite repetir una situación de juego desde cero sin abandonar la partida.
    /// </summary>
    public static void RecargarEscenaActual()
    {
        string escenaActual = SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneController] Recargando escena: {escenaActual}");
        SceneManager.LoadScene(escenaActual);
    }

    /// <summary>
    /// Detiene o reanuda el tiempo del juego, congelando o reactivando
    /// el movimiento de flotas, la producción y la simulación de mercado.
    /// </summary>
    /// <param name="pausado"><c>true</c> para detener el tiempo; <c>false</c> para reanudarlo.</param>
    public static void SetPausa(bool pausado)
    {
        Time.timeScale = pausado ? 0f : 1f;
        Debug.Log($"[SceneController] Juego {(pausado ? "pausado" : "reanudado")}");
    }

    /// <summary>
    /// Alterna entre pausa y juego activo con una sola llamada.
    /// Útil para el botón de pausa del HUD o la tecla de teclado asignada.
    /// </summary>
    public static void TogglePausa()
    {
        SetPausa(Time.timeScale > 0f);
    }
}
