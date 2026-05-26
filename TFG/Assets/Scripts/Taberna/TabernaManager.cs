using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor singleton de la taberna. Controla la oferta diaria de marineros y capitanes
/// por ciudad, y gestiona la contratación de ambos tipos de tripulación.
/// Adjuntar a un GameObject en la escena Ciudad.
/// </summary>
public class TabernaManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor de la taberna activo.</summary>
    public static TabernaManager Instance { get; private set; }

    // ─── Estado por ciudad ────────────────────────────────────────────────────

    // Clave: IdCiudad, Valor: marineros disponibles hoy
    private readonly Dictionary<int, int> _marinerosDisponibles
        = new Dictionary<int, int>();

    // Clave: IdCiudad, Valor: lista de capitanes contratables en esa ciudad
    private readonly Dictionary<int, List<CapitanData>> _capitanesDisponibles
        = new Dictionary<int, List<CapitanData>>();

    // Capitanes ya contratados por el jugador (en cualquier ciudad)
    private readonly List<CapitanData> _capitanesContratados
        = new List<CapitanData>();

    private int _proximoIdCapitan = 1;

    /// <summary>Coste en oro por marinero contratado en la taberna.</summary>
    public const int PrecioMarinero = 50;

    private static readonly string[] _nombresMedievales =
    {
        "Hans", "Klaus", "Erik", "Pieter", "Johann", "Willem", "Dirk", "Conrad",
        "Albrecht", "Heinrich", "Gerhard", "Rutger", "Berthold", "Siegfried",
        "Wolfram", "Dietrich", "Kaspar", "Ludolf", "Reinhardt", "Gottfried"
    };

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SimulacionTiempo.OnNuevoDia += RefrescarOfertaDiaria;
    }

    private void OnDestroy()
    {
        SimulacionTiempo.OnNuevoDia -= RefrescarOfertaDiaria;
    }

    // ─── Refresco diario ──────────────────────────────────────────────────────

    /// <summary>
    /// Regenera la oferta de marineros y capitanes en todas las ciudades disponibles.
    /// Se llama automáticamente cada día simulado.
    /// </summary>
    private void RefrescarOfertaDiaria()
    {
        var ciudades = GameManager.Instance?.CiudadesDisponibles;
        if (ciudades == null) return;

        foreach (CiudadData ciudad in ciudades)
        {
            int id = ciudad.IdCiudad;

            // Regenerar marineros
            _marinerosDisponibles[id] = Random.Range(10, 51);

            // Generar un capitán si la ciudad no tiene ninguno disponible
            if (!_capitanesDisponibles.ContainsKey(id) || _capitanesDisponibles[id].Count == 0)
            {
                string nombre   = _nombresMedievales[Random.Range(0, _nombresMedievales.Length)];
                var    capitan  = new CapitanData(_proximoIdCapitan++, nombre);

                if (!_capitanesDisponibles.ContainsKey(id))
                    _capitanesDisponibles[id] = new List<CapitanData>();

                _capitanesDisponibles[id].Add(capitan);
                Debug.Log($"[TabernaManager] Capitán '{nombre}' disponible en {ciudad.NombreCiudad}.");
            }
        }
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el número de marineros disponibles para contratar en la ciudad indicada.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <returns>Marineros disponibles hoy, o 0 si la ciudad no está registrada.</returns>
    public int GetMarinerosDisponibles(int idCiudad)
        => _marinerosDisponibles.TryGetValue(idCiudad, out int val) ? val : 0;

    /// <summary>
    /// Devuelve la lista de capitanes contratables en la ciudad indicada.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad.</param>
    /// <returns>Lista de <see cref="CapitanData"/> disponibles, o lista vacía si no hay.</returns>
    public List<CapitanData> GetCapitanesDisponibles(int idCiudad)
    {
        if (_capitanesDisponibles.TryGetValue(idCiudad, out var lista)) return lista;
        return new List<CapitanData>();
    }

    /// <summary>
    /// Contrata marineros para el barco indicado. El único límite es el hueco disponible
    /// en el barco (TripulacionMaxima − Tripulacion) y el oro del jugador.
    /// La oferta de marineros en ciudad es ilimitada.
    /// </summary>
    /// <param name="barco">Barco al que se incorporan los marineros.</param>
    /// <param name="cantidad">Número de marineros a contratar.</param>
    /// <returns><see cref="ResultadoOperacion"/> con el resultado de la operación.</returns>
    public ResultadoOperacion ContratarMarineros(BarcoJugador barco, int cantidad)
    {
        if (barco == null)
            return Fallo("Barco no válido.");

        int hueco = barco.TripulacionMaxima - barco.Tripulacion;

        if (hueco <= 0)
            return Fallo("El barco ya tiene la tripulación completa.");

        // Limitar solo por hueco en el barco; la oferta de ciudad es ilimitada
        int marionerosReales = cantidad > hueco ? hueco : cantidad;
        if (marionerosReales <= 0)
            return Fallo("No se pueden contratar marineros.");

        long costeReal = (long)marionerosReales * PrecioMarinero;
        if (GameManager.Instance.Dinero < costeReal)
            return Fallo($"Oro insuficiente. Necesitas {costeReal}, tienes {GameManager.Instance.Dinero}.");

        barco.ContratarMarineros(marionerosReales);
        GameManager.Instance.ModificarDinero(-costeReal);

        Debug.Log($"[TabernaManager] {marionerosReales} marineros contratados para '{barco.Nombre}' " +
                  $"por {costeReal} ƒ. Tripulación: {barco.Tripulacion}/{barco.TripulacionMaxima}.");
        return Exito();
    }

    /// <summary>
    /// Devuelve la lista de barcos de la flota del jugador.
    /// </summary>
    /// <returns>Lista de solo lectura con los barcos de <see cref="FlotaJugador"/>.</returns>
    public IReadOnlyList<BarcoJugador> ObtenerBarcosDeLaFlota()
        => GameManager.Instance?.FlotaJugador?.Barcos
           ?? new System.Collections.Generic.List<BarcoJugador>();

    /// <summary>
    /// Contrata un capitán para el barco indicado y lo mueve de la oferta de la ciudad
    /// a la lista de capitanes contratados. El coste fijo es 500 oro.
    /// Falla si el barco ya tiene capitán o si el capitán no está disponible en la ciudad actual.
    /// </summary>
    /// <param name="barco">Barco que recibirá al capitán.</param>
    /// <param name="capitan">Capitán a contratar.</param>
    /// <returns><see cref="ResultadoOperacion"/> con el resultado de la operación.</returns>
    public ResultadoOperacion ContratarCapitan(BarcoJugador barco, CapitanData capitan)
    {
        if (barco == null || capitan == null)
            return Fallo("Barco o capitán no válido.");

        CiudadData ciudad = GameManager.Instance.CiudadActual;
        if (ciudad == null)
            return Fallo("No hay ciudad activa.");

        int idCiudad = ciudad.IdCiudad;

        if (!_capitanesDisponibles.TryGetValue(idCiudad, out var listaDisponibles)
            || !listaDisponibles.Contains(capitan))
            return Fallo($"'{capitan.Nombre}' no está disponible en {ciudad.NombreCiudad}.");

        if (GetCapitanDeBarco(barco.IdBarco) != null)
            return Fallo($"'{barco.Nombre}' ya tiene un capitán asignado.");

        const long coste = 500L;
        if (GameManager.Instance.Dinero < coste)
            return Fallo($"Oro insuficiente. Contratar un capitán cuesta {coste} oro.");

        GameManager.Instance.ModificarDinero(-coste);
        capitan.IdBarcoAsignado = barco.IdBarco;
        listaDisponibles.Remove(capitan);
        _capitanesContratados.Add(capitan);

        Debug.Log($"[TabernaManager] Capitán '{capitan.Nombre}' contratado para '{barco.Nombre}' por {coste} oro. " +
                  $"Nav:{capitan.HabilidadNavegacion} Com:{capitan.HabilidadCombate}.");
        return Exito();
    }

    /// <summary>
    /// Devuelve el capitán actualmente asignado al barco indicado.
    /// </summary>
    /// <param name="idBarco">Identificador del barco.</param>
    /// <returns>El <see cref="CapitanData"/> asignado, o <c>null</c> si el barco no tiene capitán.</returns>
    public CapitanData GetCapitanDeBarco(int idBarco)
    {
        foreach (CapitanData c in _capitanesContratados)
            if (c.IdBarcoAsignado == idBarco) return c;
        return null;
    }

    /// <summary>
    /// Lista de capitanes contratados por el jugador. Solo lectura desde fuera.
    /// </summary>
    public IReadOnlyList<CapitanData> CapitanesContratados => _capitanesContratados.AsReadOnly();

    /// <summary>
    /// Añade un capitán ya contratado a la lista interna, sin pasar por el flujo de contratación.
    /// Usar exclusivamente al restaurar el estado desde la base de datos.
    /// </summary>
    /// <param name="capitan">Capitán a restaurar.</param>
    public void RestaurarCapitanContratado(CapitanData capitan)
    {
        if (capitan == null) return;
        if (!_capitanesContratados.Contains(capitan))
            _capitanesContratados.Add(capitan);
    }

    /// <summary>
    /// Vacía la lista de capitanes contratados. Llamar exclusivamente desde
    /// <see cref="LoadManager"/> antes de restaurar capitanes desde la base de datos,
    /// para evitar duplicados cuando el singleton persiste entre cargas.
    /// </summary>
    public void LimpiarCapitanesContratados() => _capitanesContratados.Clear();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ResultadoOperacion Exito()
        => new ResultadoOperacion { Exito = true };

    private static ResultadoOperacion Fallo(string mensaje)
        => new ResultadoOperacion { Exito = false, MensajeError = mensaje };
}
