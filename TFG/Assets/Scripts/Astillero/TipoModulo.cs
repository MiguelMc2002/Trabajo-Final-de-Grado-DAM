/// <summary>
/// Categoría funcional de un módulo instalable en un barco.
/// Determina qué estadística mejora principalmente el módulo.
/// </summary>
public enum TipoModulo
{
    /// <summary>Cañones, ballestas y otras armas. Aumenta la fuerza de combate.</summary>
    Armamento,

    /// <summary>Velas, mástiles y aparejos. Aumenta velocidad y maniobrabilidad.</summary>
    Velas,

    /// <summary>Bodegas y compartimentos de carga. Aumenta la capacidad de mercancías.</summary>
    Bodega,
}
