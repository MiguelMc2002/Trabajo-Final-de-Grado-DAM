using UnityEngine;

/// <summary>
/// Registro central de la partida. Conserva el estado del comerciante
/// —tesoro, puerto actual y capacidad de bodega— mientras el jugador
/// navega entre las distintas pantallas del juego.
/// En la beta los datos viven en memoria durante la sesión; en la release
/// se persistirán en la base de datos SQLite de la partida guardada.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    /// <summary>
    /// Punto de acceso global al estado de la partida activa.
    /// Permite a cualquier pantalla del juego consultar el tesoro
    /// o el puerto en el que se encuentra el jugador.
    /// </summary>
    public static GameManager Instance { get; private set; }

    // ─── Estado del jugador ──────────────────────────────────────────────────

    /// <summary>
    /// Monedas de oro en el cofre del comerciante.
    /// Sube al vender mercancía y baja al comprar en cualquier mercado de la Liga.
    /// </summary>
    public long Dinero { get; private set; }

    /// <summary>
    /// Puerto en el que está atracado el jugador en este momento.
    /// Vacío mientras el jugador navega por el mapamundi entre ciudades.
    /// </summary>
    public string CiudadActual { get; private set; }

    // ─── Constantes de beta ──────────────────────────────────────────────────

    /// <summary>Caudal inicial de la beta: suficiente para explorar la reacción de precios sin restricciones económicas.</summary>
    private const long DineroBeta = 999_999_999L;

    /// <summary>
    /// Capacidad de bodega durante la beta: sin límite, para centrar las pruebas
    /// en la mecánica de precios sin preocuparse por el espacio de carga.
    /// En la release se sustituirá por la capacidad real del barco.
    /// </summary>
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
    /// Registra un movimiento de dinero en el cofre del comerciante.
    /// Usar valor positivo al cobrar por una venta y negativo al pagar una compra.
    /// Si el tesoro no cubre el gasto, la operación no se realiza.
    /// </summary>
    /// <param name="cantidad">Monedas a ingresar (positivo) o gastar (negativo).</param>
    /// <returns><c>true</c> si el pago o cobro se realizó; <c>false</c> si el tesoro es insuficiente.</returns>
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
    /// Indica al juego en qué puerto ha atracado el jugador.
    /// Se actualiza automáticamente cada vez que la flota llega a una nueva ciudad.
    /// </summary>
    /// <param name="nombreCiudad">Nombre del puerto de destino (p.ej. "Lübeck").</param>
    public void SetCiudadActual(string nombreCiudad)
    {
        CiudadActual = nombreCiudad;
        Debug.Log($"[GameManager] Ciudad actual: {CiudadActual}");
    }
}
