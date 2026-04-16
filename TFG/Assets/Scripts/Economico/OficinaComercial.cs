using UnityEngine;

/// <summary>
/// Intermediario exclusivo entre la interfaz del mercado y los sistemas de economía
/// (<see cref="GameManager"/>, <see cref="MarketManager"/>).
/// Centraliza la validación de operaciones de compra y venta, y expone el resultado
/// de la última operación mediante <see cref="UltimoMensaje"/> para que la UI lo muestre
/// al jugador sin necesidad de leer directamente el estado interno del mercado.
/// </summary>
/// <remarks>
/// Debe inicializarse con <see cref="Inicializar"/> antes de llamar a
/// <see cref="Comprar"/> o <see cref="Vender"/>. Si <see cref="GameManager.Instance"/>
/// es <c>null</c> al operar (p. ej. al probar la escena Mercado directamente sin pasar
/// por el Menú Principal), la operación se cancela con un aviso en el log en lugar de
/// lanzar una excepción.
/// </remarks>
public class OficinaComercial : MonoBehaviour
{
    // ─── Estado interno ──────────────────────────────────────────────────────

    /// <summary>Referencia al mercado de la ciudad activa, asignada en <see cref="Inicializar"/>.</summary>
    private MarketManager _mercado;

    /// <summary>Indica si la oficina ha sido inicializada y está lista para operar.</summary>
    private bool _inicializada;

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Descripción textual del resultado de la última operación realizada.
    /// La UI del mercado puede leer esta propiedad tras cada llamada a
    /// <see cref="Comprar"/> o <see cref="Vender"/> para informar al jugador.
    /// </summary>
    public string UltimoMensaje { get; private set; } = string.Empty;

    /// <summary>
    /// Vincula esta oficina comercial al mercado de una ciudad concreta.
    /// Debe llamarse una vez antes de cualquier operación de compra o venta.
    /// </summary>
    /// <param name="mercado">Gestor del mercado de la ciudad en la que opera el jugador.</param>
    public void Inicializar(MarketManager mercado)
    {
        if (mercado == null)
        {
            Debug.LogWarning("[OficinaComercial] Se intentó inicializar con un MarketManager nulo.");
            _inicializada = false;
            return;
        }

        _mercado = mercado;
        _inicializada = true;
        UltimoMensaje = string.Empty;
        Debug.Log($"[OficinaComercial] Inicializada para el mercado de {_mercado.GetNombreCiudad()}.");
    }

    // ─── Compra ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Ejecuta la compra de un bien en el mercado de la ciudad activa.
    /// Comprueba que la ciudad disponga de stock suficiente y que el tesoro del
    /// jugador cubra el coste antes de delegar la operación al <see cref="MarketManager"/>.
    /// </summary>
    /// <param name="bien">Bien que el jugador desea adquirir.</param>
    /// <param name="cantidad">Unidades a comprar. Debe ser mayor que 0.</param>
    /// <returns>
    /// <c>true</c> si la compra se realizó correctamente y el inventario se ha actualizado;
    /// <c>false</c> si alguna validación falló (consultar <see cref="UltimoMensaje"/>).
    /// </returns>
    public bool Comprar(BienData bien, int cantidad)
    {
        if (!ValidarCondicionesBase("compra"))
            return false;

        if (bien == null)
        {
            UltimoMensaje = "No se ha especificado ningún bien para comprar.";
            Debug.LogWarning("[OficinaComercial] Comprar llamado con bien nulo.");
            return false;
        }

        if (cantidad <= 0)
        {
            UltimoMensaje = "La cantidad debe ser mayor que cero.";
            Debug.LogWarning($"[OficinaComercial] Cantidad de compra inválida: {cantidad}.");
            return false;
        }

        // Validación de stock en la ciudad
        int stockDisponible = _mercado.GetStockActual(bien);
        if (stockDisponible < cantidad)
        {
            UltimoMensaje = $"Stock insuficiente de {bien.nombre}: disponibles {stockDisponible}, solicitadas {cantidad}.";
            Debug.LogWarning($"[OficinaComercial] {UltimoMensaje}");
            return false;
        }

        // Validación de dinero del jugador
        long costeTotal = (long)(Mathf.Ceil(_mercado.GetPrecioActual(bien) * cantidad));
        if (GameManager.Instance.Dinero < costeTotal)
        {
            UltimoMensaje = $"Fondos insuficientes: la compra cuesta {costeTotal:N0} monedas y el tesoro solo tiene {GameManager.Instance.Dinero:N0}.";
            Debug.LogWarning($"[OficinaComercial] {UltimoMensaje}");
            return false;
        }

        // Delegar al mercado, que actualiza stock y bodega
        bool exito = _mercado.Comprar(bien, cantidad);

        UltimoMensaje = exito
            ? $"Compradas {cantidad} unidades de {bien.nombre} por {costeTotal:N0} monedas."
            : $"No se pudo completar la compra de {bien.nombre}. Comprueba la bodega.";

        return exito;
    }

