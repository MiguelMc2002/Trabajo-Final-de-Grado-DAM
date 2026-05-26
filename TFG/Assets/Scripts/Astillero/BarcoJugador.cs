using System.Collections.Generic;

/// <summary>
/// Representa un barco propiedad del jugador con su casco base y los módulos instalados.
/// Aplica el patrón Decorator: las propiedades calculadas suman el valor base del casco
/// con los deltas de todos los módulos instalados.
/// Clase pura C# — no hereda de MonoBehaviour.
/// </summary>
public class BarcoJugador
{
    private readonly List<ModuloBarcoData> _modulos = new List<ModuloBarcoData>();

    // ─── Identidad ────────────────────────────────────────────────────────────

    /// <summary>Identificador único del barco. Usado para buscarlo en <see cref="FlotaJugador"/>.</summary>
    public int IdBarco { get; }

    /// <summary>Nombre propio del barco asignado por el jugador.</summary>
    public string Nombre { get; }

    /// <summary>Casco base del barco. Puede ser un <see cref="TipoCascoData"/> o cualquier <see cref="CascoDecorador"/>.</summary>
    public IBarco CascoBase { get; }

    /// <summary>Lista de módulos actualmente instalados. Solo lectura desde fuera.</summary>
    public IReadOnlyList<ModuloBarcoData> ModulosInstalados => _modulos;

    /// <summary>
    /// Indica si este barco es una unidad de combate activa.
    /// Los barcos de carga no participan directamente en el combate naval.
    /// </summary>
    public bool EsBarcosCombate { get; set; }

    /// <summary>
    /// Puntos de vida actuales del barco. Se inicializa a <see cref="VidaTotal"/> al construir.
    /// Se reduce al recibir daño y se restaura al reparar en el astillero.
    /// </summary>
    public int VidaActual { get; set; }

    /// <summary>
    /// Número de tripulantes embarcados en este barco.
    /// Empieza en 0; se asigna al contratar marineros en la taberna o al generar flotas PNJ.
    /// </summary>
    public int Tripulacion { get; set; } = 0;

    /// <summary>
    /// Capacidad máxima de tripulación determinada por el casco base.
    /// </summary>
    public int TripulacionMaxima => CascoBase?.CapacidadTripulacion ?? 0;

    // ─── Propiedades calculadas ───────────────────────────────────────────────

    /// <summary>Puntos de vida máximos: base del casco más suma de deltaVida de los módulos.</summary>
    public int VidaTotal
    {
        get
        {
            int total = CascoBase.VidaBase;
            foreach (ModuloBarcoData m in _modulos) total += m.deltaVida;
            return total;
        }
    }

    /// <summary>Velocidad total: base del casco más suma de deltaVelocidad de los módulos.</summary>
    public int VelocidadTotal
    {
        get
        {
            int total = CascoBase.VelocidadBase;
            foreach (ModuloBarcoData m in _modulos) total += m.deltaVelocidad;
            return total;
        }
    }

    /// <summary>Maniobrabilidad total: base del casco más suma de deltaManiobrabilidad de los módulos.</summary>
    public int ManiobrabilidadTotal
    {
        get
        {
            int total = CascoBase.ManiobrabilidadBase;
            foreach (ModuloBarcoData m in _modulos) total += m.deltaManiobrabilidad;
            return total;
        }
    }

    /// <summary>Capacidad de carga total: base del casco más suma de deltaCargaMaxima de los módulos.</summary>
    public int CargaMaximaTotal
    {
        get
        {
            int total = CascoBase.CapacidadCargaBase;
            foreach (ModuloBarcoData m in _modulos) total += m.deltaCargaMaxima;
            return total;
        }
    }

    /// <summary>
    /// Fuerza de combate total: suma de deltaFuerzaCombate de todos los módulos instalados.
    /// Vale 0 si no hay ningún módulo de armamento.
    /// </summary>
    public int FuerzaCombateTotal
    {
        get
        {
            int total = 0;
            foreach (ModuloBarcoData m in _modulos) total += m.deltaFuerzaCombate;
            return total;
        }
    }

    /// <summary>Número de slots ocupados por los módulos instalados.</summary>
    public int SlotsUsados
    {
        get
        {
            int total = 0;
            foreach (ModuloBarcoData m in _modulos) total += m.slotsCosto;
            return total;
        }
    }

    /// <summary>Slots libres restantes en este casco.</summary>
    public int SlotsDisponibles => CascoBase.CapacidadModulos - SlotsUsados;

