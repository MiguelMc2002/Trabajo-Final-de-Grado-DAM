using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de UI del mercado que se muestra sobre la escena Ciudad como un diálogo modal.
/// Instancia una fila (<see cref="MarketRowUI"/>) por cada bien disponible, muestra la
/// cabecera con el nombre de la ciudad y el estado del almacén, y mantiene la interfaz
/// sincronizada con el <see cref="MarketManager"/> suscribiéndose a su evento
/// <see cref="MarketManager.OnMercadoActualizado"/>.
/// </summary>
public class MercadoUI : MonoBehaviour
{
    // ─── Referencias de escena ───────────────────────────────────────────────

    /// <summary>
    /// Gestor del mercado de la ciudad activa. Debe asignarse desde el Inspector.
    /// </summary>
    [Header("Mercado")]
    [SerializeField] private MarketManager _marketManager;

    /// <summary>
    /// Intermediario de comercio que valida y ejecuta las operaciones de compra y venta.
    /// Debe asignarse desde el Inspector junto al <see cref="MarketManager"/>.
    /// </summary>
    [SerializeField] private OficinaComercial _oficina;

    // ─── Prefab y contenedor de filas ────────────────────────────────────────

    /// <summary>
    /// Prefab que representa una fila del mercado (Assets/Prefabs/UI/MarketRow.prefab).
    /// Debe tener un componente <see cref="MarketRowUI"/> en su raíz.
    /// </summary>
    [Header("Filas del mercado")]
    [SerializeField] private GameObject _prefabMarketRow;

    /// <summary>
    /// Transform contenedor (p. ej. un VerticalLayoutGroup) donde se instancian
    /// las filas al activar el panel. Se destruyen al desactivarlo.
    /// </summary>
    [SerializeField] private Transform _contenedorFilas;

    // ─── Cabecera del panel ──────────────────────────────────────────────────

    /// <summary>Muestra el nombre de la ciudad cuyo mercado se está consultando.</summary>
    [Header("Cabecera")]
    [SerializeField] private TextMeshProUGUI _textoNombreCiudad;

    /// <summary>
    /// Muestra la capacidad usada del almacén del jugador.
    /// En la beta el formato es <c>{usado} / ∞</c>; en la release será <c>{usado} / {total}</c>.
    /// </summary>
    [SerializeField] private TextMeshProUGUI _textoCapacidadAlmacen;

    // ─── Botón de cierre ─────────────────────────────────────────────────────

    /// <summary>
    /// Botón que cierra el panel del mercado y devuelve la vista a la ciudad.
    /// Debe asignarse desde el Inspector.
    /// </summary>
    public Button BotonCerrar;

    // ─── Referencias cacheadas ───────────────────────────────────────────────

    private CiudadController _ciudadController;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _ciudadController = FindAnyObjectByType<CiudadController>();

        if (_ciudadController == null)
            Debug.LogWarning("[MercadoUI] No se encontró CiudadController en la escena.");

        if (BotonCerrar != null)
            BotonCerrar.onClick.AddListener(Cerrar);
        else
            Debug.LogWarning("[MercadoUI] BotonCerrar no asignado en el Inspector.");
    }

    private void OnEnable()
    {
        if (_marketManager == null)
        {
            Debug.LogError("[MercadoUI] No hay MarketManager asignado. Asígnalo desde el Inspector.");
            return;
        }

        if (_oficina == null)
        {
            Debug.LogError("[MercadoUI] No hay OficinaComercial asignada. Asígnala desde el Inspector.");
            return;
        }

        _oficina.Inicializar(_marketManager);
        _marketManager.OnMercadoActualizado += OnMercadoActualizado;
        ConstruirFilas();
        RefrescarCabecera();
    }

    private void OnDisable()
    {
        if (_marketManager != null)
            _marketManager.OnMercadoActualizado -= OnMercadoActualizado;

        // Destruir las filas instanciadas para que no se acumulen al reabrir el panel
        if (_contenedorFilas != null)
        {
            foreach (Transform hijo in _contenedorFilas)
                Destroy(hijo.gameObject);
        }
    }

    // ─── Construcción de la lista de bienes ──────────────────────────────────

    /// <summary>
    /// Instancia una fila del mercado por cada bien registrado en el <see cref="MarketManager"/>
    /// y la inicializa llamando a <see cref="MarketRowUI.Inicializar"/>.
    /// Se llama automáticamente al activar el panel desde <see cref="OnEnable"/>.
    /// </summary>
    private void ConstruirFilas()
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
            if (entrada.Bien == null)
            {
                Debug.LogWarning("[MercadoUI] Se encontró una entrada sin BienData; se omite.");
                continue;
            }

            GameObject fila = Instantiate(_prefabMarketRow, _contenedorFilas);
            MarketRowUI rowUI = fila.GetComponent<MarketRowUI>();

            if (rowUI == null)
            {
                Debug.LogWarning($"[MercadoUI] El prefab MarketRow no tiene componente MarketRowUI; fila de '{entrada.Bien.nombre}' ignorada.");
                continue;
            }

            rowUI.Inicializar(entrada.Bien, _marketManager, _oficina);
        }
    }

    // ─── Control del panel ───────────────────────────────────────────────────

    /// <summary>
    /// Cierra el panel del mercado delegando en <see cref="CiudadController.CerrarTodosPaneles"/>.
    /// Se registra como listener de <see cref="BotonCerrar"/> en <c>Awake</c>.
    /// </summary>
    public void Cerrar()
    {
        if (_ciudadController != null)
            _ciudadController.CerrarTodosPaneles();
        else
            gameObject.SetActive(false);
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
