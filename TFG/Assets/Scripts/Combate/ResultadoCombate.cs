using System.Collections.Generic;

/// <summary>
/// Resultado inmutable de una resolución de combate naval.
/// Generado por <see cref="CombateNavalResolver.Resolver"/> y consumido por la UI de resultados.
/// No modifica ningún estado externo por sí mismo.
/// </summary>
public class ResultadoCombate
{
    /// <summary>Flota que inició el ataque. Se conserva para mostrar nombres en la UI.</summary>
    public FlotaRuntimeData Atacante { get; }

    /// <summary>Flota que recibió el ataque. Se conserva para mostrar nombres en la UI.</summary>
    public FlotaRuntimeData Defensor { get; }

    /// <summary><c>true</c> si el jugador salió victorioso del encuentro.</summary>
    public bool JugadorGana { get; }

    /// <summary>Barcos perdidos por el atacante durante el combate.</summary>
    public int BarcosPerdidosAtacante { get; }

    /// <summary>Barcos perdidos por el defensor durante el combate.</summary>
    public int BarcosPerdidosDefensor { get; }

    /// <summary>Daño total recibido por el atacante en puntos de vida.</summary>
    public float DanioRecibidoAtacante { get; }

    /// <summary>Daño total recibido por el defensor en puntos de vida.</summary>
    public float DanioRecibidoDefensor { get; }

    /// <summary>Oro obtenido como botín. Vale 0 si el jugador perdió.</summary>
    public long BotinOro { get; }

    /// <summary>
    /// Mercancías capturadas al vencido.
    /// Clave: <c>id_bien</c>. Valor: unidades capturadas. Vacío si el jugador perdió.
    /// </summary>
    public Dictionary<int, int> BotinMercancia { get; }

    /// <summary>Descripción narrativa del resultado del combate para mostrar en pantalla.</summary>
    public string TextoNarrativo { get; }

    /// <summary><c>true</c> si el jugador optó por huir y consiguió escapar sin combatir.</summary>
    public bool JugadorHuyo { get; }

    /// <summary>
    /// Inicializa un resultado de combate con todos sus campos.
    /// </summary>
    public ResultadoCombate(
        FlotaRuntimeData atacante,
        FlotaRuntimeData defensor,
        bool jugadorGana,
        int barcosPerdidosAtacante,
        int barcosPerdidosDefensor,
        float danioRecibidoAtacante,
        float danioRecibidoDefensor,
        long botinOro,
        Dictionary<int, int> botinMercancia,
        string textoNarrativo,
        bool jugadorHuyo = false)
    {
        Atacante                = atacante;
        Defensor                = defensor;
        JugadorGana             = jugadorGana;
        BarcosPerdidosAtacante  = barcosPerdidosAtacante;
        BarcosPerdidosDefensor  = barcosPerdidosDefensor;
        DanioRecibidoAtacante   = danioRecibidoAtacante;
        DanioRecibidoDefensor   = danioRecibidoDefensor;
        BotinOro                = botinOro;
        BotinMercancia          = botinMercancia ?? new Dictionary<int, int>();
        TextoNarrativo          = textoNarrativo ?? string.Empty;
        JugadorHuyo             = jugadorHuyo;
    }
}
