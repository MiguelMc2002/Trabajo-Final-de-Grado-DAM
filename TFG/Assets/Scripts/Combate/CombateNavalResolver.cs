using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clase estática pura que resuelve encuentros navales de forma instantánea.
/// No modifica ningún objeto externo salvo aplicar daño mediante <see cref="FlotaRuntimeData.AplicarDanio"/>
/// y transferir el botín de oro a <see cref="GameManager"/> cuando el jugador gana.
/// </summary>
public static class CombateNavalResolver
{
    /// <summary>
    /// Resuelve un encuentro naval entre dos flotas y devuelve el resultado completo.
    /// Aplica el daño directamente sobre las flotas y, si el jugador gana, llama a
    /// <see cref="GameManager.ModificarDinero"/> con el botín de oro.
    /// </summary>
    /// <param name="atacante">Flota que inicia el ataque.</param>
    /// <param name="defensor">Flota que recibe el ataque.</param>
    /// <param name="jugadorEsAtacante">
    /// <c>true</c> si la flota del jugador es la atacante; <c>false</c> si es la defensora.
    /// </param>
    /// <param name="jugadorIntentaHuir">
    /// <c>true</c> si el jugador intenta escapar antes del combate.
    /// Hay un 30 % de probabilidad de huida limpia; si falla, el jugador combate
    /// con un 20 % de penalización en su FuerzaTotal.
    /// </param>
    /// <returns>Resultado inmutable con todas las cifras del combate.</returns>
    public static ResultadoCombate Resolver(
        FlotaRuntimeData atacante,
        FlotaRuntimeData defensor,
        bool jugadorEsAtacante,
        bool jugadorIntentaHuir = false)
    {
        // ── Huida ────────────────────────────────────────────────────────────────
        if (jugadorIntentaHuir && Random.value < 0.30f)
        {
            return new ResultadoCombate(
                atacante, defensor,
                jugadorGana: true,
                barcosPerdidosAtacante: 0,
                barcosPerdidosDefensor: 0,
                danioRecibidoAtacante: 0f,
                danioRecibidoDefensor: 0f,
                botinOro: 0L,
                botinMercancia: new Dictionary<int, int>(),
                textoNarrativo: "Lograsteis escapar entre la niebla. La flota enemiga os perdió de vista.",
                jugadorHuyo: true);
        }

        // ── Cálculo de fuerzas ────────────────────────────────────────────────────
        float fuerzaAtacante = CalcularFuerza(atacante);
        float fuerzaDefensor = CalcularFuerza(defensor);

        // Penalización al jugador si intentó huir y falló
        if (jugadorIntentaHuir)
        {
            if (jugadorEsAtacante) fuerzaAtacante *= 0.80f;
            else                   fuerzaDefensor  *= 0.80f;
        }

        fuerzaAtacante *= Random.Range(0.85f, 1.15f);
        fuerzaDefensor *= Random.Range(0.85f, 1.15f);

        bool atacanteGana = fuerzaAtacante >= fuerzaDefensor;

        // ── Daño ─────────────────────────────────────────────────────────────────
        float danioGanador  = (atacanteGana ? atacante : defensor).VidaActual * 0.20f;
        float danioPerdedor = (atacanteGana ? defensor : atacante).VidaActual * 0.60f;

        float danioAtacante = atacanteGana ? danioGanador  : danioPerdedor;
        float danioDefensor = atacanteGana ? danioPerdedor : danioGanador;

        atacante.AplicarDanio(danioAtacante);
        defensor.AplicarDanio(danioDefensor);

        // ── Barcos perdidos (1 barco por cada 20 puntos de daño, mínimo 0) ────────
        int barcosPerdidosAtacante = Mathf.Max(0, Mathf.FloorToInt(danioAtacante / 20f));
        int barcosPerdidosDefensor = Mathf.Max(0, Mathf.FloorToInt(danioDefensor / 20f));

        // Clampear para no exceder los barcos disponibles
        barcosPerdidosAtacante = Mathf.Min(barcosPerdidosAtacante, atacante.NumBarcos);
        barcosPerdidosDefensor = Mathf.Min(barcosPerdidosDefensor, defensor.NumBarcos);

        atacante.NumBarcos -= barcosPerdidosAtacante;
        defensor.NumBarcos -= barcosPerdidosDefensor;

        // ── Determinar si el jugador ganó ─────────────────────────────────────────
        bool jugadorGana = jugadorEsAtacante ? atacanteGana : !atacanteGana;

        // ── Botín (solo si el jugador gana) ───────────────────────────────────────
        long botinOro = 0L;
        var  botinMercancia = new Dictionary<int, int>();

        if (jugadorGana)
        {
            FlotaRuntimeData vencido = atacanteGana ? defensor : atacante;
            botinOro = (long)(vencido.VidaMax * 10f);

            foreach (var kvp in vencido.Carga)
            {
                int cantidad = Mathf.RoundToInt(kvp.Value * 0.40f);
                if (cantidad > 0) botinMercancia[kvp.Key] = cantidad;
            }

            GameManager.Instance.ModificarDinero(botinOro);
        }

        // ── Texto narrativo ───────────────────────────────────────────────────────
        FlotaRuntimeData perdedor = atacanteGana ? defensor : atacante;
        string texto = jugadorGana
            ? $"¡Victoria! La flota de {perdedor.NombrePropietario} ha sido derrotada."
            : $"La flota de {atacante.NombrePropietario} ha sido destruida. Regresad al puerto más cercano.";

        return new ResultadoCombate(
            atacante, defensor,
            jugadorGana,
            barcosPerdidosAtacante,
            barcosPerdidosDefensor,
            danioAtacante,
            danioDefensor,
            botinOro,
            botinMercancia,
            texto);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula la fuerza total de combate de una flota sin ruido aleatorio.
    /// Fórmula: <c>(VidaActual × FuerzaCañones × (NumBarcos × 0.3 + 0.7)) × HabilidadCapitan</c>.
    /// </summary>
    /// <param name="flota">Flota a evaluar.</param>
    /// <returns>Valor de fuerza base antes del factor aleatorio.</returns>
    private static float CalcularFuerza(FlotaRuntimeData flota)
        => flota.VidaActual * flota.FuerzaCanhones
           * (flota.NumBarcos * 0.3f + 0.7f)
           * flota.HabilidadCapitan;
}
