using UnityEngine;

/// <summary>
/// Decorador concreto para el casco tipo Hulk (ComponentDecoratorImpl).
/// Navío de gran tonelaje, lento pero con la mayor capacidad de carga y el mayor número de slots.
/// Crear desde el menú Assets → Create → TFG → Astillero → Cascos → Hulk.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Astillero/Cascos/Hulk", fileName = "CascoHulk")]
public class CascoHulk : CascoDecorador
{
    /// <inheritdoc/>
    public override int    IdTipoCasco          => 2;
    /// <inheritdoc/>
    public override string NombreCasco          => "Hulk";
    /// <inheritdoc/>
    public override int    VidaBase             => 150;
    /// <inheritdoc/>
    public override int    VelocidadBase        => 2;
    /// <inheritdoc/>
    public override int    ManiobrabilidadBase  => 1;
    /// <inheritdoc/>
    public override int    CapacidadCargaBase   => 350;
    /// <inheritdoc/>
    public override int    CapacidadModulos     => 6;
    /// <inheritdoc/>
    public override int    CapacidadTripulacion => 60;
    /// <inheritdoc/>
    public override int    CosteOro             => 1200;
}
