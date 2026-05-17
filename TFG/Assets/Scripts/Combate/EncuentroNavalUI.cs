using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour que gestiona el panel de encuentro naval del jugador.
/// Se suscribe a <see cref="CombateEventos.OnCombateIniciado"/>, muestra el panel
/// cuando el jugador está involucrado y delega la resolución en <see cref="CombateNavalResolver"/>.
/// Adjuntar a un GameObject persistente en la escena Mapamundi y asignar los campos desde el Inspector.
/// </summary>
public class EncuentroNavalUI : MonoBehaviour
{
    [SerializeField] private GameObject          _panelEncuentro;
    [SerializeField] private TextMeshProUGUI     _textoNarrativo;
    [SerializeField] private Button              _btnAtacar;
    [SerializeField] private Button              _btnHuir;
    [SerializeField] private Button              _btnDefender;

    private FlotaRuntimeData _atacante;
    private FlotaRuntimeData _defensor;
    private bool             _jugadorEsAtacante;
    private ResultadoCombate _ultimoResultado;

    private void Start()
    {
        _btnAtacar.onClick.AddListener(OnAtacar);
        _btnHuir.onClick.AddListener(OnHuir);
        _btnDefender.onClick.AddListener(OnDefender);

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
        _atacante = atacante;
        _defensor = defensor;

        // FlotaJugador no existe todavía: tratar siempre al jugador como defensor por defecto
        _jugadorEsAtacante = false;

        _panelEncuentro.SetActive(true);
        SceneController.SetPausa(true);

        if (_jugadorEsAtacante)
        {
            _textoNarrativo.text = $"Habéis avistado la flota de {defensor.NombrePropietario}. ¿Atacáis o huís, capitán?";
            _btnAtacar.gameObject.SetActive(true);
            _btnHuir.gameObject.SetActive(true);
            _btnDefender.gameObject.SetActive(false);
        }
        else
        {
            _textoNarrativo.text = "Una flota pirata os intercepta en alta mar. ¿Qué ordenáis, capitán?";
            _btnAtacar.gameObject.SetActive(false);
            _btnHuir.gameObject.SetActive(false);
            _btnDefender.gameObject.SetActive(true);
        }
    }

    // ─── Callbacks de botones ─────────────────────────────────────────────────

    private void OnAtacar()
    {
        _ultimoResultado = CombateNavalResolver.Resolver(_atacante, _defensor, jugadorEsAtacante: true);
        MostrarResultado();
    }

    private void OnDefender()
    {
        _ultimoResultado = CombateNavalResolver.Resolver(_atacante, _defensor, jugadorEsAtacante: false);
        MostrarResultado();
    }

    private void OnHuir()
    {
        _ultimoResultado = CombateNavalResolver.Resolver(_atacante, _defensor, jugadorEsAtacante: true, jugadorIntentaHuir: true);
        MostrarResultado();
    }

    // ─── Resultado ────────────────────────────────────────────────────────────

    /// <summary>
    /// Cierra el panel de encuentro, reanuda el tiempo y vuelca el resultado al log.
    /// El panel de resultados visual se implementará en la siguiente sesión.
    /// </summary>
    private void MostrarResultado()
    {
        _panelEncuentro.SetActive(false);
        SceneController.SetPausa(false);

        ResultadoCombate r = _ultimoResultado;
        Debug.Log($"[EncuentroNaval] {r.TextoNarrativo}");
        Debug.Log($"[EncuentroNaval] JugadorGana={r.JugadorGana} | JugadorHuyo={r.JugadorHuyo}");
        Debug.Log($"[EncuentroNaval] Daño atacante={r.DanioRecibidoAtacante:F1} ({r.BarcosPerdidosAtacante} barcos) " +
                  $"| Daño defensor={r.DanioRecibidoDefensor:F1} ({r.BarcosPerdidosDefensor} barcos)");
        Debug.Log($"[EncuentroNaval] BotínOro={r.BotinOro} | Mercancías capturadas={r.BotinMercancia.Count} tipos");
    }
}
