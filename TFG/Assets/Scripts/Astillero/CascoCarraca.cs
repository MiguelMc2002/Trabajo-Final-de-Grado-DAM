using UnityEngine;

/// <summary>
/// Decorador concreto para el casco tipo Carraca (ComponentDecoratorImpl).
/// Navío robusto de combate y carga, con gran resistencia y tripulación amplia.
/// Crear desde el menú Assets → Create → TFG → Astillero → Cascos → Carraca.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Astillero/Cascos/Carraca", fileName = "CascoCarraca")]
public class CascoCarraca : CascoDecorador
{
    /// <inheritdoc/>
    public override int    IdTipoCasco          => 3;
    /// <inheritdoc/>
    public override string NombreCasco          => "Carraca";
    /// <inheritdoc/>
    public override int    VidaBase             => 200;
    /// <inheritdoc/>
    public override int    VelocidadBase        => 2;
    /// <inheritdoc/>
    public override int    ManiobrabilidadBase  => 2;
    /// <inheritdoc/>
    public override int    CapacidadCargaBase   => 300;
    /// <inheritdoc/>
    public override int    CapacidadModulos     => 5;
    /// <inheritdoc/>
    public override int    CapacidadTripulacion => 80;
    /// <inheritdoc/>
    public override int    CosteOro             => 1800;
}