    // ─── Venta ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Ejecuta la venta de un bien desde la bodega del jugador al mercado de la ciudad activa.
    /// Comprueba que el jugador disponga de suficientes unidades en bodega antes de delegar
    /// la operación al <see cref="MarketManager"/>.
    /// </summary>
    /// <param name="bien">Bien que el jugador desea vender.</param>
    /// <param name="cantidad">Unidades a vender. Debe ser mayor que 0.</param>
    /// <returns>
    /// <c>true</c> si la venta se realizó correctamente y el tesoro se ha actualizado;
    /// <c>false</c> si alguna validación falló (consultar <see cref="UltimoMensaje"/>).
    /// </returns>
    public bool Vender(BienData bien, int cantidad)
    {
        if (!ValidarCondicionesBase("venta"))
            return false;

        if (bien == null)
        {
            UltimoMensaje = "No se ha especificado ningún bien para vender.";
            Debug.LogWarning("[OficinaComercial] Vender llamado con bien nulo.");
            return false;
        }

        if (cantidad <= 0)
        {
            UltimoMensaje = "La cantidad debe ser mayor que cero.";
            Debug.LogWarning($"[OficinaComercial] Cantidad de venta inválida: {cantidad}.");
            return false;
        }

        // Validación de unidades en la bodega del jugador
        int enBodega = GameManager.Instance.GetCantidadBien(bien);
        if (enBodega < cantidad)
        {
            UltimoMensaje = $"Mercancía insuficiente en bodega: tienes {enBodega} unidades de {bien.nombre}, intentas vender {cantidad}.";
            Debug.LogWarning($"[OficinaComercial] {UltimoMensaje}");
            return false;
        }

        // Delegar al mercado, que actualiza stock y tesoro
        long precioEstimado = (long)(Mathf.Floor(_mercado.GetPrecioActual(bien) * cantidad));
        bool exito = _mercado.Vender(bien, cantidad);

        UltimoMensaje = exito
            ? $"Vendidas {cantidad} unidades de {bien.nombre} por {precioEstimado:N0} monedas."
            : $"No se pudo completar la venta de {bien.nombre}.";

        return exito;
    }

    // ─── Validaciones comunes ────────────────────────────────────────────────

    /// <summary>
    /// Comprueba las condiciones previas compartidas por compra y venta:
    /// que la oficina esté inicializada, que el mercado no sea nulo y que
    /// <see cref="GameManager.Instance"/> esté disponible.
    /// Rellena <see cref="UltimoMensaje"/> y registra un aviso en el log si falla alguna.
    /// </summary>
    /// <param name="tipoOperacion">Nombre de la operación ("compra" o "venta") para los mensajes de log.</param>
    /// <returns><c>true</c> si todas las condiciones se cumplen; <c>false</c> en caso contrario.</returns>
    private bool ValidarCondicionesBase(string tipoOperacion)
    {
        if (!_inicializada)
        {
            UltimoMensaje = "La oficina comercial no ha sido inicializada.";
            Debug.LogWarning($"[OficinaComercial] Intento de {tipoOperacion} sin inicializar la oficina.");
            return false;
        }

        if (_mercado == null)
        {
            UltimoMensaje = "No hay mercado activo asignado a la oficina comercial.";
            Debug.LogWarning($"[OficinaComercial] MarketManager es null al intentar una {tipoOperacion}.");
            return false;
        }

        if (GameManager.Instance == null)
        {
            UltimoMensaje = "El sistema de juego no está disponible. Inicia la partida desde el Menú Principal.";
            Debug.LogWarning($"[OficinaComercial] GameManager.Instance es null al intentar una {tipoOperacion}. ¿Se está probando la escena directamente?");
            return false;
        }

        return true;
    }
}
