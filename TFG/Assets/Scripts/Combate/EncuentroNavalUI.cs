using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour que gestiona el panel de encuentro naval del jugador.
/// Se suscribe a <see cref="CombateEventos.OnCombateIniciado"/>, muestra el panel
/// cuando el jugador está involucrado y delega la resolución en <see cref="CombateNavalResolver"/>.
/// El panel expone dos botones: Luchar (el jugador combate) y Huir (intenta escapar).
/// Adjuntar a un GameObject persistente en la escena Mapamundi y asignar los campos desde el Inspector.
/// </summary>
public class EncuentroNavalUI : MonoBehaviour
{
    [SerializeField] private GameObject          _panelEncuentro;
    [SerializeField] private TextMeshProUGUI     _textoNarrativo;
    [SerializeField] private Button              _btnLuchar;
    [SerializeField] private Button              _btnHuir;

    [SerializeField] private TextMeshProUGUI _textoHuida;

    private FlotaRuntimeData _atacante;
    private FlotaRuntimeData _defensor;
    private bool             _jugadorEsAtacante;

    private void Start()
    {
        _btnLuchar.onClick.AddListener(OnLuchar);
        _btnHuir.onClick.AddListener(OnHuir);

        if (_panelEncuentro != null)
            _panelEncuentro.SetActive(false);
    }

    private void OnEnable()
    {
        CombateEventos.OnCombateIniciado += OnCombateIniciado;
    }

    private void OnDisable()
    {
        CombateEventos.OnCombateIniciado -= OnCombateIniciado;
    }

    // ─── Evento de combate ────────────────────────────────────────────────────

    private void OnCombateIniciado(FlotaRuntimeData atacante, FlotaRuntimeData defensor)
    {
        _atacante          = atacante;
        _defensor          = defensor;
        // El jugador tiene id -1; si es el atacante, él inició la persecución en modo pirata
        _jugadorEsAtacante = (atacante.Id == -1);

        _panelEncuentro.SetActive(true);
        SceneController.SetPausa(true);

        _textoNarrativo.text = _jugadorEsAtacante
            ? $"Habéis interceptado a la flota de {defensor.NombrePropietario}. ¿Atacáis o la dejáis pasar, capitán?"
            : $"Una flota pirata de {atacante.NombrePropietario} os intercepta. ¿Combatís o huís, capitán?";

        // Renombrar el botón secundario según el rol del jugador
        TextMeshProUGUI txtHuir = _btnHuir.GetComponentInChildren<TextMeshProUGUI>();
        if (txtHuir != null) txtHuir.text = _jugadorEsAtacante ? "Dejar Pasar" : "Huir";

        _btnLuchar.gameObject.SetActive(true);
        _btnHuir.gameObject.SetActive(true);
    }

    // ─── Callbacks de botones ─────────────────────────────────────────────────

    private void OnLuchar()
    {
        _panelEncuentro.SetActive(false);
        SceneController.SetPausa(false);

        // Iniciar combate asíncrono en el gestor; el resultado llegará por OnCombateJugadorTerminado
        GestorCombatesActivos.Instance?.IniciarCombate(_atacante, _defensor, esDelJugador: true);
    }

    private void OnHuir()
    {
        if (_jugadorEsAtacante)
        {
            // "Dejar Pasar": el jugador decide no atacar
            _panelEncuentro.SetActive(false);
            SceneController.SetPausa(false);
            MapamundiController.Instance?.ReanudarIcono(_atacante.Id);
            MapamundiController.Instance?.ReanudarIcono(_defensor.Id);
            CombateEventos.DispararFinCombate();
            return;
        }

        // Intentar huir: comparar velocidades
        FlotaRuntimeData flotaJugador = GameManager.Instance?.FlotaJugador?.ComoFlotaRuntime();
        float velJugador = flotaJugador?.VelocidadFlota ?? 0f;
        float velEnemigo = _atacante.VelocidadFlota;

        if (velJugador > velEnemigo)
        {
            // Huida exitosa
            _panelEncuentro.SetActive(false);
            SceneController.SetPausa(false);
            MapamundiController.Instance?.ReanudarIcono(_atacante.Id);
            MapamundiController.Instance?.ReanudarIcono(_defensor.Id);
            CombateEventos.DispararFinCombate();
        }
        else
        {
            // Huida fallida: avisar y combate automático tras 2s
            _btnLuchar.gameObject.SetActive(false);
            _btnHuir.gameObject.SetActive(false);
            if (_textoHuida != null)
                _textoHuida.text = "¡No podéis escapar! El enemigo es más rápido. El combate comienza...";
            else
                _textoNarrativo.text = "¡No podéis escapar! El combate comienza...";
            StartCoroutine(IniciarCombateTrasEspera());
        }
    }

    private IEnumerator IniciarCombateTrasEspera()
    {
        yield return new WaitForSecondsRealtime(2f);
        _panelEncuentro.SetActive(false);
        SceneController.SetPausa(false);
        GestorCombatesActivos.Instance?.IniciarCombate(_atacante, _defensor, esDelJugador: true);
    }

}
