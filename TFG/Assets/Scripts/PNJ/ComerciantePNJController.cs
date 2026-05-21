using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controlador puro (no MonoBehaviour) que gobierna el ciclo de vida
/// de una flota PNJ comerciante mediante una máquina de estados.
/// Se instancia por cada flota registrada en <see cref="FlotaManager"/>
/// y avanza un paso por día de juego al invocar <see cref="Tick"/>.
/// </summary>
public class ComerciantePNJController
{
    private readonly FlotaRuntimeData _flota;
    private readonly FlotaManager _manager;

    private int _diasRestantesViaje;
    private readonly Dictionary<int, double> _precioCompra;

    /// <summary>
    /// Historial de las últimas 2 ciudades visitadas como destino.
    /// Evita que el comerciante repita ciclos cortos A→B→A→B indefinidamente.
    /// </summary>
    private readonly Queue<int> _historialCiudades = new Queue<int>(2);

    /// <summary>
    /// Inicializa el controlador vinculándolo a una flota y a su gestor.
    /// </summary>
    /// <param name="flota">Datos de runtime de la flota que este controlador gobierna.</param>
    /// <param name="manager">Gestor central de flotas PNJ, usado para aplicar transiciones de estado.</param>
    public ComerciantePNJController(FlotaRuntimeData flota, FlotaManager manager)
    {
        _flota        = flota;
        _manager      = manager;
        _precioCompra = new Dictionary<int, double>();
    }

    /// <summary>
    /// Avanza la lógica de comportamiento de la flota un día de juego.
    /// Delega en el método privado correspondiente al estado actual de la flota.
    /// Debe llamarse desde <see cref="FlotaManager.TickTodosLosControladores"/> cada vez que <c>OnNuevoDia</c> se dispare.
    /// </summary>
    public void Tick()
    {
        switch (_flota.EstadoActual)
        {
            case EstadoFlotaPNJ.EnPuerto:
                TickEnPuerto();
                break;
            case EstadoFlotaPNJ.Viajando:
                TickViajando();
                break;
            case EstadoFlotaPNJ.Comerciando:
                TickComerciando();
                break;
            case EstadoFlotaPNJ.Huyendo:
                TickHuyendo();
                break;
            case EstadoFlotaPNJ.Patrullando:
                // TODO Día 21: lógica de patrulla pirata con memoria de casillas
                break;
            case EstadoFlotaPNJ.Interceptando:
                // TODO Día 21: persecución activa de comerciante objetivo
                break;
            case EstadoFlotaPNJ.HuyendoAPuerto:
                // TODO Día 21: comerciante busca ciudad más cercana
                break;
            case EstadoFlotaPNJ.EsperandoEnPuerto:
                // TODO Día 21: comprobación diaria de proximidad pirata
                break;
        }
    }

    // ─── Estados ─────────────────────────────────────────────────────────────

