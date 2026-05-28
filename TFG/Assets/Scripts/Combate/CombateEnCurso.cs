using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un combate naval en curso resuelto de forma asíncrona, turno a turno.
/// Puro C# — no hereda de MonoBehaviour. Cada turno equivale a una hora de juego.
/// <see cref="GestorCombatesActivos"/> lo crea y lo avanza cada <see cref="SimulacionTiempo.OnNuevaHora"/>.
/// El daño se distribuye sobre los barcos individuales de <see cref="FlotaRuntimeData.BarcosFlota"/>;
/// un barco con <c>VidaActual == 0</c> queda inutilizado (potencialmente capturable al final).
/// </summary>
public class CombateEnCurso
{
    // ─── Propiedades públicas ─────────────────────────────────────────────────

    /// <summary>Flota que inició el encuentro.</summary>
    public FlotaRuntimeData Atacante { get; }

    /// <summary>Flota que recibió el ataque.</summary>
    public FlotaRuntimeData Defensor { get; }

    /// <summary>Número de turno completado (empieza en 0). Un turno = una hora de juego.</summary>
    public int TurnoActual { get; private set; }

    /// <summary>Si <c>true</c>, el jugador (Id == -1) es una de las dos flotas.</summary>
    public bool EsDelJugador { get; }

    /// <summary>
    /// Verdadero si el combate terminó por timeout (5 días sin resolución),
    /// interpretado como evasión del defensor con los daños acumulados.
    /// </summary>
    public bool TerminoPorTimeout { get; private set; }

    /// <summary>Barcos activos al inicio del combate en la flota atacante.</summary>
    public int NumBarcosInicialAtacante { get; }

    /// <summary>Barcos activos al inicio del combate en la flota defensora.</summary>
    public int NumBarcosInicialDefensor { get; }

    /// <summary>Flota enemiga del jugador (la que no tiene Id == -1).</summary>
    public FlotaRuntimeData FlotaEnemiga => Atacante.Id != -1 ? Atacante : Defensor;

    /// <summary>El jugador ganó: su flota sobrevive y la enemiga fue destruida.</summary>
    public bool JugadorGano
    {
        get
        {
            FlotaRuntimeData jugador = Atacante.Id == -1 ? Atacante : Defensor;
            return !jugador.EstaDestruida() && FlotaEnemiga.EstaDestruida();
        }
    }

    /// <summary>Flota superviviente al terminar el combate; <c>null</c> si ambas destruidas.</summary>
    public FlotaRuntimeData Ganador
    {
        get
        {
            bool atVive = !Atacante.EstaDestruida();
            bool dfVive = !Defensor.EstaDestruida();
            if (atVive && !dfVive) return Atacante;
            if (dfVive && !atVive) return Defensor;
            return null;
        }
    }

    // ─── Constantes ───────────────────────────────────────────────────────────

    /// <summary>Turnos máximos (5 días × 24 h) antes de forzar la evasión del defensor.</summary>
    private const int MaxTurnos = 120;

    /// <summary>
    /// Fuerza base de combate que aporta cada barco activo incluso sin módulos de armamento.
    /// Representa armas ligeras, mosquetes y abordaje.
    /// </summary>
    private const float FuerzaBaseXBarco = 5f;

    /// <summary>
    /// Fracción de la fuerza de combate que se convierte en daño cada turno (hora de juego).
    /// </summary>
    private const float MultiplicadorDanio = 0.15f;

    // ─────────────────────────────────────────────────────────────────────────

    /// <param name="atacante">Flota que inicia el encuentro.</param>
    /// <param name="defensor">Flota que recibe el ataque.</param>
    /// <param name="esDelJugador">Verdadero si alguna flota tiene Id == -1.</param>
    public CombateEnCurso(FlotaRuntimeData atacante, FlotaRuntimeData defensor, bool esDelJugador)
    {
        Atacante    = atacante;
        Defensor    = defensor;
        EsDelJugador = esDelJugador;
        TurnoActual = 0;
        NumBarcosInicialAtacante = atacante.NumBarcos;
        NumBarcosInicialDefensor = defensor.NumBarcos;
    }

    // ─── Resolución de turno ──────────────────────────────────────────────────

