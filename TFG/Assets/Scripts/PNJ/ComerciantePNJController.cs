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
    private readonly MemoriaComercialPNJDAO _memoriaDAO;

    private int _diasRestantesViaje;
    private readonly Dictionary<int, double> _precioCompra;

    /// <summary>
    /// Inicializa el controlador vinculándolo a una flota, a su gestor y al DAO de memoria comercial.
    /// </summary>
    /// <param name="flota">Datos de runtime de la flota que este controlador gobierna.</param>
    /// <param name="manager">Gestor central de flotas PNJ, usado para aplicar transiciones de estado.</param>
    /// <param name="memoriaDAO">DAO de memoria comercial PNJ compartido con FlotaManager.</param>
    public ComerciantePNJController(FlotaRuntimeData flota, FlotaManager manager, MemoriaComercialPNJDAO memoriaDAO)
    {
        _flota        = flota;
        _manager      = manager;
        _memoriaDAO   = memoriaDAO;
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
        }
    }

    // ─── Estados ─────────────────────────────────────────────────────────────

    private void TickEnPuerto()
    {
        if (_flota.TieneCarga())
        {
            Debug.LogWarning($"[PNJ] {_flota.NombrePropietario} tiene carga sin vender en puerto. Redirigiendo a Comerciando.");
            CambiarEstado(EstadoFlotaPNJ.Comerciando);
            return;
        }

        var preciosVigentes = ObtenerSnapshotGlobal();

        if (preciosVigentes == null || preciosVigentes.Count == 0)
        {
            Debug.Log($"[PNJ] {_flota.NombrePropietario} no tiene información de mercado, esperando...");
            return;
        }

        Debug.Log($"[PNJ] {_flota.NombrePropietario} evaluando mercado con {preciosVigentes.Count} entradas conocidas...");

        // ── PASO 1: Seleccionar bien y destino más rentable ───────────────────

        int idBienSeleccionado             = -1;
        int ciudadDestinoId                = -1;
        double mejorMargen                 = 0;
        MemoriaComercialPNJDto mejorDestinoDto = null;

        var porBien = preciosVigentes.GroupBy(e => e.IdBien);

        foreach (var grupo in porBien)
        {
            var entradasGrupo = grupo.ToList();

            MemoriaComercialPNJDto precioOrigen = entradasGrupo
                .Where(e => e.IdCiudad == _flota.CiudadOrigenId)
                .OrderByDescending(e => e.DiaJuegoConocido)
                .FirstOrDefault();

            if (precioOrigen == null) continue;

            MemoriaComercialPNJDto mejorDestino = entradasGrupo
                .Where(e => e.IdCiudad != _flota.CiudadOrigenId)
                .OrderByDescending(e => e.PrecioConocido)
                .FirstOrDefault();

            if (mejorDestino == null) continue;

            double margen = mejorDestino.PrecioConocido - precioOrigen.PrecioConocido;
            if (margen > mejorMargen)
            {
                mejorMargen        = margen;
                idBienSeleccionado = grupo.Key;
                mejorDestinoDto    = mejorDestino;
            }
        }

        if (idBienSeleccionado == -1 || mejorDestinoDto == null)
        {
            // Sin margen positivo en ninguna ruta: viaja en vacío a una ciudad aleatoria
            // para que el snapshot del día siguiente pueda ofrecer mejores oportunidades.
            var ciudadesAlternativas = GameManager.Instance.CiudadesDisponibles
                .Where(c => c.IdCiudad != _flota.CiudadOrigenId)
                .ToList();

            if (ciudadesAlternativas.Count == 0)
            {
                Debug.Log($"[PNJ] {_flota.NombrePropietario} no hay ciudades alternativas disponibles.");
                return;
            }

            var destino = ciudadesAlternativas[UnityEngine.Random.Range(0, ciudadesAlternativas.Count)];
            Debug.Log($"[PNJ] {_flota.NombrePropietario} sin ruta rentable en ciudad {_flota.CiudadOrigenId}, viajando en vacío a ciudad {destino.IdCiudad}.");
            IniciarViaje(destino.IdCiudad, new Dictionary<int, double>(), diasViaje: 3);
            return;
        }

        // ── PASO 2: Verificar stock real en mercado ───────────────────────────

        EntradaMercado entrada = ObtenerEntrada(_flota.CiudadOrigenId, idBienSeleccionado);

        if (entrada == null || entrada.StockActual <= 0)
        {
            Debug.Log($"[PNJ] {_flota.NombrePropietario} sin stock de bien {idBienSeleccionado} en ciudad {_flota.CiudadOrigenId}.");
            return;
        }

        int cantidad = Mathf.Min(entrada.StockActual, 10);

        // ── PASO 3: Compra simulada ───────────────────────────────────────────

        if (entrada.StockActual < cantidad)
        {
            Debug.Log($"[PNJ] {_flota.NombrePropietario} falló compra de bien {idBienSeleccionado}.");
            return;
        }

        entrada.StockActual -= cantidad;
        GameManager.Instance.NotificarMercadoActualizado(_flota.CiudadOrigenId, entrada.Bien);

        _flota.Carga[idBienSeleccionado] = cantidad;
        Debug.Log($"[PNJ] {_flota.NombrePropietario} compró {cantidad}x bien {idBienSeleccionado} a {entrada.PrecioActual}g en ciudad {_flota.CiudadOrigenId}.");

        // ── PASO 4: Iniciar viaje ─────────────────────────────────────────────

        ciudadDestinoId = mejorDestinoDto.IdCiudad;

        if (ciudadDestinoId == _flota.CiudadOrigenId)
        {
            CiudadData alternativa = GameManager.Instance.CiudadesDisponibles
                .FirstOrDefault(c => c.IdCiudad != _flota.CiudadOrigenId);

            if (alternativa == null)
            {
                Debug.LogWarning($"[PNJ] {_flota.NombrePropietario} no encontró ciudad destino alternativa.");
                return;
            }

            ciudadDestinoId = alternativa.IdCiudad;
        }

        Debug.Log($"[PNJ] {_flota.NombrePropietario} partirá hacia ciudad {ciudadDestinoId}.");

        IniciarViaje(
            ciudadDestinoId,
            new Dictionary<int, double> { { idBienSeleccionado, (double)entrada.PrecioActual } },
            diasViaje: 3
        );
    }

    private void TickViajando()
    {
        _diasRestantesViaje--;

        if (_diasRestantesViaje <= 0)
        {
            _flota.CiudadOrigenId = _flota.CiudadDestinoId;
            Debug.Log($"[PNJ] {_flota.NombrePropietario} ha llegado a ciudad {_flota.CiudadDestinoId}.");
            CambiarEstado(EstadoFlotaPNJ.Comerciando);
        }
    }

    private void TickComerciando()
    {
        if (!_flota.TieneCarga())
        {
            Debug.Log($"[PNJ] {_flota.NombrePropietario} llegó sin carga a ciudad {_flota.CiudadOrigenId}.");
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
                Debug.Log($"[PNJ] bien {idBien} no existe en mercado de ciudad {_flota.CiudadOrigenId}, descartando.");
                bienesParaEliminar.Add(idBien);
                continue;
            }

            _precioCompra.TryGetValue(idBien, out double precioCompraRegistrado);

            if (entrada.PrecioActual > precioCompraRegistrado)
            {
                entrada.StockActual += cantidad;
                GameManager.Instance.NotificarMercadoActualizado(_flota.CiudadOrigenId, entrada.Bien);
                Debug.Log($"[PNJ] {_flota.NombrePropietario} vendió {cantidad}x bien {idBien} a {entrada.PrecioActual}g en ciudad {_flota.CiudadOrigenId}.");
                _memoriaDAO.GuardarMemoria(0, idBien, _flota.CiudadOrigenId, entrada.PrecioActual, GameManager.Instance.EstadoPartida.DiaJuego);
            }
            else
            {
                Debug.Log($"[PNJ] {_flota.NombrePropietario} vendió con pérdidas bien {idBien}: compró a {precioCompraRegistrado}g, precio actual {entrada.PrecioActual}g. Vendiendo igualmente para liberar bodega.");
                entrada.StockActual += cantidad;
                GameManager.Instance.NotificarMercadoActualizado(_flota.CiudadOrigenId, entrada.Bien);
            }

            bienesParaEliminar.Add(idBien);
        }

        foreach (int idBien in bienesParaEliminar)
        {
            _flota.Carga.Remove(idBien);
            _precioCompra.Remove(idBien);
        }

        CambiarEstado(EstadoFlotaPNJ.EnPuerto);
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
        _flota.CiudadDestinoId = ciudadDestinoId;
        _diasRestantesViaje    = diasViaje;

        _precioCompra.Clear();
        foreach (var kvp in preciosCompra)
            _precioCompra[kvp.Key] = kvp.Value;

        CambiarEstado(EstadoFlotaPNJ.Viajando);
    }

    // ─── Helpers privados ────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el snapshot global de precios de mercado almacenado con idFlota=0.
    /// Es la memoria compartida que FlotaManager actualiza cada 7 días.
    /// </summary>
    /// <returns>Lista de entradas de memoria con precios observados en todas las ciudades.</returns>
    private List<MemoriaComercialPNJDto> ObtenerSnapshotGlobal()
    {
        return _memoriaDAO.ObtenerMemoriaDeFlota(0);
    }

    /// <summary>
    /// Busca la <see cref="EntradaMercado"/> de un bien concreto en el mercado de una ciudad,
    /// resolviendo el <see cref="BienData"/> a partir del índice del catálogo.
    /// </summary>
    /// <param name="idCiudad">Identificador de la ciudad cuyo mercado se consulta.</param>
    /// <param name="idBien">Índice del bien en <see cref="GameManager.CatalogoBienes"/>.</param>
    /// <returns>La entrada del mercado correspondiente, o <c>null</c> si no existe.</returns>
    private EntradaMercado ObtenerEntrada(int idCiudad, int idBien)
    {
        var entradas = GameManager.Instance.GetEntradasMercado(idCiudad);
        if (entradas == null) return null;

        BienData bien = GameManager.Instance.CatalogoBienes[idBien];
        return entradas.FirstOrDefault(e => e.Bien == bien);
    }
}
