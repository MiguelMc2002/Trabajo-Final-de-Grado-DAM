using TMPro;
using UnityEngine;

/// <summary>
/// Coordinador central de la escena Ciudad.
/// Muestra el nombre del puerto en el que está atracado el jugador y actúa como
/// punto de entrada para los edificios clickables del mapa visual: cada
/// <see cref="EdificioClickable"/> de la escena llama a <see cref="AbrirEdificio"/>
/// al ser pulsado, y esta clase decide qué pantalla cargar o qué aviso emitir.
/// </summary>
public class CiudadController : MonoBehaviour
{
    // ─── Referencias UI ──────────────────────────────────────────────────────

    /// <summary>
    /// Texto donde se muestra el nombre del puerto en el que está atracado el jugador.
    /// Si <see cref="GameManager.Instance"/> no está disponible al abrir la escena
    /// (prueba directa sin pasar por el Menú Principal), muestra "Ciudad de prueba".
    /// </summary>
    [SerializeField] private TextMeshProUGUI _textoNombreCiudad;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        MostrarNombreCiudad();
    }

    // ─── Inicialización ───────────────────────────────────────────────────────

    /// <summary>
    /// Escribe el nombre de la ciudad actual en el encabezado de la pantalla.
    /// Si <see cref="GameManager.Instance"/> es <c>null</c> (p. ej. al probar la escena
    /// Ciudad directamente), muestra "Ciudad de prueba" y registra un aviso en el log
    /// sin lanzar excepción.
    /// </summary>
    private void MostrarNombreCiudad()
    {
        if (_textoNombreCiudad == null)
        {
            Debug.LogWarning("[CiudadController] _textoNombreCiudad no asignado en el Inspector.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CiudadController] GameManager.Instance es null. ¿Se está probando la escena directamente? Se muestra 'Ciudad de prueba'.");
            _textoNombreCiudad.text = "Ciudad de prueba";
            return;
        }

        _textoNombreCiudad.text = GameManager.Instance.CiudadActual;
    }

    // ─── API pública para edificios clickables ────────────────────────────────

    /// <summary>
    /// Abre el servicio correspondiente al edificio que el jugador acaba de pulsar.
    /// Los componentes <see cref="EdificioClickable"/> de la escena invocan este método
    /// pasando su tipo como parámetro.
    /// En la beta solo el mercado navega a una nueva pantalla; astillero y taberna
    /// emiten un aviso y permanecen pendientes para el Día 4.
    /// </summary>
    /// <param name="tipo">Tipo de edificio pulsado por el jugador.</param>
    public void AbrirEdificio(TipoEdificio tipo)
    {
        switch (tipo)
        {
            case TipoEdificio.Mercado:
                SceneController.IrAMercado();
                break;

            case TipoEdificio.Astillero:
                Debug.Log("[CiudadController] Astillero no disponible en beta.");
                break;

            case TipoEdificio.Taberna:
                Debug.Log("[CiudadController] Taberna no disponible en beta.");
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
    Taberna
}
