using System.Collections.Generic;

/// <summary>
/// Resultado inmutable de una resolución de combate naval automática.
/// Se genera por <see cref="CombateNavalResolver.Resolver"/> y no modifica
/// ningún estado externo: es el llamador quien aplica los cambios.
/// </summary>
public class ResultadoCombate
{
    /// <summary>
    /// Posibles desenlaces de un encuentro naval entre un pirata y su víctima.
    /// </summary>
    public enum DesenlaceCombate
    {
        /// <summary>El pirata vence: la víctima pierde barcos y carga.</summary>
        PirataGana,
        /// <summary>La víctima consigue escapar antes del primer disparo.</summary>
        ComercianteEscapa,
        /// <summary>El comerciante rechaza el ataque y el pirata sale dañado.</summary>
        ComercianteGana,
        /// <summary>La víctima se rinde sin combatir, cediendo toda la carga.</summary>
        Rendicion,
        /// <summary>Ambos bandos sufren bajas pero ninguno gana de forma clara.</summary>
        Empate,
    }

    /// <summary>Desenlace final del encuentro naval.</summary>
    public DesenlaceCombate Desenlace { get; }

    /// <summary>Puntos de vida del atacante (pirata) tras el combate.</summary>
    public float VidaFinalAtacante { get; }

    /// <summary>Puntos de vida del defensor (víctima) tras el combate.</summary>
    public float VidaFinalDefensor { get; }

    /// <summary>Número de barcos del atacante hundidos durante el combate.</summary>
    public int BarcosHundidosAtacante { get; }

    /// <summary>Número de barcos del defensor hundidos durante el combate.</summary>
    public int BarcosHundidosDefensor { get; }

    /// <summary>Número de barcos del defensor capturados por el atacante.</summary>
    public int BarcosCapturedDefensor { get; }

    /// <summary>
    /// Mercancías capturadas al defensor.
    /// Clave: <c>id_bien</c>. Valor: unidades capturadas.
    /// Vacío si el pirata no ganó.
    /// </summary>
    public Dictionary<int, int> BotonCapturado { get; }

    /// <summary>Texto descriptivo del combate para el log de juego.</summary>
    public string Descripcion { get; }

    /// <summary>
    /// Inicializa un resultado de combate con todos sus campos. Todos los valores son inmutables.
    /// </summary>
    /// <param name="desenlace">Desenlace del encuentro.</param>
    /// <param name="vidaAtacante">Vida final del pirata.</param>
    /// <param name="vidaDefensor">Vida final de la víctima.</param>
    /// <param name="barcosHundidosAtacante">Bajas del pirata.</param>
    /// <param name="barcosHundidosDefensor">Bajas de la víctima.</param>
    /// <param name="barcosCapturedDefensor">Barcos capturados al defensor.</param>
    /// <param name="boton">Carga capturada (puede ser null).</param>
    /// <param name="descripcion">Texto del log (puede ser null).</param>
    public ResultadoCombate(
        DesenlaceCombate desenlace,
        float vidaAtacante,
        float vidaDefensor,
        int barcosHundidosAtacante,
        int barcosHundidosDefensor,
        int barcosCapturedDefensor,
        Dictionary<int, int> boton,
        string descripcion)
    {
        Desenlace              = desenlace;
        VidaFinalAtacante      = vidaAtacante;
        VidaFinalDefensor      = vidaDefensor;
        BarcosHundidosAtacante = barcosHundidosAtacante;
        BarcosHundidosDefensor = barcosHundidosDefensor;
        BarcosCapturedDefensor = barcosCapturedDefensor;
        BotonCapturado         = boton ?? new Dictionary<int, int>();
        Descripcion            = descripcion ?? string.Empty;
    }
}
