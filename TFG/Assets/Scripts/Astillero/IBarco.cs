/// <summary>
/// Interfaz común del patrón Decorator para cascos de barco (IComponent).
/// Tanto <see cref="TipoCascoData"/> (componente concreto) como <see cref="CascoDecorador"/>
/// (decorador abstracto) la implementan, lo que permite tratarlos de forma uniforme.
/// </summary>
public interface IBarco
{
    /// <summary>Identificador único del tipo de casco, coincide con <c>id_tipo_casco</c> en la BD.</summary>
    int IdTipoCasco { get; }

    /// <summary>Nombre del tipo de casco mostrado en el astillero (p. ej. "Cog", "Galera").</summary>
    string NombreCasco { get; }

    /// <summary>Puntos de vida base antes de aplicar módulos.</summary>
    int VidaBase { get; }

    /// <summary>Velocidad de navegación base antes de aplicar módulos.</summary>
    int VelocidadBase { get; }

    /// <summary>Maniobrabilidad base antes de aplicar módulos.</summary>
    int ManiobrabilidadBase { get; }

    /// <summary>Capacidad de carga máxima base antes de aplicar módulos.</summary>
    int CapacidadCargaBase { get; }

    /// <summary>Número de slots disponibles para instalar módulos en este casco.</summary>
    int CapacidadModulos { get; }

    /// <summary>Número máximo de tripulantes que puede embarcar este tipo de casco.</summary>
    int CapacidadTripulacion { get; }

    /// <summary>Precio base en oro que cobra el astillero por la mano de obra de construcción.</summary>
    int CosteOro { get; }
}