    private void TickEnPuerto()
    {
        if (_flota.IsPirata) { TickPatrullaPirata(); return; }

        if (_flota.TieneCarga())
        {
            CambiarEstado(EstadoFlotaPNJ.Comerciando);
            return;
        }

        // ── PASO 1: Seleccionar bien y destino más rentable ───────────────────

        int idBienSeleccionado   = -1;
        double mejorMargen       = 0;
        int mejorCiudadDestinoId = -1;
        var ciudadesDisponibles  = GameManager.Instance.CiudadesDisponibles;

        for (int idBien = 1; idBien <= GameManager.Instance.CatalogoBienes.Count; idBien++)
        {
            float precioOrigen = EstimarPrecioMercado(_flota.CiudadOrigenId, idBien);
            if (precioOrigen <= 0f) continue;

            float mejorPrecioDestino = 0f;
            int   mejorDestinoBien   = -1;

            foreach (CiudadData ciudad in ciudadesDisponibles)
            {
                if (ciudad.IdCiudad == _flota.CiudadOrigenId) continue;

                if (_historialCiudades.Contains(ciudad.IdCiudad)) continue;

                int flotasEnRuta = _manager.ContarFlotasEnRutaHacia(ciudad.IdCiudad, idBien);
                if (flotasEnRuta >= 2) continue;

                float precioDestino = EstimarPrecioMercado(ciudad.IdCiudad, idBien);
                if (precioDestino > mejorPrecioDestino)
                {
                    mejorPrecioDestino = precioDestino;
                    mejorDestinoBien   = ciudad.IdCiudad;
                }
            }

            if (mejorDestinoBien == -1) continue;

            double margen = mejorPrecioDestino - precioOrigen;

            if (margen > mejorMargen)
            {
                mejorMargen          = margen;
                idBienSeleccionado   = idBien;
                mejorCiudadDestinoId = mejorDestinoBien;
            }
        }

        if (idBienSeleccionado == -1 || mejorCiudadDestinoId == -1)
        {
            // Sin margen positivo en ninguna ruta: viaja en vacío a una ciudad aleatoria
            // para que el mercado del día siguiente pueda ofrecer mejores oportunidades.
            var ciudadesAlternativas = ciudadesDisponibles
                .Where(c => c.IdCiudad != _flota.CiudadOrigenId)
                .ToList();

            if (ciudadesAlternativas.Count == 0)
            {
                return;
            }

            var destino = ciudadesAlternativas[UnityEngine.Random.Range(0, ciudadesAlternativas.Count)];

            {
                RutaCalculadorTilemap calculador = Object.FindFirstObjectByType<RutaCalculadorTilemap>();
                CiudadData ciudadOrigen1  = ciudadesDisponibles.FirstOrDefault(c => c.IdCiudad == _flota.CiudadOrigenId);
                int diasViaje1 = 3;
                if (calculador != null && ciudadOrigen1 != null)
                {
                    List<Vector3Int> ruta = calculador.CalcularRutaConRuido(ciudadOrigen1.CasillaMapamundi, destino.CasillaMapamundi);
                    _flota.RutaActualTilemap = ruta;
                    diasViaje1 = ruta.Count > 0 ? Mathf.Max(1, ruta.Count / 5) : 3;
                }
                IniciarViaje(destino.IdCiudad, new Dictionary<int, double>(), diasViaje: diasViaje1);
            }
            return;
        }

        // ── PASO 2: Verificar stock real en mercado ───────────────────────────

        EntradaMercado entrada = ObtenerEntrada(_flota.CiudadOrigenId, idBienSeleccionado);

        if (entrada == null || entrada.StockActual <= 0)
        {
            // Sin stock en ciudad actual: viajar en vacío a la ciudad con stock del bien seleccionado.
            CiudadData ciudadConStock = null;
            foreach (CiudadData candidato in ciudadesDisponibles
                .Where(c => c.IdCiudad != _flota.CiudadOrigenId)
                .OrderByDescending(c => EstimarPrecioMercado(c.IdCiudad, idBienSeleccionado)))
            {
                EntradaMercado entradaCandidato = ObtenerEntrada(candidato.IdCiudad, idBienSeleccionado);
                if (entradaCandidato != null && entradaCandidato.StockActual > 0)
                {
                    ciudadConStock = candidato;
                    break;
                }
            }

            if (ciudadConStock == null)
            {
                ciudadConStock = ciudadesDisponibles
                    .Where(c => c.IdCiudad != _flota.CiudadOrigenId)
                    .OrderBy(_ => UnityEngine.Random.value)
                    .FirstOrDefault();
            }

            if (ciudadConStock == null) return;

            RutaCalculadorTilemap calc = Object.FindFirstObjectByType<RutaCalculadorTilemap>();
            CiudadData origen = ciudadesDisponibles.FirstOrDefault(c => c.IdCiudad == _flota.CiudadOrigenId);
            int dias = 3;
            if (calc != null && origen != null)
            {
                List<Vector3Int> ruta = calc.CalcularRutaConRuido(origen.CasillaMapamundi, ciudadConStock.CasillaMapamundi);
                _flota.RutaActualTilemap = ruta;
                dias = ruta.Count > 0 ? Mathf.Max(1, ruta.Count / 5) : 3;
            }
            IniciarViaje(ciudadConStock.IdCiudad, new Dictionary<int, double>(), diasViaje: dias);
            return;
        }

        int cantidad = Mathf.Min(entrada.StockActual, 10);

        // ── PASO 3: Compra simulada ───────────────────────────────────────────

        if (entrada.StockActual < cantidad)
            return;

        entrada.StockActual -= cantidad;
        GameManager.Instance.NotificarMercadoActualizado(_flota.CiudadOrigenId, entrada.Bien);

        _flota.Carga[idBienSeleccionado] = cantidad;

        // ── PASO 4: Iniciar viaje ─────────────────────────────────────────────

        if (mejorCiudadDestinoId == _flota.CiudadOrigenId)
        {
            CiudadData alternativa = ciudadesDisponibles
                .FirstOrDefault(c => c.IdCiudad != _flota.CiudadOrigenId);

            if (alternativa == null)
                return;

            mejorCiudadDestinoId = alternativa.IdCiudad;
        }

        {
            RutaCalculadorTilemap calculador = Object.FindFirstObjectByType<RutaCalculadorTilemap>();
            CiudadData ciudadOrigen2  = ciudadesDisponibles.FirstOrDefault(c => c.IdCiudad == _flota.CiudadOrigenId);
            CiudadData ciudadDestino2 = ciudadesDisponibles.FirstOrDefault(c => c.IdCiudad == mejorCiudadDestinoId);
            int diasViaje2 = 3;
            if (calculador != null && ciudadOrigen2 != null && ciudadDestino2 != null)
            {
                List<Vector3Int> ruta = calculador.CalcularRutaConRuido(ciudadOrigen2.CasillaMapamundi, ciudadDestino2.CasillaMapamundi);
                _flota.RutaActualTilemap = ruta;
                diasViaje2 = ruta.Count > 0 ? Mathf.Max(1, ruta.Count / 5) : 3;
            }
            IniciarViaje(
                mejorCiudadDestinoId,
                new Dictionary<int, double> { { idBienSeleccionado, (double)entrada.PrecioActual } },
                diasViaje: diasViaje2
            );
        }
    }

