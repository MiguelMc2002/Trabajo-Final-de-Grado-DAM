using System;
using UnityEngine;

/// <summary>
/// Gestiona el tiempo interno del juego: fecha, velocidad y avance diario.
/// Se añade manualmente al GameObject GameManager en escena.
/// No modifica <c>Time.timeScale</c> en ningún momento; la velocidad se aplica
/// multiplicando por <see cref="VelocidadActual"/> en el acumulador interno.
/// Esto permite que animaciones de UI y el menú de pausa sigan activos aunque
/// el tiempo de juego esté pausado.
/// </summary>
public class SimulacionTiempo : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global a la simulación activa. Puede ser <c>null</c> en escenas sin simulación.</summary>
    public static SimulacionTiempo Instance { get; private set; }

    // ─── Velocidades ──────────────────────────────────────────────────────────

    // Índice 0 = pausa, los demás son velocidades reales
    private static readonly float[] _velocidadesValidas = { 0f, 0.25f, 1f, 2f, 10f };

    // Empieza en índice 2 → velocidad 1x
    private int _indiceVelocidad = 2;

    // Último índice no-cero, para restaurar tras pausa por menú o TogglePausa
    private int _ultimoIndiceNoZero = 2;

    // ─── Configuración SerializeField ─────────────────────────────────────────

    [SerializeField] private float _segundosRealPorDiaJuego = 5f;

    [Header("Fecha de inicio")]
    [SerializeField] private int _diaInicio  = 1;
    [SerializeField] private int _mesInicio  = 3;
    [SerializeField] private int _añoInicio  = 1350;

    // ─── Estado de fecha ──────────────────────────────────────────────────────

    private int _diaActual;
    private int _mesActual;
    private int _añoActual;

    private float _acumulador;
    private float _acumuladorHora;

    // Delay de 0.5s para evitar input fantasma al cargar escena (mismo patrón que CiudadController)
    private float _tiempoDesdeInicio;
    private const float DelayInput = 0.5f;

    // ─── Propiedades públicas ─────────────────────────────────────────────────

    /// <summary>Día actual del calendario de juego (1-30).</summary>
    public int DiaActual => _diaActual;

    /// <summary>Mes actual del calendario de juego (1-12).</summary>
    public int MesActual => _mesActual;

    /// <summary>Año actual del calendario de juego.</summary>
    public int AñoActual => _añoActual;

    /// <summary>Velocidad de simulación activa. Devuelve 0 si está pausado.</summary>
    public float VelocidadActual => _velocidadesValidas[_indiceVelocidad];

    /// <summary>Indica si la simulación está pausada (<see cref="VelocidadActual"/> == 0).</summary>
    public bool EstaPausado => VelocidadActual == 0f;

    // ─── Eventos estáticos ────────────────────────────────────────────────────

    /// <summary>Se dispara cada vez que avanza una hora de juego (24 veces por día).</summary>
    public static event Action OnNuevaHora;

    /// <summary>Se dispara cada vez que avanza un día de juego.</summary>
    public static event Action OnNuevoDia;

    /// <summary>Se dispara cada vez que avanza un mes de juego.</summary>
    public static event Action OnNuevoMes;

    /// <summary>Se dispara cada vez que cambia la velocidad de simulación o el estado de pausa.</summary>
    public static event Action OnVelocidadCambiada;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        _diaActual  = _diaInicio;
        _mesActual  = _mesInicio;
        _añoActual  = _añoInicio;
    }

    private void Update()
    {
        _tiempoDesdeInicio += Time.deltaTime;

        // El menú de pausa congela Time.timeScale → no acumular tiempo de juego
        if (Time.timeScale == 0f) return;

        float dt = Time.deltaTime * VelocidadActual;
        _acumulador     += dt;
        _acumuladorHora += dt;

        float segundosPorHora = _segundosRealPorDiaJuego / 24f;
        while (_acumuladorHora >= segundosPorHora)
        {
            _acumuladorHora -= segundosPorHora;
            OnNuevaHora?.Invoke();
        }

        if (_acumulador >= _segundosRealPorDiaJuego)
        {
            _acumulador -= _segundosRealPorDiaJuego;
            AvanzarDia();
        }

        // Input de teclado con delay para evitar input fantasma
        if (_tiempoDesdeInicio < DelayInput) return;

        if (Input.GetKeyDown(KeyCode.Space))
            TogglePausa();
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            BajarVelocidad();
        else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            SubirVelocidad();
    }

    // ─── Avance de fecha ──────────────────────────────────────────────────────

    private void AvanzarDia()
    {
        _diaActual++;
        OnNuevoDia?.Invoke();

        if (_diaActual > 30)
        {
            _diaActual = 1;
            _mesActual++;
            OnNuevoMes?.Invoke();

            if (_mesActual > 12)
            {
                _mesActual = 1;
                _añoActual++;
            }
        }
    }

    // ─── Control de velocidad ─────────────────────────────────────────────────

    /// <summary>
    /// Incrementa la velocidad al siguiente nivel del array. Si ya está al máximo no hace nada.
    /// </summary>
    public void SubirVelocidad()
    {
        if (_indiceVelocidad >= _velocidadesValidas.Length - 1) return;

        _indiceVelocidad++;
        OnVelocidadCambiada?.Invoke();
    }

    /// <summary>
    /// Reduce la velocidad al nivel anterior del array. Nunca llega a pausa (índice 0).
    /// </summary>
    public void BajarVelocidad()
    {
        if (_indiceVelocidad <= 1) return;

        _indiceVelocidad--;
        OnVelocidadCambiada?.Invoke();
    }

    /// <summary>
    /// Alterna entre pausa y la última velocidad activa.
    /// Si está pausado, restaura el último índice no-cero.
    /// Si no está pausado, guarda el índice actual y pone velocidad 0.
    /// </summary>
    public void TogglePausa()
    {
        if (EstaPausado)
        {
            _indiceVelocidad = _ultimoIndiceNoZero;
        }
        else
        {
            _ultimoIndiceNoZero = _indiceVelocidad;
            _indiceVelocidad    = 0;
        }

        OnVelocidadCambiada?.Invoke();
    }

    /// <summary>
    /// Pausa la simulación sin guardar estado. Llamado por <see cref="MenuPausa"/> al abrir el panel.
    /// No toca <see cref="_ultimoIndiceNoZero"/> para no interferir con <see cref="TogglePausa"/>.
    /// </summary>
    public void PausarPorMenu()
    {
        _indiceVelocidad = 0;
        OnVelocidadCambiada?.Invoke();
    }

    /// <summary>
    /// Restaura la velocidad previa al abrir el menú. Llamado por <see cref="MenuPausa"/> al cerrar el panel.
    /// </summary>
    public void ReanudarDesdMenu()
    {
        _indiceVelocidad = _ultimoIndiceNoZero;
        OnVelocidadCambiada?.Invoke();
    }

    /// <summary>
    /// Sobrescribe la fecha y la velocidad de la simulación con los valores leídos desde
    /// la base de datos al cargar una partida guardada. Si la velocidad indicada no coincide
    /// con ninguno de los valores válidos se usa 1x como fallback.
    /// </summary>
    /// <param name="dia">Día del calendario de juego a restaurar (1-30).</param>
    /// <param name="mes">Mes del calendario de juego a restaurar (1-12).</param>
    /// <param name="anio">Año del calendario de juego a restaurar.</param>
    /// <param name="velocidad">Multiplicador de velocidad guardado (0.25, 1, 2 ó 10).</param>
    public void SetEstado(int dia, int mes, int anio, float velocidad)
    {
        _diaActual = dia;
        _mesActual = mes;
        _añoActual = anio;

        // Buscar el índice correspondiente a la velocidad guardada
        int indiceEncontrado = 2; // fallback: 1x
        for (int i = 0; i < _velocidadesValidas.Length; i++)
        {
            if (Mathf.Approximately(_velocidadesValidas[i], velocidad))
            {
                indiceEncontrado = i;
                break;
            }
        }

        _indiceVelocidad    = indiceEncontrado;
        _ultimoIndiceNoZero = indiceEncontrado > 0 ? indiceEncontrado : 2;
        OnVelocidadCambiada?.Invoke();

        Debug.Log($"[SimulacionTiempo] Estado restaurado: {_diaActual}/{_mesActual}/{_añoActual} vel={VelocidadActual}x");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─── Formato de texto ─────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la fecha actual formateada para mostrar en la interfaz.
    /// Ejemplo: "1 de Marzo de 1350".
    /// </summary>
    /// <returns>Cadena con la fecha completa en español.</returns>
    public string GetFechaFormateada()
    {
        return $"{_diaActual} de {NombreMes(_mesActual)} de {_añoActual}";
    }

    /// <summary>
    /// Devuelve la velocidad actual formateada para mostrar en la interfaz.
    /// Devuelve "⏸" si está pausado, o la velocidad con sufijo "x" en caso contrario.
    /// </summary>
    /// <returns>Cadena con la velocidad formateada.</returns>
    public string GetVelocidadFormateada()
    {
        if (EstaPausado) return "||";

        return VelocidadActual switch
        {
            0.25f => "0.25x",
            1f    => "1x",
            2f    => "2x",
            10f   => "10x",
            _     => $"{VelocidadActual}x"
        };
    }

    private static string NombreMes(int mes)
    {
        return mes switch
        {
            1  => "Enero",
            2  => "Febrero",
            3  => "Marzo",
            4  => "Abril",
            5  => "Mayo",
            6  => "Junio",
            7  => "Julio",
            8  => "Agosto",
            9  => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _  => "?"
        };
    }
}
