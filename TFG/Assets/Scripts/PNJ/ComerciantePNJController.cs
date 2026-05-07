using UnityEngine;

/// <summary>
/// Controlador puro (no MonoBehaviour) que gobierna el ciclo de vida
/// de una flota PNJ comerciante mediante una máquina de estados.
/// Se instancia por cada flota registrada en <see cref="FlotaManager"/>
/// y avanza un paso por día de juego al invocar <see cref="Tick"/>.
/// </summary>
public class ComerciantePNJController
{
    private readonly FlotaRuntimeData _flota;
    private readonly FlotaManager _manager;

    /// <summary>
    /// Inicializa el controlador vinculándolo a una flota y a su gestor.
    /// </summary>
    /// <param name="flota">Datos de runtime de la flota que este controlador gobierna.</param>
    /// <param name="manager">Gestor central de flotas PNJ, usado para aplicar transiciones de estado.</param>
    public ComerciantePNJController(FlotaRuntimeData flota, FlotaManager manager)
    {
        _flota   = flota;
        _manager = manager;
    }

    /// <summary>
    /// Avanza la lógica de comportamiento de la flota un día de juego.
    /// Delega en el método privado correspondiente al estado actual de la flota.
    /// Debe llamarse desde <see cref="FlotaManager.TickTodosLosControladores"/> cada vez que <c>OnNuevoDia</c> se dispare.
    /// </summary>
    public void Tick()
    {
        switch (_flota.EstadoActual)
        {
            case EstadoFlotaPNJ.EnPuerto:
                TickEnPuerto();
                break;
            case EstadoFlotaPNJ.Viajando:
                TickViajando();
                break;
            case EstadoFlotaPNJ.Comerciando:
                TickComerciando();
                break;
        }
    }

    // ─── Estados ─────────────────────────────────────────────────────────────

    private void TickEnPuerto()
    {
        Debug.Log($"[ComerciantePNJController] Flota {_flota.Id} procesando estado EnPuerto.");
    }

    private void TickViajando()
    {
        Debug.Log($"[ComerciantePNJController] Flota {_flota.Id} procesando estado Viajando.");
    }

    private void TickComerciando()
    {
        Debug.Log($"[ComerciantePNJController] Flota {_flota.Id} procesando estado Comerciando.");
    }

    // ─── Transiciones ────────────────────────────────────────────────────────

    private void CambiarEstado(EstadoFlotaPNJ nuevoEstado)
    {
        _manager.CambiarEstado(_flota.Id, nuevoEstado);
    }
}
