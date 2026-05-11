using System.Collections.Generic;

/// <summary>
/// Datos de runtime de una flota PNJ comerciante.
/// POCO puro (no MonoBehaviour): vive en <see cref="EstadoPartida.FlotasPorId"/>
/// y es gestionado por <see cref="FlotaManager"/>.
/// </summary>
public class FlotaRuntimeData
{
    /// <summary>Identificador único de la flota. Coincide con el <c>id_flota</c> de la base de datos.</summary>
    public int Id { get; }

    /// <summary>Nombre del comerciante propietario de la flota.</summary>
    public string NombrePropietario { get; }

    /// <summary>
    /// Nivel de inteligencia comercial del comerciante, entre 0 y 1.
    /// Determina la precisión con la que estima los precios de mercado en ciudades no visitadas.
    /// Un valor de 1 implica estimaciones casi exactas; 0 implica error máximo del 40%.
    /// Se asigna aleatoriamente al crear la flota y no cambia durante la partida.
    /// </summary>
    public float InteligenciaComercial { get; }

    /// <summary>Identificador de la ciudad donde la flota inició su ruta actual.</summary>
    public int CiudadOrigenId { get; set; }

    /// <summary>
    /// Identificador de la ciudad a la que se dirige la flota.
    /// Vale <c>-1</c> si la flota no tiene destino asignado todavía.
    /// </summary>
    public int CiudadDestinoId { get; set; }

    /// <summary>Estado actual dentro de la máquina de estados de comportamiento PNJ.</summary>
    public EstadoFlotaPNJ EstadoActual { get; set; }

    /// <summary>
    /// Secuencia de identificadores de ciudad que componen la ruta activa.
    /// El primer elemento es el siguiente waypoint; el último, el destino final.
    /// </summary>
    public List<int> RutaActual { get; set; }

    /// <summary>
    /// Inventario de la bodega de la flota.
    /// Clave: identificador del bien (coincide con <c>id_bien</c> del catálogo).
    /// Valor: unidades cargadas.
    /// </summary>
    public Dictionary<int, int> Carga { get; set; }

    /// <summary>
    /// Inicializa una flota PNJ con su identificador y propietario.
    /// El resto de campos quedan en sus valores por defecto:
    /// destino <c>-1</c>, estado <see cref="EstadoFlotaPNJ.EnPuerto"/> y colecciones vacías.
    /// </summary>
    /// <param name="id">Identificador único de la flota.</param>
    /// <param name="nombrePropietario">Nombre del comerciante propietario.</param>
    public FlotaRuntimeData(int id, string nombrePropietario)
    {
        Id                    = id;
        NombrePropietario     = nombrePropietario;
        InteligenciaComercial = UnityEngine.Random.Range(0.1f, 1.0f);
        CiudadOrigenId   = -1;
        CiudadDestinoId  = -1;
        EstadoActual     = EstadoFlotaPNJ.EnPuerto;
        RutaActual       = new List<int>();
        Carga            = new Dictionary<int, int>();
    }

    // Posición y ruta en el mapamundi

    /// <summary>Posición actual de la flota en coordenadas de mundo del mapamundi.</summary>
    public UnityEngine.Vector2 PosicionActual;

    /// <summary>Casilla de destino en coordenadas offset del Tilemap.</summary>
    public UnityEngine.Vector3Int CasillaDestino;

    /// <summary>Secuencia de casillas offset que componen la ruta en el mapamundi. Se recalcula al cargar.</summary>
    [System.NonSerialized] public List<UnityEngine.Vector3Int> RutaActualTilemap = new List<UnityEngine.Vector3Int>();

    /// <summary>Índice del siguiente waypoint dentro de <see cref="RutaActual"/>. Se recalcula al cargar.</summary>
    [System.NonSerialized] public int IndiceWaypointActual;

    /// <summary>
    /// Indica si la bodega de la flota contiene al menos un bien con cantidad mayor que cero.
    /// </summary>
    /// <returns><c>true</c> si hay mercancía cargada; <c>false</c> si la bodega está vacía.</returns>
    public bool TieneCarga()
    {
        foreach (int cantidad in Carga.Values)
        {
            if (cantidad > 0)
                return true;
        }
        return false;
    }
}
