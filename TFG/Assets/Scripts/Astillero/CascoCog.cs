using UnityEngine;

/// <summary>
/// Decorador concreto para el casco tipo Cog (ComponentDecoratorImpl).
/// Navío de carga de tamaño mediano, equilibrado entre velocidad y capacidad.
/// Crear desde el menú Assets → Create → TFG → Astillero → Cascos → Cog.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Astillero/Cascos/Cog", fileName = "CascoCog")]
public class CascoCog : CascoDecorador
{
    /// <inheritdoc/>
    public override int    IdTipoCasco          => 1;
    /// <inheritdoc/>
    public override string NombreCasco          => "Cog";
    /// <inheritdoc/>
    public override int    VidaBase             => 100;
    /// <inheritdoc/>
    public override int    VelocidadBase        => 3;
    /// <inheritdoc/>
    public override int    ManiobrabilidadBase  => 2;
    /// <inheritdoc/>
    public override int    CapacidadCargaBase   => 200;
    /// <inheritdoc/>
    public override int    CapacidadModulos     => 4;
    /// <inheritdoc/>
    public override int    CapacidadTripulacion => 40;
    /// <inheritdoc/>
    public override int    CosteOro             => 800;
}
