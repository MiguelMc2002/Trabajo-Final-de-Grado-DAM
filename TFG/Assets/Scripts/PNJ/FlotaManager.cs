using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor singleton de flotas PNJ activas en el mundo de juego.
/// Es la única puerta de entrada para registrar, consultar y cambiar
/// el estado de las flotas comerciantes durante la simulación.
/// </summary>
public class FlotaManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    /// <summary>Punto de acceso global al gestor de flotas PNJ.</summary>
    public static FlotaManager Instance { get; private set; }

    // ─── Referencia al estado de partida ─────────────────────────────────────

    private Dictionary<int, FlotaRuntimeData> FlotasPorId
        => GameManager.Instance.EstadoPartida.FlotasPorId;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[FlotaManager] Inicializado como singleton persistente.");
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Añade una flota PNJ al registro de flotas activas de la partida.
    /// Si ya existe una flota con el mismo <see cref="FlotaRuntimeData.Id"/>, la sobreescribe.
    /// </summary>
    /// <param name="flota">Datos de la flota a registrar. No puede ser <c>null</c>.</param>
    public void RegistrarFlota(FlotaRuntimeData flota)
    {
        if (flota == null)
        {
            Debug.LogError("[FlotaManager] RegistrarFlota: el parámetro flota es null.");
            return;
        }

        FlotasPorId[flota.Id] = flota;
        Debug.Log($"[FlotaManager] Flota registrada: id={flota.Id}, propietario={flota.NombrePropietario}");
    }

    /// <summary>
    /// Devuelve los datos de runtime de la flota con el identificador indicado.
    /// </summary>
    /// <param name="id">Identificador de la flota a buscar.</param>
    /// <returns>
    /// El <see cref="FlotaRuntimeData"/> correspondiente,
    /// o <c>null</c> si no hay ninguna flota con ese identificador.
    /// </returns>
    public FlotaRuntimeData ObtenerFlota(int id)
    {
        return FlotasPorId.TryGetValue(id, out FlotaRuntimeData flota) ? flota : null;
    }

    /// <summary>
    /// Devuelve todas las flotas PNJ actualmente activas en el mundo.
    /// </summary>
    /// <returns>Colección de solo lectura con todas las flotas registradas.</returns>
    public IReadOnlyCollection<FlotaRuntimeData> ObtenerTodasLasFlotas()
    {
        return FlotasPorId.Values;
    }

    /// <summary>
    /// Realiza una transición de estado en la flota indicada y registra el cambio en el log.
    /// No realiza ninguna acción si la flota no existe en el registro.
    /// </summary>
    /// <param name="flotaId">Identificador de la flota cuyo estado se cambia.</param>
    /// <param name="nuevoEstado">Nuevo estado de la máquina de estados PNJ.</param>
    public void CambiarEstado(int flotaId, EstadoFlotaPNJ nuevoEstado)
    {
        FlotaRuntimeData flota = ObtenerFlota(flotaId);
        if (flota == null)
        {
            Debug.LogWarning($"[FlotaManager] CambiarEstado: no existe flota con id={flotaId}.");
            return;
        }

        flota.EstadoActual = nuevoEstado;
        Debug.Log($"[FlotaManager] Flota {flotaId} → {nuevoEstado}");
    }
}
