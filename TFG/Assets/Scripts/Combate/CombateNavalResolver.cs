using System.Collections.Generic;
using UnityEngine;
using static ResultadoCombate;

/// <summary>
/// Clase estática pura que resuelve encuentros navales entre una flota pirata y su víctima
/// sin simular rondas. El cálculo es instantáneo y no modifica ningún objeto externo.
/// </summary>
public static class CombateNavalResolver
{
    /// <summary>
    /// Resuelve un encuentro naval entre un pirata y una víctima de forma automática.
    /// El método NO modifica ninguno de los dos objetos <see cref="FlotaRuntimeData"/>:
    /// el resultado debe ser aplicado por el llamador.
    /// </summary>
    /// <param name="pirata">Flota atacante. Debe tener <see cref="FlotaRuntimeData.IsPirata"/> == <c>true</c>.</param>
    /// <param name="victima">Flota defensora.</param>
    /// <param name="rng">Generador de aleatoriedad. Si es <c>null</c> se crea uno nuevo.</param>
    /// <returns>Resultado inmutable con el desenlace y todas las cifras del combate.</returns>
    public static ResultadoCombate Resolver(FlotaRuntimeData pirata, FlotaRuntimeData victima, System.Random rng = null)
    {
        rng ??= new System.Random();

        // ── PASO 1: ¿Intenta huir la víctima? ────────────────────────────────

        float ratioFuerza = pirata.FuerzaCanhones / Mathf.Max(0.01f, victima.FuerzaCanhones);
        bool intentaHuir  = ratioFuerza >= 1.2f;

        if (intentaHuir)
        {
            float factorHuida = (victima.VelocidadFlota + victima.ManiobrabilidadFlota + victima.HabilidadCapitan * 10f)
                                / Mathf.Max(0.01f, pirata.VelocidadFlota + pirata.ManiobrabilidadFlota + pirata.HabilidadCapitan * 10f);
            factorHuida *= (float)(rng.NextDouble() * 0.4 + 0.8); // ruido ±20%

            if (factorHuida > 1.0f)
            {
                return new ResultadoCombate(
                    DesenlaceCombate.ComercianteEscapa,
                    pirata.VidaActual,
                    victima.VidaActual,
                    0, 0, 0,
                    new Dictionary<int, int>(),
                    $"{victima.NombrePropietario} escapó de {pirata.NombrePropietario}.");
            }
        }

        // ── PASO 2: ¿Puede luchar la víctima? ────────────────────────────────

        bool puedeLuchar = victima.FuerzaCanhones > 0f || victima.NumBarcos > 1;

        if (!puedeLuchar)
        {
            var botonRendicion = new Dictionary<int, int>();
            foreach (var kvp in victima.Carga)
                if (kvp.Value > 0) botonRendicion[kvp.Key] = kvp.Value;

            return new ResultadoCombate(
                DesenlaceCombate.Rendicion,
                pirata.VidaActual,
                0f,
                0, 0, victima.NumBarcos,
                botonRendicion,
                $"{victima.NombrePropietario} se rindió ante {pirata.NombrePropietario}. Botín: {botonRendicion.Count} tipos.");
        }

        // ── PASO 3: Combate ───────────────────────────────────────────────────

        float fuerzaA = pirata.FuerzaCanhones + pirata.Tripulacion * 0.5f + pirata.HabilidadCapitan * 10f;
        float fuerzaD = victima.FuerzaCanhones + victima.Tripulacion * 0.5f + victima.HabilidadCapitan * 10f;

        float factorA     = (float)(rng.NextDouble() * 0.4 + 0.8);
        float factorD     = (float)(rng.NextDouble() * 0.4 + 0.8);
        float fuerzaFinalA = fuerzaA * factorA;
        float fuerzaFinalD = fuerzaD * factorD;

        float ratioA = fuerzaFinalA / Mathf.Max(0.01f, fuerzaFinalA + fuerzaFinalD);

        int barcosHundidosA = Mathf.Clamp(Mathf.RoundToInt(pirata.NumBarcos  * (1f - ratioA) * 0.8f), 0, pirata.NumBarcos);
        int barcosHundidosD = Mathf.Clamp(Mathf.RoundToInt(victima.NumBarcos * ratioA         * 0.8f), 0, victima.NumBarcos);

        // Captura de barcos del defensor (solo si el pirata gana)
        int barcosCap = 0;
        if (ratioA > 0.5f)
        {
            float probCaptura = Mathf.Clamp01((pirata.ManiobrabilidadFlota + pirata.Tripulacion * 0.01f) / 15f);
            barcosCap       = Mathf.RoundToInt(barcosHundidosD * probCaptura);
            barcosCap       = Mathf.Min(barcosCap, barcosHundidosD);
            barcosHundidosD -= barcosCap;
        }

        float vidaFinalA = pirata.VidaMax  * Mathf.Clamp01((pirata.NumBarcos  - barcosHundidosA)              / (float)Mathf.Max(1, pirata.NumBarcos));
        float vidaFinalD = victima.VidaMax * Mathf.Clamp01((victima.NumBarcos - barcosHundidosD - barcosCap)  / (float)Mathf.Max(1, victima.NumBarcos));

        DesenlaceCombate desenlace = ratioA >= 0.5f ? DesenlaceCombate.PirataGana : DesenlaceCombate.ComercianteGana;

        // Empate si ninguno destruye al otro significativamente
        if (Mathf.Abs(ratioA - 0.5f) < 0.05f)
            desenlace = DesenlaceCombate.Empate;

        // Botín (solo si pirata gana)
        var boton = new Dictionary<int, int>();
        if (desenlace == DesenlaceCombate.PirataGana)
        {
            float pct = barcosCap > 0 ? 1.0f : 0.5f;
            foreach (var kvp in victima.Carga)
            {
                if (kvp.Value <= 0) continue;
                int cant = Mathf.FloorToInt(kvp.Value * pct);
                if (cant > 0) boton[kvp.Key] = cant;
            }
        }

        string desc = $"Combate {pirata.NombrePropietario} vs {victima.NombrePropietario} | " +
                      $"Ratio:{ratioA:F2} | HundidosP:{barcosHundidosA} HundidosV:{barcosHundidosD} " +
                      $"CapturadosV:{barcosCap} | Botín:{boton.Count} tipos | Desenlace:{desenlace}";

        return new ResultadoCombate(desenlace, vidaFinalA, vidaFinalD,
            barcosHundidosA, barcosHundidosD, barcosCap, boton, desc);
    }
}
