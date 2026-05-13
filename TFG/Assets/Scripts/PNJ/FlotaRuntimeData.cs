using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Datos de runtime de una flota PNJ (comerciante o pirata).
/// POCO puro (no MonoBehaviour): vive en <see cref="EstadoPartida.FlotasPorId"/>
/// y es gestionado por <see cref="FlotaManager"/>.
/// </summary>
public class FlotaRuntimeData
{
    // ─── Backing fields de combate ────────────────────────────────────────────

    private bool  _isPirata;
    private float _vidaMax;
    private float _vidaActual;
    private float _fuerzaCanhones;
    private float _velocidadFlota;
    private float _maniobrabilidadFlota;
    private float _habilidadCapitan;
    private int   _numBarcos;
    private int   _tripulacion;

    // ─── Propiedades de combate ───────────────────────────────────────────────

    /// <summary>Indica si esta flota es pirata. Los piratas atacan a comerciantes al cruzarse.</summary>
    public bool IsPirata => _isPirata;

    /// <summary>Puntos de vida máximos de la flota, proporcionales al número inicial de barcos.</summary>
    public float VidaMax => _vidaMax;

    /// <summary>Puntos de vida actuales de la flota. Se reduce al recibir daño en combate.</summary>
    public float VidaActual
    {
        get => _vidaActual;
        set => _vidaActual = value;
    }

    /// <summary>Potencia de fuego combinada de todos los barcos de la flota.</summary>
    public float FuerzaCanhones => _fuerzaCanhones;

    /// <summary>Velocidad de navegación de la flota, usada para resolver intentos de huida.</summary>
    public float VelocidadFlota => _velocidadFlota;

    /// <summary>Maniobrabilidad de la flota, influye en capturas de barco y maniobras evasivas.</summary>
    public float ManiobrabilidadFlota => _maniobrabilidadFlota;

    /// <summary>Habilidad táctica del capitán. Coincide con <see cref="InteligenciaComercial"/> al crear la flota.</summary>
    public float HabilidadCapitan => _habilidadCapitan;

    /// <summary>Número de barcos operativos en la flota. Se reduce con bajas en combate.</summary>
    public int NumBarcos
    {
        get => _numBarcos;
        set => _numBarcos = value;
    }

    /// <summary>Tripulantes activos. Influyen en la fuerza de combate cuerpo a cuerpo.</summary>
    public int Tripulacion
    {
        get => _tripulacion;
        set => _tripulacion = value;
    }
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
        InteligenciaComercial = Random.Range(0.1f, 1.0f);
        CiudadOrigenId   = -1;
        CiudadDestinoId  = -1;
        EstadoActual     = EstadoFlotaPNJ.EnPuerto;
        RutaActual       = new List<int>();
        Carga            = new Dictionary<int, int>();

        _isPirata             = false;
        _vidaMax              = 100f;
        _vidaActual           = 100f;
        _fuerzaCanhones       = 8f;
        _velocidadFlota       = 5f;
        _maniobrabilidadFlota = 5f;
        _habilidadCapitan     = InteligenciaComercial;
        _numBarcos            = 5;
        _tripulacion          = 30;
    }

    /// <summary>
    /// Constructor para flotas pirata. Sobreescribe las estadísticas de combate con valores hostiles.
    /// </summary>
    /// <param name="id">Identificador único de la flota.</param>
    /// <param name="nombrePropietario">Nombre del capitán pirata.</param>
    /// <param name="esPirata">Debe ser <c>true</c> para activar los stats piratas.</param>
    public FlotaRuntimeData(int id, string nombrePropietario, bool esPirata) : this(id, nombrePropietario)
    {
        _isPirata = esPirata;
        if (esPirata)
        {
            _fuerzaCanhones       = 25f;
            _velocidadFlota       = 4f;
            _maniobrabilidadFlota = 4f;
            _tripulacion          = 40;
            // NumBarcos y VidaMax se mantienen en 5 y 100f
            // HabilidadCapitan ya asignada desde InteligenciaComercial en this()
        }
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
    /// Reduce la vida de la flota en la cantidad indicada, sin bajar de cero.
    /// </summary>
    /// <param name="cantidad">Puntos de daño a aplicar.</param>
    /// <returns><c>true</c> si la flota queda destruida (vida llega a 0).</returns>
    public bool AplicarDanio(float cantidad)
    {
        _vidaActual = Mathf.Max(0f, _vidaActual - cantidad);
        return _vidaActual <= 0f;
    }

    /// <summary>
    /// Indica si la flota ha sido destruida por completo (sin barcos operativos).
    /// </summary>
    /// <returns><c>true</c> si <see cref="NumBarcos"/> es cero o negativo.</returns>
    public bool EstaDestruida() => _numBarcos <= 0;

    /// <summary>
    /// Restaura la flota a su estado inicial de combate: vida máxima, tripulación y barcos completos,
    /// y bodega vaciada. Se llama semanalmente en piratas como ciclo de reabastecimiento.
    /// </summary>
    public void ResetearParaReabastecimiento()
    {
        _vidaActual  = _vidaMax;
        _tripulacion = _isPirata ? 40 : 30;
        _numBarcos   = 5;
        Carga.Clear();
        UnityEngine.Debug.Log($"[FlotaRuntimeData] {NombrePropietario} reabastecida: vida={_vidaActual} tripulación={_tripulacion} barcos={_numBarcos}");
    }

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