    // ─── Constructor ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa un barco nuevo con su identificador, nombre y casco base.
    /// <see cref="VidaActual"/> se fija al valor de <see cref="VidaTotal"/> en el momento de la construcción.
    /// </summary>
    /// <param name="idBarco">Identificador único del barco.</param>
    /// <param name="nombre">Nombre propio asignado por el jugador.</param>
    /// <param name="casco">ScriptableObject con las estadísticas base del casco.</param>
    public BarcoJugador(int idBarco, string nombre, IBarco casco)
    {
        IdBarco   = idBarco;
        Nombre    = nombre;
        CascoBase = casco;
        VidaActual = VidaTotal;
    }

    // ─── Métodos públicos ─────────────────────────────────────────────────────

    /// <summary>
    /// Intenta instalar un módulo en el barco.
    /// Falla si no hay slots suficientes, si hay un módulo con el mismo nombre
    /// o si el módulo requiere pólvora y el año de juego aún no la ha desbloqueado.
    /// </summary>
    /// <param name="modulo">Módulo a instalar.</param>
    /// <returns><c>true</c> si la instalación tuvo éxito; <c>false</c> en caso contrario.</returns>
    public bool InstalarModulo(ModuloBarcoData modulo)
    {
        if (!PuedeInstalar(modulo)) return false;
        _modulos.Add(modulo);
        return true;
    }

    /// <summary>
    /// Desinstala un módulo previamente instalado en el barco.
    /// </summary>
    /// <param name="modulo">Módulo a desinstalar.</param>
    /// <returns><c>true</c> si el módulo estaba instalado y se eliminó; <c>false</c> si no estaba.</returns>
    public bool DesinstalarModulo(ModuloBarcoData modulo)
    {
        return _modulos.Remove(modulo);
    }

    /// <summary>
    /// Comprueba si un módulo puede instalarse en el barco según slots disponibles,
    /// ausencia de duplicado por nombre y requisito de pólvora.
    /// </summary>
    /// <param name="modulo">Módulo candidato.</param>
    /// <returns><c>true</c> si el módulo se puede instalar.</returns>
    public bool PuedeInstalar(ModuloBarcoData modulo)
    {
        if (SlotsDisponibles < modulo.slotsCosto) return false;

        foreach (ModuloBarcoData m in _modulos)
            if (m.nombreModulo == modulo.nombreModulo) return false;

        if (modulo.requierePolvora &&
            (SimulacionTiempo.Instance == null ||
             SimulacionTiempo.Instance.AñoActual < ModuloBarcoData.AnioDesbloqueoPolvoraJuego))
            return false;

        return true;
    }

    /// <summary>
    /// Embarca la cantidad indicada de marineros sin superar <see cref="TripulacionMaxima"/>.
    /// </summary>
    /// <param name="cantidad">Marineros a contratar.</param>
    /// <returns>Marineros realmente embarcados (puede ser menor si el barco está casi lleno).</returns>
    public int ContratarMarineros(int cantidad)
    {
        int hueco  = TripulacionMaxima - Tripulacion;
        int reales = cantidad > hueco ? hueco : (cantidad < 0 ? 0 : cantidad);
        Tripulacion += reales;
        return reales;
    }

    /// <summary>
    /// Licencia la cantidad indicada de marineros sin bajar de cero tripulantes.
    /// </summary>
    /// <param name="cantidad">Marineros a licenciar.</param>
    /// <returns>Marineros realmente licenciados.</returns>
    public int LicenciarMarineros(int cantidad)
    {
        int reales = cantidad > Tripulacion ? Tripulacion : (cantidad < 0 ? 0 : cantidad);
        Tripulacion -= reales;
        return reales;
    }

    /// <summary>
    /// Devuelve el primer módulo instalado cuyo <see cref="ModuloBarcoData.tipoModulo"/>
    /// coincida con el tipo indicado. Solo puede haber un módulo por tipo.
    /// </summary>
    /// <param name="tipo">Categoría funcional buscada (Armamento, Velas o Bodega).</param>
    /// <returns>El <see cref="ModuloBarcoData"/> encontrado, o <c>null</c> si no hay ninguno de ese tipo.</returns>
    public ModuloBarcoData ObtenerModuloPorTipo(TipoModulo tipo)
    {
        foreach (ModuloBarcoData m in _modulos)
            if (m.tipoModulo == tipo) return m;
        return null;
    }
}
