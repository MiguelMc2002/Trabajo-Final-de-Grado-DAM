using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Fachada de escena que conecta la interfaz del mercado con el estado persistente
/// almacenado en <see cref="GameManager"/>. No guarda la lista de entradas internamente:
/// lee y escribe siempre sobre el diccionario <c>MercadosPorCiudad</c> del singleton,
/// lo que garantiza que el mercado de la ciudad sobrevive a los cambios de escena.
/// </summary>
/// <remarks>
/// Fórmula de precio:
/// <c>precio_actual = precioBase × (stockMaximo / Mathf.Max(stockActual, 1))</c>
/// — a menor stock, mayor precio; a mayor stock, menor precio.
/// </remarks>
public class MarketManager : MonoBehaviour
{
    // ─── Configuración ───────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject con el nombre de la ciudad y la lista inicial de bienes del mercado.
    /// Se usa solo si el mercado de esta ciudad todavía no está registrado en GameManager.
    /// </summary>
    [Header("Datos de ciudad")]
    public CiudadData DatosCiudad;

    // ─── Límites de precio ───────────────────────────────────────────────────

    // Precio mínimo: 50% del precio base (mercado saturado)
    private const float MultiplicadorPrecioMinimo = 0.5f;
    // Precio máximo: 500% del precio base (escasez extrema)
    private const float MultiplicadorPrecioMaximo = 5.0f;

    // ─── Eventos ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Se lanza cada vez que el stock o el precio de cualquier bien cambia en esta ciudad.
    /// La interfaz del mercado se suscribe a este evento para refrescar las filas afectadas.
    /// El parámetro es el bien cuyo estado ha cambiado, o <c>null</c> si cambiaron todos.
    /// </summary>
    public event Action<BienData> OnMercadoActualizado;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (DatosCiudad == null)
        {
            Debug.LogWarning("[MarketManager] DatosCiudad no asignado; el mercado no se inicializará.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[MarketManager] GameManager.Instance es null en Start. Asegúrate de que existe en la escena inicial.");
            return;
        }

        // Si el mercado de esta ciudad aún no está en GameManager, lo inicializamos desde el asset
        if (!GameManager.Instance.TieneMercado(DatosCiudad.IdCiudad))
        {
            List<EntradaMercado> copia = new List<EntradaMercado>(DatosCiudad.Mercado.Count);
            foreach (EntradaMercado origen in DatosCiudad.Mercado)
            {
                float precio = origen.Bien != null
                    ? origen.Bien.precioBase * ((float)origen.Bien.stockMaximo / Mathf.Max(origen.StockActual, 1))
                    : 0f;

                copia.Add(new EntradaMercado
                {
                    Bien             = origen.Bien,
                    StockActual      = origen.StockActual,
                    StockMax         = origen.StockMax,
                    ProduccionDiaria = origen.ProduccionDiaria,
                    ConsumoDiario    = origen.ConsumoDiario,
                    PrecioActual     = precio
                });
            }

            GameManager.Instance.RegistrarMercadoCiudad(DatosCiudad.IdCiudad, copia);
        }

