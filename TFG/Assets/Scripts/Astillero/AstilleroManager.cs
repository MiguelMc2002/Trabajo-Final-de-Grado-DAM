using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resultado de una operación del astillero (compra, instalación, reparación o venta).
/// </summary>
public struct ResultadoOperacion
{
    /// <summary><c>true</c> si la operación se completó con éxito.</summary>
    public bool Exito;

    /// <summary>Descripción del motivo del fallo. Vacío si <see cref="Exito"/> es <c>true</c>.</summary>
    public string MensajeError;
}

/// <summary>
/// Gestor singleton del astillero. Centraliza la compra, personalización, reparación
/// y venta de barcos del jugador, verificando oro antes de cada operación.
/// Adjuntar a un GameObject en la escena Ciudad y asignar los catálogos desde el Inspector.
/// La persistencia de la flota del jugador se implementará en un día buffer posterior;
/// por ahora todo vive en memoria.
/// </summary>
public class AstilleroManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor del astillero activo.</summary>
    public static AstilleroManager Instance { get; private set; }

    // ─── Catálogos (asignar desde Inspector) ─────────────────────────────────

    /// <summary>
    /// Cascos base (TipoCascoData legacy) asignados desde el Inspector.
    /// Se combinan con <see cref="_cascosDecoradores"/> en <see cref="CascosDisponibles"/>.
    /// </summary>
    [SerializeField] private List<TipoCascoData> _cascosBases = new List<TipoCascoData>();

    /// <summary>
    /// Cascos definidos mediante decoradores concretos (CascoCog, CascoHulk, etc.).
    /// Asignar los assets desde el Inspector.
    /// </summary>
    [SerializeField] private List<CascoDecorador> _cascosDecoradores = new List<CascoDecorador>();

    /// <summary>
    /// Lista unificada de todos los tipos de casco disponibles (bases + decoradores).
    /// Usar esta propiedad en lugar de acceder directamente a las listas internas.
    /// </summary>
    public IReadOnlyList<IBarco> CascosDisponibles
    {
        get
        {
            var combinados = new List<IBarco>();
            combinados.AddRange(_cascosBases);
            combinados.AddRange(_cascosDecoradores);
            return combinados;
        }
    }

    /// <summary>
    /// Módulos disponibles para instalar en los barcos.
    /// Asignar todos los ScriptableObjects de módulo desde el Inspector.
    /// </summary>
    [SerializeField] public List<ModuloBarcoData> ModulosDisponibles = new List<ModuloBarcoData>();

    // Contador interno para generar IDs únicos de barco durante la sesión
    private int _proximoIdBarco = 1;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Construye un barco nuevo con el casco indicado y lo añade a la flota del jugador.
    /// Descuenta el coste en oro. No consume materiales (sin persistencia de almacén por ahora).
    /// </summary>
    /// <param name="casco">Tipo de casco a construir.</param>
    /// <param name="nombreBarco">Nombre propio del nuevo barco.</param>
    /// <returns><see cref="ResultadoOperacion"/> con el resultado de la operación.</returns>
    public ResultadoOperacion ComprarBarco(IBarco casco, string nombreBarco)
    {
        if (casco == null)
            return Fallo("Tipo de casco no válido.");

        FlotaJugador flota = GameManager.Instance.FlotaJugador;

        if (flota.Barcos.Count >= FlotaJugador.MaxBarcos)
            return Fallo($"La flota ya tiene el máximo de {FlotaJugador.MaxBarcos} barcos.");

        if (GameManager.Instance.Dinero < casco.CosteOro)
            return Fallo($"Oro insuficiente. Necesitas {casco.CosteOro}, tienes {GameManager.Instance.Dinero}.");

        GameManager.Instance.ModificarDinero(-casco.CosteOro);

        var barco = new BarcoJugador(_proximoIdBarco++, nombreBarco, casco);
        flota.AñadirBarco(barco);

        Debug.Log($"[AstilleroManager] Barco comprado: '{nombreBarco}' ({casco.NombreCasco}) por {casco.CosteOro} oro.");
        return Exito();
    }

    /// <summary>
    /// Instala un módulo en el barco indicado.
    /// Si el barco ya tiene un módulo del mismo <see cref="TipoModulo"/>, lo sustituye
    /// calculando el coste neto: coste nuevo − 50% del coste del anterior.
    /// Si el coste neto es negativo, devuelve la diferencia al jugador.
    /// </summary>
    /// <param name="barco">Barco donde se instalará el módulo.</param>
    /// <param name="moduloNuevo">Módulo a instalar.</param>
    /// <returns><see cref="ResultadoOperacion"/> con el resultado de la operación.</returns>
    public ResultadoOperacion InstalarModulo(BarcoJugador barco, ModuloBarcoData moduloNuevo)
    {
        if (barco == null || moduloNuevo == null)
            return Fallo("Barco o módulo no válido.");

        // Comprobar requisito de pólvora antes de nada
        if (moduloNuevo.requierePolvora &&
            (SimulacionTiempo.Instance == null ||
             SimulacionTiempo.Instance.AñoActual < ModuloBarcoData.AnioDesbloqueoPolvoraJuego))
            return Fallo($"'{moduloNuevo.nombreModulo}' requiere pólvora (disponible en {ModuloBarcoData.AnioDesbloqueoPolvoraJuego}).");

        // Calcular coste neto según si ya hay módulo del mismo tipo
        ModuloBarcoData moduloAnterior = barco.ObtenerModuloPorTipo(moduloNuevo.tipoModulo);
        int costeNeto;

        if (moduloAnterior != null)
        {
            // Swap: devolvemos el 50% del módulo anterior
            costeNeto = moduloNuevo.costeOro - Mathf.RoundToInt(moduloAnterior.costeOro * 0.5f);
        }
        else
        {
            // Comprobar slots solo si no hay swap (el swap libera primero)
            if (barco.SlotsDisponibles < moduloNuevo.slotsCosto)
                return Fallo($"Slots insuficientes. Necesitas {moduloNuevo.slotsCosto}, libres {barco.SlotsDisponibles}.");

            costeNeto = moduloNuevo.costeOro;
        }

        // Verificar oro si el coste es positivo
        if (costeNeto > 0 && GameManager.Instance.Dinero < costeNeto)
            return Fallo($"Oro insuficiente. Coste neto: {costeNeto}, tienes {GameManager.Instance.Dinero}.");

        // Aplicar cambios económicos
        if (costeNeto >= 0)
            GameManager.Instance.ModificarDinero(-costeNeto);
        else
            GameManager.Instance.ModificarDinero(Mathf.Abs(costeNeto));

        // Sustituir módulo si hay uno anterior
        if (moduloAnterior != null)
            barco.DesinstalarModulo(moduloAnterior);

        barco.InstalarModulo(moduloNuevo);

        Debug.Log($"[AstilleroManager] Módulo '{moduloNuevo.nombreModulo}' instalado en '{barco.Nombre}'. Coste neto: {costeNeto} oro.");
        return Exito();
    }

    /// <summary>
    /// Repara el barco hasta su vida máxima actual (<see cref="BarcoJugador.VidaTotal"/>).
    /// El coste es de 10 oro por punto de daño. No hace nada si el barco ya está en perfecto estado.
    /// </summary>
    /// <param name="barco">Barco a reparar.</param>
    /// <returns><see cref="ResultadoOperacion"/> con el resultado de la operación.</returns>
    public ResultadoOperacion RepararBarco(BarcoJugador barco)
    {
        if (barco == null)
            return Fallo("Barco no válido.");

        int danio = barco.VidaTotal - barco.VidaActual;
        if (danio <= 0)
        {
            Debug.Log($"[AstilleroManager] '{barco.Nombre}' ya está en perfecto estado.");
            return Exito();
        }

        long costeReparacion = danio * 10L;

        if (GameManager.Instance.Dinero < costeReparacion)
            return Fallo($"Oro insuficiente. Reparación cuesta {costeReparacion}, tienes {GameManager.Instance.Dinero}.");

        GameManager.Instance.ModificarDinero(-costeReparacion);
        barco.VidaActual = barco.VidaTotal;

        Debug.Log($"[AstilleroManager] '{barco.Nombre}' reparado por {costeReparacion} oro ({danio} puntos de daño).");
        return Exito();
    }

    /// <summary>
    /// Vende un barco de la flota del jugador por el 50% del valor total
    /// (coste del casco más coste de todos los módulos instalados) y lo elimina de la flota.
    /// </summary>
    /// <param name="barco">Barco a vender.</param>
    /// <returns><see cref="ResultadoOperacion"/> con el resultado de la operación.</returns>
    public ResultadoOperacion VenderBarco(BarcoJugador barco)
    {
        if (barco == null)
            return Fallo("Barco no válido.");

        int costeTotal = barco.CascoBase.CosteOro;
        foreach (ModuloBarcoData m in barco.ModulosInstalados)
            costeTotal += m.costeOro;

        long valorVenta = (long)(costeTotal * 0.5f);

        GameManager.Instance.ModificarDinero(valorVenta);
        GameManager.Instance.FlotaJugador.EliminarBarco(barco.IdBarco);

        Debug.Log($"[AstilleroManager] '{barco.Nombre}' vendido por {valorVenta} oro.");
        return Exito();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ResultadoOperacion Exito()
        => new ResultadoOperacion { Exito = true };

    private static ResultadoOperacion Fallo(string mensaje)
        => new ResultadoOperacion { Exito = false, MensajeError = mensaje };
}
