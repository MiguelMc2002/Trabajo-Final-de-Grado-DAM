using UnityEngine;

/// <summary>
/// Gestor central del juego. Singleton persistente entre escenas que almacena
/// el estado global del jugador durante toda la partida (beta: sin SQLite).
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    /// <summary>Instancia única accesible desde cualquier script.</summary>
    public static GameManager Instance { get; private set; }

    // ─── Estado del jugador ──────────────────────────────────────────────────

    /// <summary>Dinero actual del jugador.</summary>
    public long Dinero { get; private set; }

    /// <summary>Nombre de la ciudad en la que se encuentra el jugador actualmente.</summary>
    public string CiudadActual { get; private set; }

    // ─── Constantes de beta ──────────────────────────────────────────────────

    /// <summary>Dinero inicial para la beta: ilimitado para probar reacción de precios.</summary>
    private const long DineroBeta = 999_999_999L;

    /// <summary>Capacidad de almacén en beta: ilimitada.</summary>
    public const int CapacidadAlmacen = int.MaxValue;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Patrón singleton: destruir duplicados y persistir entre escenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InicializarEstado();
    }

    /// <summary>
    /// Inicializa el estado de partida a los valores por defecto de la beta.
    /// </summary>
    private void InicializarEstado()
    {
        Dinero = DineroBeta;
        CiudadActual = string.Empty;
        Debug.Log($"[GameManager] Partida iniciada. Dinero: {Dinero:N0}");
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Añade o resta dinero al saldo del jugador.
    /// Las compras pasan un valor negativo; las ventas, positivo.
    /// </summary>
    /// <param name="cantidad">Cantidad a sumar (positiva) o restar (negativa).</param>
    /// <returns><c>true</c> si la operación se completó; <c>false</c> si no hay saldo suficiente.</returns>
    public bool ModificarDinero(long cantidad)
    {
        if (Dinero + cantidad < 0)
        {
            Debug.LogWarning("[GameManager] Saldo insuficiente para realizar la operación.");
            return false;
        }

        Dinero += cantidad;
        Debug.Log($"[GameManager] Dinero actualizado: {Dinero:N0} ({(cantidad >= 0 ? "+" : string.Empty)}{cantidad:N0})");
        return true;
    }

    /// <summary>
    /// Registra la ciudad en la que se encuentra el jugador.
    /// Lo llama <see cref="SceneController"/> al hacer la transición al mapa de ciudad.
    /// </summary>
    /// <param name="nombreCiudad">Nombre exacto de la ciudad destino.</param>
    public void SetCiudadActual(string nombreCiudad)
    {
        CiudadActual = nombreCiudad;
        Debug.Log($"[GameManager] Ciudad actual: {CiudadActual}");
    }
}
