using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro central de la partida. Conserva el estado del comerciante
/// —tesoro, puerto actual, capacidad de bodega e inventario de mercancías—
/// mientras el jugador navega entre las distintas pantallas del juego.
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
    /// <c>null</c> mientras el jugador navega por el mapamundi entre ciudades.
    /// </summary>
    public CiudadData CiudadActual { get; private set; }

    /// <summary>
    /// Puerto en el que estuvo atracado el jugador antes del destino actual.
    /// Útil para ofrecer la opción de volver al puerto de origen tras un viaje.
    /// <c>null</c> si el jugador no ha visitado ninguna ciudad todavía.
    /// </summary>
    public CiudadData UltimaCiudad { get; private set; }

    /// <summary>
    /// Inventario de mercancías en la bodega del jugador.
    /// La clave es el <see cref="BienData"/> del bien; el valor, las unidades almacenadas.
    /// En la beta la capacidad es ilimitada (<see cref="CapacidadAlmacen"/> = <c>int.MaxValue</c>).
    /// </summary>
    private readonly Dictionary<BienData, int> _almacen = new Dictionary<BienData, int>();

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
        Debug.Log($"[GameManager] Inicializado como singleton persistente. Dinero: {Dinero:N0}");
    }

    /// <summary>
    /// Inicializa el estado de partida a los valores por defecto de la beta.
    /// </summary>
    private void InicializarEstado()
    {
        Dinero = DineroBeta;
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
    /// Registra el puerto en el que ha atracado el jugador.
    /// Invocado desde el mapamundi al hacer clic en un marcador de ciudad.
    /// </summary>
    /// <param name="ciudad"><see cref="CiudadData"/> del puerto al que llega el jugador.</param>
    public void EstablecerCiudadActual(CiudadData ciudad)
    {
        UltimaCiudad = CiudadActual;
        CiudadActual = ciudad;
        Debug.Log($"[GameManager] Ciudad actual: {ciudad.NombreCiudad}");
    }

    // ─── Almacén del jugador ─────────────────────────────────────────────────

    /// <summary>
    /// Devuelve las unidades del bien indicado que hay en la bodega del jugador.
    /// Si el bien no está en el inventario, retorna 0.
    /// </summary>
    /// <param name="bien">Bien cuya cantidad se quiere consultar.</param>
    /// <returns>Unidades disponibles en bodega (0 o más).</returns>
    public int GetCantidadBien(BienData bien)
    {
        return _almacen.TryGetValue(bien, out int cantidad) ? cantidad : 0;
    }

    /// <summary>
    /// Modifica la cantidad de un bien en la bodega del jugador.
    /// Usar valor positivo al cargar mercancía (compra) y negativo al descargarla (venta).
    /// La operación se rechaza si el resultado sería negativo o superaría <see cref="CapacidadAlmacen"/>.
    /// </summary>
    /// <param name="bien">Bien cuya cantidad se modifica.</param>
    /// <param name="cantidad">Unidades a añadir (positivo) o retirar (negativo).</param>
    /// <returns><c>true</c> si la operación se realizó; <c>false</c> si no hay suficiente stock o capacidad.</returns>
    public bool ModificarCantidadBien(BienData bien, int cantidad)
    {
        int actual = GetCantidadBien(bien);
        int nuevo = actual + cantidad;

        if (nuevo < 0)
        {
            Debug.LogWarning($"[GameManager] Stock insuficiente de '{bien.nombre}' para retirar {-cantidad} unidades (disponible: {actual}).");
            return false;
        }

        // Comprobación de capacidad total de bodega (para la release; en beta es int.MaxValue)
        int totalActual = GetTotalUnidadesAlmacen();
        if (cantidad > 0 && totalActual + cantidad > CapacidadAlmacen)
        {
            Debug.LogWarning($"[GameManager] Bodega llena. No se pueden cargar {cantidad} unidades de '{bien.nombre}'.");
            return false;
        }

        if (nuevo == 0)
            _almacen.Remove(bien);
        else
            _almacen[bien] = nuevo;

        Debug.Log($"[GameManager] Almacén '{bien.nombre}': {actual} → {nuevo}");
        return true;
    }

    /// <summary>
    /// Devuelve el total de unidades de todas las mercancías almacenadas en bodega.
    /// Se usa para comprobar si hay espacio disponible antes de cargar más mercancía.
    /// </summary>
    /// <returns>Suma de todas las unidades en bodega.</returns>
    public int GetTotalUnidadesAlmacen()
    {
        int total = 0;
        foreach (int cantidad in _almacen.Values)
            total += cantidad;
        return total;
    }

    /// <summary>
    /// Expone el inventario completo de bodega en modo de solo lectura.
    /// Útil para que la interfaz del almacén enumere todos los bienes cargados.
    /// </summary>
    /// <returns>Diccionario de solo lectura con cada bien y sus unidades.</returns>
    public IReadOnlyDictionary<BienData, int> GetAlmacen()
    {
        return _almacen;
    }
}
