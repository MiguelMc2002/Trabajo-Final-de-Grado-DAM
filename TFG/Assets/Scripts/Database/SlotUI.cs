using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente adjunto al prefab de cada fila de slot en la pantalla de guardado/carga.
/// Recibe los metadatos del slot, rellena los textos y activa o desactiva los botones
/// según si el slot contiene una partida guardada o está vacío.
/// </summary>
public class SlotUI : MonoBehaviour
{
    // ─── Textos ───────────────────────────────────────────────────────────────

    /// <summary>Muestra el nombre de la partida (p. ej. "Partida 2").</summary>
    [SerializeField] private TextMeshProUGUI _textoNombre;

    /// <summary>Muestra la fecha y hora del último guardado (p. ej. "12/04/1998 18:30").</summary>
    [SerializeField] private TextMeshProUGUI _textoFecha;

    /// <summary>Muestra los días de juego transcurridos (p. ej. "42 días jugados").</summary>
    [SerializeField] private TextMeshProUGUI _textoDias;

    // ─── Botones ──────────────────────────────────────────────────────────────

    /// <summary>GameObject padre que agrupa los tres botones de acción del slot (BotonesSlot).</summary>
    [SerializeField] private GameObject _contenedorBotones;

    /// <summary>Botón para guardar la partida actual en este slot.</summary>
    [SerializeField] private Button _botonGuardar;

    /// <summary>Botón para cargar la partida almacenada en este slot.</summary>
    [SerializeField] private Button _botonCargar;

    /// <summary>Botón para eliminar definitivamente la partida de este slot.</summary>
    [SerializeField] private Button _botonBorrar;

    // ─── Estado ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Metadatos del slot que esta fila representa. Accesible desde
    /// <see cref="PantallaSlotsUI"/> para identificar sobre qué slot actuar.
    /// </summary>
    public SlotData Datos { get; private set; }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la fila con los metadatos del slot, el modo del panel y la referencia al panel padre.
    /// Rellena los textos, aplica colores y configura qué botones están activos según el modo:
    /// modo Guardar → solo botón Guardar visible;
    /// modo Cargar → botones Cargar y Borrar visibles (solo si el slot está ocupado).
    /// Los listeners delegan en <paramref name="pantalla"/> para centralizar la lógica.
    /// </summary>
    /// <param name="datos">Metadatos del slot leídos al abrir el panel.</param>
    /// <param name="modo">Modo actual del panel (Guardar o Cargar).</param>
    /// <param name="pantalla">Panel padre que gestiona las acciones de guardado y carga.</param>
    public void Inicializar(SlotData datos, SlotModo modo, PantallaSlotsUI pantalla)
    {
        gameObject.SetActive(true);
        if (_botonGuardar.transform.parent != null)
            _botonGuardar.transform.parent.gameObject.SetActive(true);

        Datos = datos;

        // Rellenar textos y colores según estado del slot
        _textoFecha.color = new Color32(0xAA, 0xAA, 0xAA, 0xFF);
        _textoDias.color  = new Color32(0xAA, 0xAA, 0xAA, 0xFF);

        if (datos.EstaOcupado)
        {
            _textoNombre.text  = datos.NombrePartida;
            _textoNombre.color = new Color32(0xC8, 0xA8, 0x4B, 0xFF);
            _textoFecha.text   = datos.FechaGuardado;
            _textoDias.text    = $"{datos.DiasJugados} días jugados";
        }
        else
        {
            _textoNombre.text  = "— Vacío —";
            _textoNombre.color = new Color32(0x88, 0x88, 0x88, 0xFF);
            _textoFecha.text   = "";
            _textoDias.text    = "";
        }

        // Activar / desactivar botones según modo y estado del slot
        // BotonesSlot puede estar inactivo en escena: activarlo primero para que los hijos sean visibles
        if (_contenedorBotones != null)
            _contenedorBotones.SetActive(true);

        bool ocupado = datos.EstaOcupado;
        _botonGuardar.gameObject.SetActive(modo == SlotModo.Guardar);
        _botonCargar.gameObject.SetActive( modo == SlotModo.Cargar && ocupado);
        _botonBorrar.gameObject.SetActive( modo == SlotModo.Cargar && ocupado);

        // Registrar listeners (limpiar antes para evitar duplicados al reutilizar el prefab)
        _botonGuardar.onClick.RemoveAllListeners();
        _botonCargar.onClick.RemoveAllListeners();
        _botonBorrar.onClick.RemoveAllListeners();

        _botonGuardar.onClick.AddListener(() => pantalla.OnGuardar(this));
        _botonCargar.onClick.AddListener(()  => pantalla.OnCargar(this));
        _botonBorrar.onClick.AddListener(()  => pantalla.OnBorrar(this));
    }
}
