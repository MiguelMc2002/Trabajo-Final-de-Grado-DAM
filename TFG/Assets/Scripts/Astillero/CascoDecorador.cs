using UnityEngine;

/// <summary>
/// Decorador abstracto del patrón Decorator para cascos de barco (ComponentDecorator).
/// Envuelve un <see cref="TipoCascoData"/> base y delega todas las propiedades de
/// <see cref="IBarco"/> en él por defecto. Las subclases concretas sobreescriben con
/// <c>override</c> las propiedades que necesiten modificar.
/// No tiene <see cref="CreateAssetMenuAttribute"/> porque es abstracto y no se puede
/// instanciar directamente desde el Editor.
/// </summary>
public abstract class CascoDecorador : ScriptableObject, IBarco
{
    /// <summary>
    /// Casco base que este decorador envuelve. Asignar desde el Inspector.
    /// Proporciona los valores de respaldo si la subclase no sobreescribe una propiedad.
    /// </summary>
    [SerializeField] protected TipoCascoData _cascoBase;

    // ─── IBarco — delegación al casco base ────────────────────────────────────

    /// <inheritdoc/>
    public virtual int    IdTipoCasco          => _cascoBase.IdTipoCasco;
    /// <inheritdoc/>
    public virtual string NombreCasco          => _cascoBase.NombreCasco;
    /// <inheritdoc/>
    public virtual int    VidaBase             => _cascoBase.VidaBase;
    /// <inheritdoc/>
    public virtual int    VelocidadBase        => _cascoBase.VelocidadBase;
    /// <inheritdoc/>
    public virtual int    ManiobrabilidadBase  => _cascoBase.ManiobrabilidadBase;
    /// <inheritdoc/>
    public virtual int    CapacidadCargaBase   => _cascoBase.CapacidadCargaBase;
    /// <inheritdoc/>
    public virtual int    CapacidadModulos     => _cascoBase.CapacidadModulos;
    /// <inheritdoc/>
    public virtual int    CapacidadTripulacion => _cascoBase.CapacidadTripulacion;
    /// <inheritdoc/>
    public virtual int    CosteOro             => _cascoBase.CosteOro;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avisa en consola si el casco base no está asignado, ya que las propiedades
    /// delegarían en una referencia nula y causarían una excepción en tiempo de ejecución.
    /// </summary>
    protected virtual void OnValidate()
    {
        if (_cascoBase == null)
            Debug.LogWarning($"[CascoDecorador] '{name}': el campo _cascoBase no está asignado en el Inspector.");
    }
}
