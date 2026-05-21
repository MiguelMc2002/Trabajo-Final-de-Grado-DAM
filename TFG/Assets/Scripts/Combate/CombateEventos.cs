using System;
using UnityEngine;

/// <summary>
/// Canal de eventos estático para notificar el inicio de combates navales.
/// Los sistemas interesados (UI, resolver, audio) se suscriben a <see cref="OnCombateIniciado"/>
/// y reaccionan sin que los controladores PNJ conozcan sus detalles.
/// </summary>
public static class CombateEventos
{
    /// <summary>
    /// Se dispara cuando dos flotas entran en contacto y deben resolver un combate.
    /// El primer parámetro es el atacante; el segundo, el defensor.
    /// </summary>
    public static event Action<FlotaRuntimeData, FlotaRuntimeData> OnCombateIniciado;

    /// <summary>
    /// Lanza <see cref="OnCombateIniciado"/> si ambas flotas son válidas y no están destruidas.
    /// </summary>
    /// <param name="atacante">Flota que inicia el ataque (no puede ser <c>null</c> ni estar destruida).</param>
    /// <param name="defensor">Flota que recibe el ataque (no puede ser <c>null</c> ni estar destruida).</param>
    public static void DispararCombate(FlotaRuntimeData atacante, FlotaRuntimeData defensor)
    {
        if (atacante == null || atacante.EstaDestruida())
        {
            Debug.LogWarning("[CombateEventos] DispararCombate: atacante nulo o destruido, combate cancelado.");
            return;
        }

        if (defensor == null || defensor.EstaDestruida())
        {
            Debug.LogWarning("[CombateEventos] DispararCombate: defensor nulo o destruido, combate cancelado.");
            return;
        }

        OnCombateIniciado?.Invoke(atacante, defensor);
    }
}
