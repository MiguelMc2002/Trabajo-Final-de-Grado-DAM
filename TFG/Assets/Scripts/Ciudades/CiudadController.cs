using TMPro;
using UnityEngine;

/// <summary>
/// Coordinador central de la escena Ciudad.
/// Muestra el nombre del puerto en el que está atracado el jugador y actúa como
/// punto de entrada para los edificios clickables del mapa visual: cada
/// <see cref="EdificioClickable"/> de la escena llama a <see cref="AbrirEdificio"/>
/// al ser pulsado, y esta clase gestiona qué panel de UI mostrar u ocultar.
/// </summary>
public class CiudadController : MonoBehaviour
{
    // ─── Referencias UI ──────────────────────────────────────────────────────

    /// <summary>
    /// Texto donde se muestra el nombre del puerto en el que está atracado el jugador.
    /// </summary>
    [SerializeField] private TextMeshProUGUI _textoNombreCiudad;

    // ─── Datos de ciudad ─────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject con la configuración de la ciudad actual.
    /// Si está asignado, <see cref="Start"/> usa <see cref="CiudadData.NombreCiudad"/>
    /// para mostrar el nombre en pantalla en lugar de consultar <see cref="GameManager"/>.
    /// </summary>
    public CiudadData DatosCiudad;

    // ─── Paneles de edificios ─────────────────────────────────────────────────

    /// <summary>Panel de UI del mercado. Se activa al pulsar el edificio Mercado.</summary>
    public GameObject PanelMercado;

    /// <summary>Panel de UI del astillero. Se activa al pulsar el edificio Astillero.</summary>
    public GameObject PanelAstillero;

    /// <summary>Panel de UI de la taberna. Se activa al pulsar el edificio Taberna.</summary>
    public GameObject PanelTaberna;

    /// <summary>Panel de UI de la flota. Se activa al pulsar el edificio Puerto.</summary>
    public GameObject PanelFlota;

    /// <summary>Botón que regresa al mapamundi. Se oculta mientras hay un panel abierto.</summary>
    public GameObject BotonMapa;

    // ─────────────────────────────────────────────────────────────────────────

    // Momento en que se cargó la escena, para bloquear input accidental al inicio
    private float _tiempoCargaEscena;