    private void TickViajando()
    {
        _diasRestantesViaje--;

        if (_diasRestantesViaje <= 0)
        {
            _flota.CiudadOrigenId    = _flota.CiudadDestinoId;
            if (_historialCiudades.Count >= 2) _historialCiudades.Dequeue();
            _historialCiudades.Enqueue(_flota.CiudadDestinoId);

            if (_flota.IsPirata)
                CambiarEstado(EstadoFlotaPNJ.Patrullando);
            else
                CambiarEstado(EstadoFlotaPNJ.Comerciando);
        }
    }

    private void TickComerciando()
    {
        if (_flota.IsPirata) { TickPatrullaPirata(); return; }

        if (!_flota.TieneCarga())
        {
            CambiarEstado(EstadoFlotaPNJ.EnPuerto);
            return;
        }

        var bienesParaEliminar = new List<int>();

        foreach (int idBien in new List<int>(_flota.Carga.Keys))
        {
            int cantidad = _flota.Carga[idBien];

            EntradaMercado entrada = ObtenerEntrada(_flota.CiudadOrigenId, idBien);

            if (entrada == null)
            {
                bienesParaEliminar.Add(idBien);
                continue;
            }

            _precioCompra.TryGetValue(idBien, out double precioCompraRegistrado);

            entrada.StockActual += cantidad;
            GameManager.Instance.NotificarMercadoActualizado(_flota.CiudadOrigenId, entrada.Bien);

            bienesParaEliminar.Add(idBien);
        }

        foreach (int idBien in bienesParaEliminar)
        {
            _flota.Carga.Remove(idBien);
            _precioCompra.Remove(idBien);
        }

        CambiarEstado(EstadoFlotaPNJ.EnPuerto);
    }

    private void TickHuyendo()
    {
        EstadoFlotaPNJ siguienteEstado = _flota.IsPirata ? EstadoFlotaPNJ.Patrullando : EstadoFlotaPNJ.EnPuerto;
        CambiarEstado(siguienteEstado);
    }

