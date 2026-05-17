using UnityEngine;

/// <summary>
/// Decorador concreto para el casco tipo Galera (ComponentDecoratorImpl).
/// Navío ligero y muy maniobrable, ideal para escolta y combate rápido.
/// Carga reducida y pocos slots a cambio de la mayor velocidad y maniobrabilidad del juego.
/// Crear desde el menú Assets → Create → TFG → Astillero → Cascos → Galera.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Astillero/Cascos/Galera", fileName = "CascoGalera")]
public class CascoGalera : CascoDecorador
{
    /// <inheritdoc/>
    public override int    IdTipoCasco          => 4;
    /// <inheritdoc/>
    public override string NombreCasco          => "Galera";
    /// <inheritdoc/>
    public override int    VidaBase             => 80;
    /// <inheritdoc/>
    public override int    VelocidadBase        => 5;
    /// <inheritdoc/>
    public override int    ManiobrabilidadBase  => 4;
    /// <inheritdoc/>
    public override int    CapacidadCargaBase   => 150;
    /// <inheritdoc/>
    public override int    CapacidadModulos     => 3;
    /// <inheritdoc/>
    public override int    CapacidadTripulacion => 50;
    /// <inheritdoc/>
    public override int    CosteOro             => 1500;
}
