using TMPro;
using UnityEngine;

/// <summary>
/// Gestiona la pantalla del mercado de una ciudad: instancia una fila
/// (<see cref="MarketRowUI"/>) por cada bien disponible, muestra la cabecera
/// con el nombre de la ciudad y el estado del almacén, y mantiene la interfaz
/// sincronizada con el <see cref="MarketManager"/> suscribiéndose a su evento
/// <see cref="MarketManager.OnMercadoActualizado"/>.
/// </summary>
public class MercadoUI : MonoBehaviour
{
    // ─── Referencias de escena ───────────────────────────────────────────────

    /// <summary>
    /// Gestor del mercado de la ciudad activa. Debe asignarse desde el Inspector
    /// o provenir del <see cref="SceneController"/> al abrir la pantalla.
    /// </summary>
    [Header("Mercado")]
    [SerializeField] private MarketManager _marketManager;

    // ─── Prefab y contenedor de filas ────────────────────────────────────────

    /// <summary>
    /// Prefab que representa una fila del mercado (Assets/Prefabs/UI/MarketRow.prefab).
    /// Debe tener un componente <see cref="MarketRowUI"/> en su raíz.
    /// </summary>
    [Header("Filas del mercado")]
    [SerializeField] private GameObject _prefabMarketRow;

    /// <summary>
    /// Transform contenedor (p. ej. un VerticalLayoutGroup) donde se instancian
    /// las filas al abrir la pantalla. Se destruyen al cerrarla.
    /// </summary>
    [SerializeField] private Transform _contenedorFilas;

    // ─── Cabecera de la pantalla ─────────────────────────────────────────────

    /// <summary>Muestra el nombre de la ciudad cuyo mercado se está consultando.</summary>
    [Header("Cabecera")]
    [SerializeField] private TextMeshProUGUI _textoNombreCiudad;

    /// <summary>
    /// Muestra la capacidad usada del almacén del jugador.
    /// En la beta el formato es <c>{usado} / ∞</c>; en la release será <c>{usado} / {total}</c>.
    /// </summary>
    [SerializeField] private TextMeshProUGUI _textoCapacidadAlmacen;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_marketManager == null)
        {
            Debug.LogError("[MercadoUI] No hay MarketManager asignado. Asígnalo desde el Inspector.");
            return;
        }

        InstanciarFilas();
        _marketManager.OnMercadoActualizado += OnMercadoActualizado;
        RefrescarCabecera();
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar referencias a objetos destruidos
        if (_marketManager != null)
            _marketManager.OnMercadoActualizado -= OnMercadoActualizado;
    }

    // ─── Construcción de la lista de bienes ──────────────────────────────────

    /// <summary>
    /// Instancia una fila del mercado por cada bien registrado en el <see cref="MarketManager"/>
    /// y la inicializa llamando a <see cref="MarketRowUI.Inicializar"/>.
    /// Se ejecuta una vez al abrir la pantalla.
    /// </summary>
    private void InstanciarFilas()
    {
        if (_prefabMarketRow == null)
        {
            Debug.LogError("[MercadoUI] No hay prefab MarketRow asignado.");
            return;
        }

        if (_contenedorFilas == null)
        {
            Debug.LogError("[MercadoUI] No hay contenedor de filas asignado.");
            return;
        }

        foreach (EntradaMercado entrada in _marketManager.GetEntradas())
        {
            if (entrada.bien == null)
            {
                Debug.LogWarning("[MercadoUI] Se encontró una entrada sin BienData; se omite.");
                continue;
            }

            GameObject fila = Instantiate(_prefabMarketRow, _contenedorFilas);
            MarketRowUI rowUI = fila.GetComponent<MarketRowUI>();

            if (rowUI == null)
            {
                Debug.LogWarning($"[MercadoUI] El prefab MarketRow no tiene componente MarketRowUI; fila de '{entrada.bien.nombre}' ignorada.");
                continue;
            }

            rowUI.Inicializar(entrada.bien, _marketManager);
        }
    }

    // ─── Cabecera ────────────────────────────────────────────────────────────

    /// <summary>
    /// Callback del evento <see cref="MarketManager.OnMercadoActualizado"/>.
    /// Refresca la cabecera cada vez que cambia el stock de cualquier bien,
    /// ya que el total del almacén puede haber variado tras una compra o venta.
    /// </summary>
    /// <param name="bienActualizado">Bien cuyo estado ha cambiado (no se usa directamente aquí).</param>
    private void OnMercadoActualizado(BienData bienActualizado)
    {
        RefrescarCabecera();
    }

    /// <summary>
    /// Actualiza el nombre de la ciudad y el indicador de capacidad del almacén del jugador.
    /// En la beta la capacidad máxima se muestra como ∞ (<see cref="GameManager.CapacidadAlmacen"/>
    /// equivale a <c>int.MaxValue</c>).
    /// </summary>
    private void RefrescarCabecera()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MercadoUI] GameManager.Instance es null; se omite el refresco de cabecera.");
            return;
        }

        if (_textoNombreCiudad != null)
            _textoNombreCiudad.text = _marketManager.GetNombreCiudad();

        if (_textoCapacidadAlmacen != null)
        {
            int usado = GameManager.Instance.GetTotalUnidadesAlmacen();
            bool esBeta = GameManager.CapacidadAlmacen == int.MaxValue;
            _textoCapacidadAlmacen.text = esBeta
                ? $"{usado:N0} / ∞"
                : $"{usado:N0} / {GameManager.CapacidadAlmacen:N0}";
        }
    }
}
