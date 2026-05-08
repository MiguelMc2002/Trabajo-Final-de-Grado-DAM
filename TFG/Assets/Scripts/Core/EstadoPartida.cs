using System.Collections.Generic;

/// <summary>
/// Contenedor serializable con el estado completo de una partida en curso.
/// Es la fuente de verdad de todos los datos dinámicos del mundo mientras el jugador
/// está jugando. <see cref="GameManager"/> es su único propietario y la expone
/// a través de métodos controlados para evitar escrituras descontroladas.
/// </summary>
[System.Serializable]
public class EstadoPartida
{
    /// <summary>
    /// Estado vivo de los mercados de todas las ciudades de la Liga Hanseática.
    /// La clave es el <c>IdCiudad</c> del <see cref="CiudadData"/> correspondiente.
    /// Esta es la fuente de verdad del mercado: <see cref="MarketManager"/> lee y
    /// escribe aquí en lugar de mantener su propia lista interna.
    /// </summary>
    /// <summary>Día actual de la simulación de juego. Se incrementa cada tick diario.</summary>
    public int DiaJuego = 1;

    public Dictionary<int, List<EntradaMercado>> MercadosPorCiudad = new();

    // Pendiente: tipo concreto se define en Semana 3 - ver sesion_planificacion_release.md
    /// <summary>
    /// Estado de cada flota PNJ y del jugador indexado por su identificador único.
    /// La fuente de verdad de posición, tripulación y ruta de cada flota.
    /// </summary>
    public Dictionary<int, FlotaRuntimeData> FlotasPorId = new();

    // Pendiente: tipo concreto se define en Semana 4 - ver sesion_planificacion_release.md
    /// <summary>
    /// Estado de cada barco indexado por su identificador único.
    /// La fuente de verdad de vida, módulos y carga de cada barco.
    /// </summary>
    public Dictionary<int, object> BarcosPorId = new();

    // Pendiente: tipo concreto se define en Semana 5 - ver sesion_planificacion_release.md
    /// <summary>
    /// Edificios activos en cada ciudad indexados por el <c>IdCiudad</c> de <see cref="CiudadData"/>.
    /// La fuente de verdad de la capacidad productiva de cada ciudad.
    /// </summary>
    public Dictionary<int, List<object>> EdificiosPorCiudad = new();

    // Pendiente: tipo concreto se define en Semana 3 - ver sesion_planificacion_release.md
    /// <summary>
    /// Memoria comercial de cada flota PNJ indexada por el identificador de la flota.
    /// Permite que los comerciantes recuerden precios con 7 días de retraso.
    /// </summary>
    public Dictionary<int, object> MemoriaComercialPorFlota = new();
}