    /// <summary>
    /// Avanza el combate un turno: calcula la fuerza actual de cada flota (incluyendo
    /// barcos inutilizados que ya no contribuyen), distribuye el daño entre los barcos
    /// activos de cada bando y recalcula los stats agregados.
    /// </summary>
    /// <returns><c>true</c> si el combate ha terminado.</returns>
    public bool ResolverTurno()
    {
        float fuerzaAtacante = CalcularFuerza(Atacante) * Random.Range(0.7f, 1.3f);
        float fuerzaDefensor = CalcularFuerza(Defensor) * Random.Range(0.7f, 1.3f);

        AplicarDanioConBarcos(Atacante, fuerzaDefensor * MultiplicadorDanio);
        AplicarDanioConBarcos(Defensor, fuerzaAtacante * MultiplicadorDanio);

        TurnoActual++;

        if (Atacante.EstaDestruida() || Defensor.EstaDestruida())
            return true;

        if (TurnoActual >= MaxTurnos)
        {
            TerminoPorTimeout = true;
            return true;
        }

        return false;
    }

    // ─── Helpers de combate ───────────────────────────────────────────────────

    /// <summary>
    /// Calcula la fuerza de combate actual de una flota.
    /// Usa los barcos activos (<c>NumBarcos</c>) y sus cañones instalados (<c>FuerzaCanhones</c>).
    /// Escalada por la ratio de tripulación restante (referencia: 40 marineros).
    /// </summary>
    private static float CalcularFuerza(FlotaRuntimeData f)
    {
        // Fuerza base por barco (armas ligeras) + potencia de cañones instalados
        float fuerza = f.NumBarcos * FuerzaBaseXBarco + f.FuerzaCanhones;
        // La tripulación superviviente escala la efectividad
        return fuerza * (f.Tripulacion / (float)Mathf.Max(1, 40));
    }

    /// <summary>
    /// Distribuye el daño entre los barcos activos de la flota.
    /// Si tiene datos individuales (<see cref="FlotaRuntimeData.BarcosFlota"/>), aplica
    /// el daño barco a barco y recalcula stats; de lo contrario usa el fallback agregado.
    /// </summary>
    private static void AplicarDanioConBarcos(FlotaRuntimeData flota, float danio)
    {
        if (flota.BarcosFlota != null && flota.BarcosFlota.Count > 0)
        {
            var activos = new List<BarcoJugador>();
            foreach (BarcoJugador b in flota.BarcosFlota)
                if (b.VidaActual > 0) activos.Add(b);

            if (activos.Count > 0)
            {
                float danioXBarco = danio / activos.Count;
                foreach (BarcoJugador barco in activos)
                    barco.VidaActual = Mathf.Max(0, Mathf.RoundToInt(barco.VidaActual - danioXBarco));
            }

            RecalcularStatsDesdeBarcos(flota);
        }
        else
        {
            // Fallback para flotas sin datos de barcos individuales
            int barcosPrevios = flota.NumBarcos;
            flota.AplicarDanio(danio);

            // Escalar NumBarcos y FuerzaCanhones proporcionalmente a la vida perdida
            float ratioVida = flota.VidaMax > 0 ? flota.VidaActual / flota.VidaMax : 0f;
            int nuevosBarcos = Mathf.RoundToInt(barcosPrevios * ratioVida);
            int perdidos = barcosPrevios - nuevosBarcos;
            if (perdidos > 0)
            {
                flota.NumBarcos      = Mathf.Max(0, nuevosBarcos);
                flota.Tripulacion    = Mathf.Max(0, flota.Tripulacion - perdidos * 8);
                float ratioBarcos    = barcosPrevios > 0 ? (float)flota.NumBarcos / barcosPrevios : 0f;
                flota.FuerzaCanhones = flota.FuerzaCanhones * ratioBarcos;
            }
        }
    }

    /// <summary>
    /// Recalcula <see cref="FlotaRuntimeData.VidaActual"/>, <see cref="FlotaRuntimeData.NumBarcos"/>,
    /// <see cref="FlotaRuntimeData.FuerzaCanhones"/> y <see cref="FlotaRuntimeData.Tripulacion"/>
    /// a partir del estado actual de los barcos individuales.
    /// </summary>
    private static void RecalcularStatsDesdeBarcos(FlotaRuntimeData flota)
    {
        int   activoCount = 0;
        float vidaActual  = 0f;
        float fuerza      = 0f;
        int   barcosPrev  = flota.NumBarcos;

        foreach (BarcoJugador barco in flota.BarcosFlota)
        {
            if (barco.VidaActual > 0)
            {
                activoCount++;
                vidaActual += barco.VidaActual;
                fuerza     += barco.FuerzaCombateTotal;
            }
        }

        int perdidos = barcosPrev - activoCount;

        flota.VidaActual     = vidaActual;
        flota.NumBarcos      = activoCount;
        flota.FuerzaCanhones = fuerza;
        flota.Tripulacion    = Mathf.Max(0, flota.Tripulacion - perdidos * 8);
    }
}
