using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDDinero : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtDinero;
    [SerializeField] private Image _imgMoneda;

    private void OnEnable() => RefrescarDinero();
    private void Update()   => RefrescarDinero();

    private void RefrescarDinero()
    {
        if (_txtDinero == null || GameManager.Instance == null) return;
        _txtDinero.text = $"{GameManager.Instance.Dinero:N0} monedas";
    }
}
