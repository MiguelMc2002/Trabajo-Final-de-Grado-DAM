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
    /// Inicializa la fila con los metadatos del slot y la referencia al panel padre.
    /// Rellena los textos y configura qué botones están activos:
    /// slot vacío → solo Guardar; slot ocupado → los tres botones.
    /// Los listeners de los botones delegan en <paramref name="pantalla"/> para
    /// mantener toda la lógica de guardado/carga centralizada.
    /// </summary>
    /// <param name="datos">Metadatos del slot leídos al abrir el panel.</param>
    /// <param name="pantalla">Panel padre que gestiona las acciones de guardado y carga.</param>
    public void Inicializar(SlotData datos, PantallaSlotsUI pantalla)
    {
        Datos = datos;

        // Rellenar textos
        _textoNombre.text = datos.NombrePartida;

        if (datos.EstaOcupado)
        {
            _textoFecha.text = datos.FechaGuardado;
            _textoDias.text  = $"{datos.DiasJugados} días jugados";
        }
        else
        {
            _textoFecha.text = string.Empty;
            _textoDias.text  = string.Empty;
        }

        // Activar / desactivar botones según estado del slot
        _botonGuardar.gameObject.SetActive(true);
        _botonCargar.gameObject.SetActive(datos.EstaOcupado);
        _botonBorrar.gameObject.SetActive(datos.EstaOcupado);

        // Registrar listeners (limpiar antes para evitar duplicados al reutilizar el prefab)
        _botonGuardar.onClick.RemoveAllListeners();
        _botonCargar.onClick.RemoveAllListeners();
        _botonBorrar.onClick.RemoveAllListeners();

        _botonGuardar.onClick.AddListener(() => pantalla.OnGuardar(this));
        _botonCargar.onClick.AddListener(()  => pantalla.OnCargar(this));
        _botonBorrar.onClick.AddListener(()  => pantalla.OnBorrar(this));
    }
}
