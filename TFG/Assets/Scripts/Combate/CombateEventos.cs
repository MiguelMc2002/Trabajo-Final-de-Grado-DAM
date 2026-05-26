using System;
using UnityEngine;

/// <summary>
/// Canal de eventos estático para notificar el inicio y fin de combates navales.
/// Los sistemas interesados (UI, resolver, audio) se suscriben a los eventos
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
    /// Se dispara cuando el jugador cierra el panel de resultado de combate.
    /// Usado por <see cref="FlotaManager"/> para ejecutar respawns diferidos.
    /// </summary>
    public static event Action OnCombateTerminado;

    /// <summary>
    /// <c>true</c> mientras el jugador está en un combate naval sin resolver.
    /// Se activa en <see cref="DispararCombate"/> y se desactiva en <see cref="DispararFinCombate"/>.
    /// </summary>
    public static bool CombateJugadorEnCurso { get; private set; }

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

        CombateJugadorEnCurso = true;
        OnCombateIniciado?.Invoke(atacante, defensor);
    }

    /// <summary>
    /// Notifica que el combate del jugador ha concluido. Restablece <see cref="CombateJugadorEnCurso"/>
    /// y lanza <see cref="OnCombateTerminado"/> para que los sistemas pendientes (p. ej. respawn de PNJ)
    /// puedan ejecutarse con seguridad.
    /// Llamar desde <see cref="EncuentroNavalUI"/> al cerrar el panel de resultado.
    /// </summary>
    public static void DispararFinCombate()
    {
        CombateJugadorEnCurso = false;
        OnCombateTerminado?.Invoke();
    }
}
