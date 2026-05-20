using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel modal que muestra el resultado de un combate naval al jugador.
/// Se activa mediante <see cref="MostrarResultado"/> y pausa el tiempo mientras está visible.
/// Adjuntar a un GameObject en la escena de combate y cablear los campos desde el Inspector.
/// </summary>
public class ResultadoCombateUI : MonoBehaviour
{
    [SerializeField] private GameObject      _panelResultado;
    [SerializeField] private TextMeshProUGUI _txtTitulo;
    [SerializeField] private TextMeshProUGUI _txtNarrativo;
    [SerializeField] private TextMeshProUGUI _txtBotin;
    [SerializeField] private TextMeshProUGUI _txtBajas;
    [SerializeField] private Button          _btnContinuar;

    private void Awake()
    {
        if (_panelResultado != null)
            _panelResultado.SetActive(false);
        else
            Debug.LogWarning("[ResultadoCombateUI] _panelResultado no asignado en el Inspector.");

        if (_btnContinuar != null)
            _btnContinuar.onClick.AddListener(OcultarResultado);
        else
            Debug.LogWarning("[ResultadoCombateUI] _btnContinuar no asignado en el Inspector.");
    }

    /// <summary>
    /// Abre el panel de resultados, pausa el tiempo y rellena todos los textos
    /// con los datos del <paramref name="resultado"/> recibido.
    /// Llamar desde <c>EncuentroNavalUI</c> tras resolver el combate.
    /// </summary>
    /// <param name="resultado">Datos del combate ya resuelto. No puede ser null.</param>
    public void MostrarResultado(ResultadoCombate resultado)
    {
        if (resultado == null)
        {
            Debug.LogWarning("[ResultadoCombateUI] MostrarResultado recibió un resultado null.");
            return;
        }

        if (_panelResultado != null)
            _panelResultado.SetActive(true);

        Time.timeScale = 0f;

        ActualizarTitulo(resultado);
        ActualizarNarrativo(resultado);
        ActualizarBotin(resultado);
        ActualizarBajas(resultado);
    }

    /// <summary>
    /// Cierra el panel de resultados y reanuda el tiempo.
    /// Vinculado al botón Continuar en <see cref="Awake"/>.
    /// </summary>
    public void OcultarResultado()
    {
        if (_panelResultado != null)
            _panelResultado.SetActive(false);

        Time.timeScale = 1f;
    }

    // ─── Helpers de actualización ─────────────────────────────────────────────

    private void ActualizarTitulo(ResultadoCombate resultado)
    {
        if (_txtTitulo == null)
        {
            Debug.LogWarning("[ResultadoCombateUI] _txtTitulo no asignado.");
            return;
        }

        if (resultado.JugadorHuyo)
            _txtTitulo.text = "HUIDA EXITOSA";
        else if (resultado.JugadorGana)
            _txtTitulo.text = "VICTORIA";
        else
            _txtTitulo.text = "DERROTA";
    }

    private void ActualizarNarrativo(ResultadoCombate resultado)
    {
        if (_txtNarrativo == null)
        {
            Debug.LogWarning("[ResultadoCombateUI] _txtNarrativo no asignado.");
            return;
        }

        _txtNarrativo.text = resultado.TextoNarrativo;
    }

    private void ActualizarBotin(ResultadoCombate resultado)
    {
        if (_txtBotin == null)
        {
            Debug.LogWarning("[ResultadoCombateUI] _txtBotin no asignado.");
            return;
        }

        bool hayBotin = resultado.JugadorGana
            && (resultado.BotinOro > 0 || (resultado.BotinMercancia != null && resultado.BotinMercancia.Count > 0));

        _txtBotin.gameObject.SetActive(hayBotin);

        if (!hayBotin) return;

        var sb = new StringBuilder();
        sb.Append($"Botín: {resultado.BotinOro} oro");

        if (resultado.BotinMercancia != null)
        {
            foreach (KeyValuePair<int, int> entrada in resultado.BotinMercancia)
                sb.Append($"\n  + {entrada.Value}x bien_{entrada.Key}");
        }

        _txtBotin.text = sb.ToString();
    }

    private void ActualizarBajas(ResultadoCombate resultado)
    {
        if (_txtBajas == null)
        {
            Debug.LogWarning("[ResultadoCombateUI] _txtBajas no asignado.");
            return;
        }

        _txtBajas.text =
            $"Tus bajas: {resultado.BarcosPerdidosDefensor} barcos, {resultado.DanioRecibidoDefensor:F0} daño\n" +
            $"Bajas enemigas: {resultado.BarcosPerdidosAtacante} barcos, {resultado.DanioRecibidoAtacante:F0} daño";
    }
}
