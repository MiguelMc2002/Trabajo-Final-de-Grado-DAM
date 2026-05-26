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

    /// <summary>
    /// Panel de resultados post-combate. Cuando está asignado, <see cref="MostrarResultado"/>
    /// delega en él en lugar de limitarse al log de diagnóstico.
    /// </summary>
    [SerializeField] private ResultadoCombateUI  _resultadoCombateUI;

    private FlotaRuntimeData _atacante;
    private FlotaRuntimeData _defensor;
    private ResultadoCombate _ultimoResultado;

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
        _atacante = atacante;
        _defensor = defensor;

        _panelEncuentro.SetActive(true);
        SceneController.SetPausa(true);

        _textoNarrativo.text = $"Una flota pirata de {atacante.NombrePropietario} os intercepta. ¿Combatís o huís, capitán?";
        _btnLuchar.gameObject.SetActive(true);
        _btnHuir.gameObject.SetActive(true);
    }

    // ─── Callbacks de botones ─────────────────────────────────────────────────

    private void OnLuchar()
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
    /// Cierra el panel de encuentro, reanuda el tiempo y delega el resultado en
    /// <see cref="ResultadoCombateUI"/> si está asignado; si no, vuelca al log como
    /// fallback de diagnóstico.
    /// </summary>
    private void MostrarResultado()
    {
        _panelEncuentro.SetActive(false);
        SceneController.SetPausa(false);

        if (_atacante != null && _atacante.EstaDestruida() && FlotaManager.Instance != null)
            FlotaManager.Instance.EliminarFlota(_atacante.Id);

        if (_resultadoCombateUI != null)
            _resultadoCombateUI.MostrarResultado(_ultimoResultado);
        else
        {
            Debug.LogWarning("[EncuentroNavalUI] _resultadoCombateUI no asignado. Resultado solo en log.");
            CombateEventos.DispararFinCombate();
        }

        ResultadoCombate r = _ultimoResultado;
        Debug.Log($"[EncuentroNaval] {r.TextoNarrativo}");
        Debug.Log($"[EncuentroNaval] JugadorGana={r.JugadorGana} | JugadorHuyo={r.JugadorHuyo}");
        Debug.Log($"[EncuentroNaval] Daño atacante={r.DanioRecibidoAtacante:F1} ({r.BarcosPerdidosAtacante} barcos) " +
                  $"| Daño defensor={r.DanioRecibidoDefensor:F1} ({r.BarcosPerdidosDefensor} barcos)");
        Debug.Log($"[EncuentroNaval] BotínOro={r.BotinOro} | Mercancías capturadas={r.BotinMercancia.Count} tipos");
    }
}