        GameManager.Instance.OnMercadoCiudadActualizado += OnGameManagerMercadoActualizado;
    }

    private void OnEnable()
    {
        SimulacionTiempo.OnNuevoDia += AplicarTickDiario;
    }

    private void OnDisable()
    {
        SimulacionTiempo.OnNuevoDia -= AplicarTickDiario;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMercadoCiudadActualizado -= OnGameManagerMercadoActualizado;
    }

    // Reenvía el evento de GameManager como OnMercadoActualizado(BienData) para los
    // suscriptores de la UI, filtrando por la ciudad de este MarketManager.
    // Si bien == null (tick diario), dispara el evento una vez por cada bien para que
    // MarketRowUI —que filtra por identidad de BienData— refresque todas sus filas.
    private void OnGameManagerMercadoActualizado(int idCiudad, BienData bien)
    {
        if (DatosCiudad == null || idCiudad != DatosCiudad.IdCiudad) return;

        if (bien != null)
        {
            OnMercadoActualizado?.Invoke(bien);
        }
        else
        {
            IReadOnlyList<EntradaMercado> entradas = GetEntradas();
            foreach (EntradaMercado entrada in entradas)
                OnMercadoActualizado?.Invoke(entrada.Bien);
        }
    }

    // ─── Tick diario ─────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica la producción y el consumo diarios a cada bien del mercado de esta ciudad.
    /// Se invoca automáticamente mediante el evento <see cref="SimulacionTiempo.OnNuevoDia"/>.
    /// </summary>
    private void AplicarTickDiario()
    {
        if (DatosCiudad == null || GameManager.Instance == null) return;

        List<EntradaMercado> entradas = ObtenerListaEditable();
        if (entradas == null || entradas.Count == 0) return;

        foreach (EntradaMercado entrada in entradas)
        {
            entrada.StockActual += entrada.ProduccionDiaria;
            entrada.StockActual -= entrada.ConsumoDiario;
            entrada.StockActual  = Mathf.Max(0, entrada.StockActual);
            entrada.StockActual  = Mathf.Min(entrada.StockActual, entrada.StockMax);

            if (entrada.Bien != null)
                entrada.PrecioActual = CalcularPrecio(entrada.Bien, entrada.StockActual);
        }

        GameManager.Instance.NotificarMercadoActualizado(DatosCiudad.IdCiudad);
    }

    // ─── Inicialización externa (compatibilidad con CiudadController) ────────

    /// <summary>
    /// Inicializa el mercado con los datos de la ciudad indicada.
    /// Si el mercado ya está registrado en GameManager, solo actualiza la referencia
    /// local <c>DatosCiudad</c>. Si no está registrado, lo crea copiando los assets.
    /// </summary>
    /// <param name="datosCiudad">ScriptableObject de la ciudad cuyo mercado se debe cargar.</param>
    public void InicializarConCiudad(CiudadData datosCiudad)
    {
        if (datosCiudad == null) return;

        // Desuscribir del evento anterior si cambiamos de ciudad
        if (GameManager.Instance != null)
            GameManager.Instance.OnMercadoCiudadActualizado -= OnGameManagerMercadoActualizado;

        DatosCiudad = datosCiudad;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[MarketManager] GameManager.Instance es null en InicializarConCiudad.");
            return;
        }

        if (!GameManager.Instance.TieneMercado(DatosCiudad.IdCiudad))
        {
            List<EntradaMercado> copia = new List<EntradaMercado>(DatosCiudad.Mercado.Count);
            string nombreCiudad = DatosCiudad.NombreCiudad;
            Dictionary<BienData, bool> vistos = new Dictionary<BienData, bool>();

            foreach (EntradaMercado origen in DatosCiudad.Mercado)
            {
                if (origen.Bien == null)
                {
                    Debug.LogWarning($"[MarketManager:{nombreCiudad}] Entrada sin BienData asignado; se ignora.");
                    continue;
                }

                if (vistos.ContainsKey(origen.Bien))
                {
                    Debug.LogWarning($"[MarketManager:{nombreCiudad}] Bien duplicado '{origen.Bien.nombre}'; se usa la primera entrada.");
                    continue;
                }

                vistos[origen.Bien] = true;
                float precio = origen.Bien.precioBase * ((float)origen.Bien.stockMaximo / Mathf.Max(origen.StockActual, 1));

                copia.Add(new EntradaMercado
                {
                    Bien             = origen.Bien,
                    StockActual      = origen.StockActual,
                    StockMax         = origen.StockMax,
                    ProduccionDiaria = origen.ProduccionDiaria,
                    ConsumoDiario    = origen.ConsumoDiario,
                    PrecioActual     = precio
                });
            }

            GameManager.Instance.RegistrarMercadoCiudad(DatosCiudad.IdCiudad, copia);
            Debug.Log($"[MarketManager] Mercado de {nombreCiudad} creado con {copia.Count} bienes.");
        }
        else
        {
            Debug.Log($"[MarketManager] Mercado de {DatosCiudad.NombreCiudad} ya existía en GameManager; reutilizando.");
        }

        GameManager.Instance.OnMercadoCiudadActualizado += OnGameManagerMercadoActualizado;
    }

    // ─── Consulta ────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la lista completa de entradas del mercado de esta ciudad.
    /// Útil para que la interfaz construya todas las filas al abrir la pantalla de mercado.
    /// </summary>
    /// <returns>Lista de entradas del mercado (solo lectura), o lista vacía si no hay datos.</returns>
    public IReadOnlyList<EntradaMercado> GetEntradas()
    {
        if (DatosCiudad == null || GameManager.Instance == null) return new List<EntradaMercado>();
        return GameManager.Instance.GetEntradasMercado(DatosCiudad.IdCiudad) ?? new List<EntradaMercado>();
    }

    /// <summary>
    /// Devuelve el nombre de la ciudad cuyo mercado gestiona este componente.
    /// </summary>
    /// <returns>Nombre de la ciudad, o cadena vacía si no hay datos asignados.</returns>
    public string GetNombreCiudad()
    {
        return DatosCiudad != null ? DatosCiudad.NombreCiudad : string.Empty;
    }

    /// <summary>
    /// Devuelve el stock actual de un bien en este mercado.
    /// </summary>
    /// <param name="bien">Bien que se quiere consultar.</param>
    /// <returns>Unidades disponibles en la ciudad, o 0 si el bien no existe en este mercado.</returns>
    public int GetStockActual(BienData bien)
    {
        EntradaMercado entrada = BuscarEntrada(bien);
        return entrada?.StockActual ?? 0;
    }

    /// <summary>
    /// Devuelve el precio actual de un bien aplicando la fórmula de oferta y demanda.
    /// </summary>
    /// <param name="bien">Bien cuyo precio se consulta.</param>
    /// <returns>Precio en monedas de oro, o 0 si el bien no existe en este mercado.</returns>
    public float GetPrecioActual(BienData bien)
    {
        EntradaMercado entrada = BuscarEntrada(bien);
        return entrada?.PrecioActual ?? 0f;
    }

    // ─── Operaciones de comercio ─────────────────────────────────────────────

    /// <summary>
    /// Ejecuta la compra de un bien por parte del jugador en este mercado.
    /// Descuenta el coste del tesoro del jugador, reduce el stock de la ciudad
    /// y carga las unidades en la bodega del jugador.
    /// </summary>
    /// <param name="bien">Bien que se quiere comprar.</param>
    /// <param name="cantidad">Unidades a comprar (debe ser mayor que 0).</param>
    /// <returns><c>true</c> si la compra se realizó correctamente; <c>false</c> en caso contrario.</returns>
    public bool Comprar(BienData bien, int cantidad)
    {
        string nombreCiudad = DatosCiudad != null ? DatosCiudad.NombreCiudad : "?";

        if (cantidad <= 0)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] Cantidad de compra inválida: {cantidad}.");
            return false;
        }

        EntradaMercado entrada = BuscarEntrada(bien);
        if (entrada == null)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] El bien '{bien.nombre}' no está disponible en este mercado.");
            return false;
        }

        if (entrada.StockActual < cantidad)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] Stock insuficiente de '{bien.nombre}'. Disponible: {entrada.StockActual}, solicitado: {cantidad}.");
            return false;
        }

        long costeTotal = (long)Mathf.Ceil(entrada.PrecioActual * cantidad);
        if (!GameManager.Instance.ModificarDinero(-costeTotal))
            return false;

        if (!GameManager.Instance.ModificarCantidadBien(bien, cantidad))
        {
            GameManager.Instance.ModificarDinero(costeTotal);
            return false;
        }

        entrada.StockActual -= cantidad;
        entrada.PrecioActual = CalcularPrecio(bien, entrada.StockActual);
        Debug.Log($"[MarketManager:{nombreCiudad}] Compra: {cantidad}× '{bien.nombre}' por {costeTotal} monedas.");

        GameManager.Instance.NotificarMercadoActualizado(DatosCiudad.IdCiudad, bien);
        return true;
    }

    /// <summary>
    /// Ejecuta la venta de un bien del jugador en este mercado.
    /// Ingresa el precio en el tesoro del jugador, aumenta el stock de la ciudad
    /// y retira las unidades de la bodega del jugador.
    /// </summary>
    /// <param name="bien">Bien que se quiere vender.</param>
    /// <param name="cantidad">Unidades a vender (debe ser mayor que 0).</param>
    /// <returns><c>true</c> si la venta se realizó correctamente; <c>false</c> en caso contrario.</returns>
    public bool Vender(BienData bien, int cantidad)
    {
        string nombreCiudad = DatosCiudad != null ? DatosCiudad.NombreCiudad : "?";

        if (cantidad <= 0)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] Cantidad de venta inválida: {cantidad}.");
            return false;
        }

        EntradaMercado entrada = BuscarEntrada(bien);
        if (entrada == null)
        {
            Debug.LogWarning($"[MarketManager:{nombreCiudad}] El bien '{bien.nombre}' no se puede vender en este mercado.");
            return false;
        }

        if (!GameManager.Instance.ModificarCantidadBien(bien, -cantidad))
            return false;

        long ingresoTotal = (long)Mathf.Floor(entrada.PrecioActual * cantidad);
        GameManager.Instance.ModificarDinero(ingresoTotal);

        entrada.StockActual = Mathf.Min(entrada.StockActual + cantidad, bien.stockMaximo);
        entrada.PrecioActual = CalcularPrecio(bien, entrada.StockActual);
        Debug.Log($"[MarketManager:{nombreCiudad}] Venta: {cantidad}× '{bien.nombre}' por {ingresoTotal} monedas.");

        GameManager.Instance.NotificarMercadoActualizado(DatosCiudad.IdCiudad, bien);
        return true;
    }

    // ─── Helpers privados ────────────────────────────────────────────────────

    // Devuelve la lista editable (no IReadOnly) del estado de GameManager para esta ciudad.
    private List<EntradaMercado> ObtenerListaEditable()
    {
        if (DatosCiudad == null || GameManager.Instance == null) return null;
        GameManager.Instance.MercadosPorCiudad.TryGetValue(DatosCiudad.IdCiudad, out List<EntradaMercado> lista);
        return lista;
    }

    // Busca la EntradaMercado de un bien concreto en el estado de GameManager.
    private EntradaMercado BuscarEntrada(BienData bien)
    {
        List<EntradaMercado> entradas = ObtenerListaEditable();
        if (entradas == null) return null;

        foreach (EntradaMercado entrada in entradas)
        {
            if (entrada.Bien == bien) return entrada;
        }
        return null;
    }

    // ─── Precio ──────────────────────────────────────────────────────────────

    private float CalcularPrecio(BienData bien, int stockActual)
    {
        float precio = bien.precioBase * ((float)bien.stockMaximo / Mathf.Max(stockActual, 1));
        return Mathf.Clamp(precio, bien.precioBase * MultiplicadorPrecioMinimo, bien.precioBase * MultiplicadorPrecioMaximo);
    }
}
