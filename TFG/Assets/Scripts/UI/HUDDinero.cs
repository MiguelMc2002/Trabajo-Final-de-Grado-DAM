using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el dinero del jugador en el HUD de las escenas Ciudad y Mapamundi.
/// Se actualiza mediante el evento estático <see cref="GameManager.OnDineroActualizado"/>
/// para evitar polling en Update. Formato: "999.999 ƒ" (separador de miles es-ES).
/// </summary>
public class HUDDinero : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtDinero;
    [SerializeField] private Image _imgMoneda;

    private static readonly CultureInfo _culturaES = CultureInfo.GetCultureInfo("es-ES");

    private void OnEnable()
    {
        GameManager.OnDineroActualizado += ActualizarTexto;
        RefrescarDinero();
    }

    private void OnDisable()
    {
        GameManager.OnDineroActualizado -= ActualizarTexto;
    }

    /// <summary>
    /// Desuscribe el componente del evento de dinero al ser destruido.
    /// </summary>
    private void OnDestroy()
    {
        GameManager.OnDineroActualizado -= ActualizarTexto;
    }

    private void ActualizarTexto(long dinero)
    {
        if (_txtDinero == null) return;
        _txtDinero.text = FormatearDinero(dinero);
    }

    private void RefrescarDinero()
    {
        if (_txtDinero == null || GameManager.Instance == null) return;
        _txtDinero.text = FormatearDinero(GameManager.Instance.Dinero);
    }

    /// <summary>
    /// Formatea una cantidad con separador de miles (es-ES) y símbolo de florín.
    /// Ejemplo: 999999 → "999.999 ƒ"
    /// </summary>
    /// <param name="cantidad">Cantidad de monedas a formatear.</param>
    /// <returns>Cadena con separador de miles y símbolo ƒ.</returns>
    public static string FormatearDinero(long cantidad)
    {
        return $"{cantidad.ToString("N0", _culturaES)} ƒ";
    }
}
