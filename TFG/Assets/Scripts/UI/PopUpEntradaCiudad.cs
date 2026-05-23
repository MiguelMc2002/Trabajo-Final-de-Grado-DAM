using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel modal que aparece cuando la flota del jugador llega a una casilla de ciudad.
/// Pausa la simulación mientras el jugador decide.
/// - Botón Entrar: establece la ciudad en GameManager y carga la escena Ciudad.
/// - Botón Continuar navegando: cierra el panel, reanuda la simulación y deja la flota parada
///   esperando el siguiente clic derecho del jugador.
/// El GameObject raíz de este componente actúa como el panel visual;
/// se activa con Mostrar() y se desactiva con Ocultar().
/// Asignar TxtNombreCiudad, BtnEntrar y BtnContinuar desde el Inspector.
/// </summary>
public class PopUpEntradaCiudad : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtNombreCiudad;

    /// <summary>Botón que carga la escena Ciudad. Asignar desde el Inspector.</summary>
    [SerializeField] private Button btnEntrar;

    /// <summary>Botón que cierra el panel sin entrar. Asignar desde el Inspector.</summary>
    [SerializeField] private Button btnContinuar;

    private CiudadData _ciudadPendiente;

    private void Awake()
    {
        btnEntrar.onClick.AddListener(AlEntrar);
        btnContinuar.onClick.AddListener(AlContinuar);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra el pop-up con el nombre de la ciudad y pausa la simulación de tiempo.
    /// Llamado por NavegacionJugadorController al detectar llegada a casilla de ciudad.
    /// </summary>
    public void Mostrar(CiudadData ciudad)
    {
        _ciudadPendiente = ciudad;
        if (txtNombreCiudad != null)
            txtNombreCiudad.text = $"Has llegado a {ciudad.NombreCiudad}.\n¿Entras al puerto?";
        gameObject.SetActive(true);
        SimulacionTiempo.Instance?.PausarPorMenu();
    }

    /// <summary>
    /// Oculta el panel y reanuda la simulación de tiempo.
    /// </summary>
    public void Ocultar()
    {
        gameObject.SetActive(false);
        SimulacionTiempo.Instance?.ReanudarDesdMenu();
    }

    private void AlEntrar()
    {
        if (_ciudadPendiente == null) { Ocultar(); return; }
        GameManager.Instance.EstablecerCiudadActual(_ciudadPendiente);
        Ocultar();
        SceneController.IrACiudad();
    }

    private void AlContinuar()
    {
        _ciudadPendiente = null;
        NavegacionJugadorController.Instance?.MarcarSalidaDeCiudad();
        Ocultar();
    }
}