    /// <summary>
    /// Inicializa la pantalla de ciudad: sincroniza <see cref="DatosCiudad"/> con el puerto
    /// registrado en <see cref="GameManager"/> si el jugador llegó desde el mapamundi,
    /// muestra el nombre en pantalla y cierra todos los paneles de edificios.
    /// </summary>
    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.CiudadActual != null)
            DatosCiudad = GameManager.Instance.CiudadActual;

        // Forzar la inicialización del mercado con la ciudad correcta antes de que
        // cualquier panel la consulte — evita la race condition con MarketManager.Start()
        MarketManager mercado = GetComponentInChildren<MarketManager>();
        if (mercado != null)
            mercado.InicializarConCiudad(DatosCiudad);

        MostrarNombreCiudad();
        CerrarTodosPaneles();
        _tiempoCargaEscena = Time.time;
    }

    // ─── Inicialización ───────────────────────────────────────────────────────

    /// <summary>
    /// Escribe el nombre de la ciudad actual en el encabezado de la pantalla.
    /// Prioridad: <see cref="DatosCiudad.NombreCiudad"/> → <see cref="GameManager.Instance"/>
    /// → "Ciudad de prueba" como último recurso para pruebas directas desde el editor.
    /// </summary>
    private void MostrarNombreCiudad()
    {
        if (_textoNombreCiudad == null)
        {
            Debug.LogWarning("[CiudadController] _textoNombreCiudad no asignado en el Inspector.");
            return;
        }

        if (DatosCiudad != null)
        {
            _textoNombreCiudad.text = DatosCiudad.NombreCiudad;
            return;
        }

        if (GameManager.Instance != null)
        {
            _textoNombreCiudad.text = GameManager.Instance.CiudadActual?.NombreCiudad ?? string.Empty;
            return;
        }

        Debug.LogWarning("[CiudadController] DatosCiudad y GameManager.Instance son null. Se muestra 'Ciudad de prueba'.");
        _textoNombreCiudad.text = "Ciudad de prueba";
    }

    // ─── API pública para edificios clickables ────────────────────────────────

    /// <summary>Abre directamente el panel del mercado.</summary>
    public void AbrirMercado() => AbrirEdificio(TipoEdificio.Mercado);

    /// <summary>Abre directamente el panel del astillero.</summary>
    public void AbrirAstillero() => AbrirEdificio(TipoEdificio.Astillero);

    /// <summary>Abre directamente el panel de la taberna.</summary>
    public void AbrirTaberna() => AbrirEdificio(TipoEdificio.Taberna);

    /// <summary>Abre directamente el panel de flota.</summary>
    public void AbrirPuerto() => AbrirEdificio(TipoEdificio.Puerto);

    /// <summary>
    /// Cierra todos los paneles de edificios ocultándolos.
    /// Se llama antes de abrir cualquier panel y al inicializar la escena.
    /// </summary>
    public void CerrarTodosPaneles()
    {
        if (PanelMercado   != null) PanelMercado.SetActive(false);
        if (PanelAstillero != null) PanelAstillero.SetActive(false);
        if (PanelTaberna   != null) PanelTaberna.SetActive(false);
        if (PanelFlota     != null) PanelFlota.SetActive(false);
        if (BotonMapa      != null) BotonMapa.SetActive(true);
    }

    /// <summary>
    /// Cierra los paneles abiertos y regresa al mapamundi.
    /// También se activa al pulsar la tecla M.
    /// </summary>
    public void IrAMapamundi()
    {
        Debug.Log("[CiudadController] IrAMapamundi() llamado desde: " +
                  new System.Diagnostics.StackTrace().ToString());
        CerrarTodosPaneles();
        SceneController.IrAMapamundi();
    }

    // ─── Input por teclado ────────────────────────────────────────────────────

    /// <summary>
    /// Detecta atajos de teclado de la pantalla de ciudad.
    /// M → vuelve al mapamundi.
    /// </summary>
    private void Update()
    {
        // Prevenir input accidental durante la carga de escena
        if (Time.time - _tiempoCargaEscena < 0.5f) return;

        if (Input.GetKeyDown(KeyCode.M)) IrAMapamundi();
    }

    /// <summary>
    /// Cierra todos los paneles y activa el panel correspondiente al edificio pulsado.
    /// Los componentes <see cref="EdificioClickable"/> de la escena invocan este método
    /// pasando su tipo como parámetro.
    /// </summary>
    /// <param name="tipo">Tipo de edificio pulsado por el jugador.</param>
    public void AbrirEdificio(TipoEdificio tipo)
    {
        CerrarTodosPaneles();
        if (BotonMapa != null) BotonMapa.SetActive(false);

        switch (tipo)
        {
            case TipoEdificio.Mercado:
                if (PanelMercado != null)
                    PanelMercado.SetActive(true);
                else
                    Debug.LogWarning("[CiudadController] PanelMercado no asignado en el Inspector.");
                break;

            case TipoEdificio.Astillero:
                if (PanelAstillero != null)
                    PanelAstillero.SetActive(true);
                else
                    Debug.LogWarning("[CiudadController] PanelAstillero no asignado en el Inspector.");
                break;

            case TipoEdificio.Taberna:
                if (PanelTaberna != null)
                    PanelTaberna.SetActive(true);
                else
                    Debug.LogWarning("[CiudadController] PanelTaberna no asignado en el Inspector.");
                break;

            case TipoEdificio.Puerto:
                if (PanelFlota != null)
                {
                    PanelFlota.SetActive(true);
                    PanelFlota.GetComponent<PanelFlotaUI>()?.AbrirPanel();
                }
                else
                    Debug.LogWarning("[CiudadController] PanelFlota no asignado en el Inspector.");
                break;

            default:
                Debug.LogWarning($"[CiudadController] TipoEdificio desconocido: {tipo}.");
                break;
        }
    }
}

/// <summary>
/// Identifica cada tipo de edificio con el que el jugador puede interactuar
/// en el mapa visual de la ciudad.
/// </summary>
public enum TipoEdificio
{
    /// <summary>Mercado de la ciudad: permite comprar y vender mercancías.</summary>
    Mercado,

    /// <summary>Astillero: construcción y reparación de barcos. Disponible tras la beta.</summary>
    Astillero,

    /// <summary>Taberna: contratación de capitanes y tripulación. Disponible tras la beta.</summary>
    Taberna,

    /// <summary>Puerto: gestión de la flota del jugador.</summary>
    Puerto
}
