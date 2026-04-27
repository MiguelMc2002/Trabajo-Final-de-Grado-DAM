using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Panel de HUD que muestra la fecha y la velocidad de simulación actuales.
/// Persiste entre escenas mediante <c>DontDestroyOnLoad</c>. Si se detecta un
/// duplicado al cargar una escena, la instancia nueva se auto-destruye.
/// Se oculta automáticamente en escenas no jugables suscribiéndose a
/// <c>SceneManager.sceneLoaded</c>; las escenas donde debe ser visible se
/// configuran en <c>_escenasVisibles</c> desde el Inspector.
/// Crear el GameObject que contiene este componente en UNA SOLA escena
/// (p. ej. la primera escena jugable); no añadirlo en el Canvas de cada escena.
/// El input de teclado lo gestiona <see cref="SimulacionTiempo"/>; los botones
/// de este panel son un acceso alternativo vía ratón.
/// </summary>
public class HUDTiempo : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al HUD de tiempo persistente.</summary>
    public static HUDTiempo Instance { get; private set; }

    // ─── Visibilidad por escena ───────────────────────────────────────────────

    /// <summary>
    /// Nombres exactos de las escenas en las que el HUD debe ser visible.
    /// En el resto de escenas <c>_panelVisible</c> se desactiva automáticamente.
    /// </summary>
    [SerializeField] private string[] _escenasVisibles = { "Ciudad", "Mapamundi" };

    /// <summary>
    /// GameObject hijo que contiene todo el contenido visual del HUD.
    /// Se activa/desactiva según la escena; el GameObject raíz permanece
    /// siempre activo para que la suscripción a <c>SceneManager.sceneLoaded</c>
    /// no se pierda al ocultar el HUD.
    /// </summary>
    [SerializeField] private GameObject _panelVisible;

    // ─── Referencias UI ───────────────────────────────────────────────────────

    [SerializeField] private TextMeshProUGUI _textoFecha;
    [SerializeField] private TextMeshProUGUI _textoVelocidad;

    /// <summary>Botón que reduce la velocidad de simulación un nivel.</summary>
    [SerializeField] private Button _botonMenos;

    /// <summary>Botón que aumenta la velocidad de simulación un nivel.</summary>
    [SerializeField] private Button _botonMas;

    /// <summary>Referencia a la simulación de tiempo. Asignar desde el Inspector.</summary>
    [SerializeField] private SimulacionTiempo _simulacion;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Destruir duplicados que aparezcan al recargar escenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Aplicar visibilidad correcta desde el primer frame sobre el panel hijo
        if (_panelVisible != null)
            _panelVisible.SetActive(System.Array.IndexOf(_escenasVisibles, SceneManager.GetActiveScene().name) >= 0);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnScenaCargada;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnScenaCargada;
    }

    private void Start()
    {
        // Registrar botones con RemoveAllListeners preventivo para evitar duplicados
        _botonMenos.onClick.RemoveAllListeners();
        _botonMas.onClick.RemoveAllListeners();
        if (_simulacion != null)
        {
            _botonMenos.onClick.AddListener(_simulacion.BajarVelocidad);
            _botonMas.onClick.AddListener(_simulacion.SubirVelocidad);
        }

        SimulacionTiempo.OnNuevoDia          += ActualizarUI;
        SimulacionTiempo.OnVelocidadCambiada += ActualizarUI;

        ActualizarUI();
    }

    private void OnDestroy()
    {
        SimulacionTiempo.OnNuevoDia          -= ActualizarUI;
        SimulacionTiempo.OnVelocidadCambiada -= ActualizarUI;
    }

    // ─── Visibilidad ──────────────────────────────────────────────────────────

    private void OnScenaCargada(Scene escena, LoadSceneMode modo)
    {
        if (_panelVisible != null)
            _panelVisible.SetActive(System.Array.IndexOf(_escenasVisibles, escena.name) >= 0);
    }

    // ─── Refresco de UI ───────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza todos los textos y el estado interactable de los botones.
    /// Se llama al inicio y cada vez que cambia la fecha o la velocidad.
    /// </summary>
    public void ActualizarUI()
    {
        if (_simulacion == null) return;

        _textoFecha.text     = _simulacion.GetFechaFormateada();
        _textoVelocidad.text = _simulacion.GetVelocidadFormateada();

        _botonMenos.interactable = !_simulacion.EstaPausado && _simulacion.VelocidadActual > 0.25f;
        _botonMas.interactable   = !_simulacion.EstaPausado && _simulacion.VelocidadActual < 10f;
    }
}