    private void TickPatrullaPirata()
    {
        _diasRestantesViaje    = UnityEngine.Random.Range(3, 8);
        _flota.CiudadDestinoId = -1;
        _flota.RutaActualTilemap = new System.Collections.Generic.List<UnityEngine.Vector3Int>();
        CambiarEstado(EstadoFlotaPNJ.Patrullando);
        // TODO Día 21: ruta real hacia casillas de mar con sesgo a rutas comerciales transitadas
    }

    // ─── Transiciones ────────────────────────────────────────────────────────

    private void CambiarEstado(EstadoFlotaPNJ nuevoEstado)
    {
        _manager.CambiarEstado(_flota.Id, nuevoEstado);
    }

    // ─── Helpers de inicio de viaje ──────────────────────────────────────────

    /// <summary>
    /// Inicia un viaje hacia una ciudad destino, fijando los días de tránsito y
    /// registrando el precio de compra de cada bien cargado.
    /// Debe llamarse desde fuera al asignar un destino a la flota.
    /// </summary>
    /// <param name="ciudadDestinoId">Identificador de la ciudad a la que viajará la flota.</param>
    /// <param name="preciosCompra">Precios pagados por cada bien cargado (idBien → precio).</param>
    /// <param name="diasViaje">Duración del trayecto en días de juego (por defecto 3).</param>
    public void IniciarViaje(int ciudadDestinoId, Dictionary<int, double> preciosCompra, int diasViaje = 3)
    {
        _flota.CiudadDestinoId  = ciudadDestinoId;
        _diasRestantesViaje     = diasViaje;

        _precioCompra.Clear();
        foreach (var kvp in preciosCompra)
            _precioCompra[kvp.Key] = kvp.Value;

        CambiarEstado(EstadoFlotaPNJ.Viajando);
    }

    // ─── Helpers privados ────────────────────────────────────────────────────

    /// <summary>
    /// Estima el precio actual de un bien en una ciudad consultando el mercado en memoria.
    /// El error de estimación depende de la inteligencia comercial del comerciante:
    /// un comerciante inteligente (1.0) estima con ±5% de error;
    /// uno poco hábil (0.1) puede equivocarse hasta ±38%.
    /// Fórmula: ruido = Lerp(0.40, 0.05, inteligencia); precio ∈ [real-ruido, real+ruido].
    /// </summary>
    /// <param name="idCiudad">Ciudad cuyo precio se quiere estimar.</param>
    /// <param name="idBien">Identificador 1-based del bien.</param>
    /// <returns>Precio estimado con ruido proporcional a la inteligencia del comerciante.</returns>
    private float EstimarPrecioMercado(int idCiudad, int idBien)
    {
        EntradaMercado entrada = ObtenerEntrada(idCiudad, idBien);
        if (entrada == null) return 0f;

        float precioReal = entrada.PrecioActual;
        float ruido      = UnityEngine.Mathf.Lerp(0.40f, 0.05f, _flota.InteligenciaComercial);
        float rango      = precioReal * ruido;
        return UnityEngine.Random.Range(precioReal - rango, precioReal + rango);
    }

    /// <summary>
    /// Busca la <see cref="EntradaMercado"/> de un bien concreto en el mercado de una ciudad,
    /// resolviendo el <see cref="BienData"/> a partir del índice del catálogo.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad cuyo mercado se consulta.</param>
    /// <param name="idBien">Índice 1-based del bien en <see cref="GameManager.CatalogoBienes"/>.</param>
    /// <returns>La entrada del mercado correspondiente, o <c>null</c> si no existe.</returns>
    private EntradaMercado ObtenerEntrada(int idCiudad, int idBien)
    {
        var entradas = GameManager.Instance.GetEntradasMercado(idCiudad);
        if (entradas == null) return null;

        int indice = idBien - 1;
        if (indice < 0 || indice >= GameManager.Instance.CatalogoBienes.Count)
            return null;
        BienData bien = GameManager.Instance.CatalogoBienes[indice];
        return entradas.FirstOrDefault(e => e.Bien == bien);
    }
}

