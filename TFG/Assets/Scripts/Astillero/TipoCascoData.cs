using UnityEngine;

/// <summary>
/// ScriptableObject que define las estadísticas base y los costes de construcción
/// de un tipo de casco de barco. Crear desde el menú
/// Assets → Create → TFG → Astillero → TipoCasco.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Astillero/TipoCasco", fileName = "NuevoTipoCasco")]
public class TipoCascoData : ScriptableObject, IBarco
{
    /// <summary>Identificador único que coincide con <c>id_tipo_casco</c> en la base de datos.</summary>
    public int idTipoCasco;

    /// <summary>Nombre del casco mostrado en el astillero (p. ej. "Cog", "Galera").</summary>
    public string nombreCasco;

    /// <summary>Puntos de vida base antes de aplicar módulos.</summary>
    public int vidaBase;

    /// <summary>Velocidad de navegación base antes de aplicar módulos.</summary>
    public int velocidadBase;

    /// <summary>Maniobrabilidad base antes de aplicar módulos.</summary>
    public int maniobrabilidadBase;

    /// <summary>Capacidad de carga máxima base antes de aplicar módulos.</summary>
    public int capacidadCargaBase;

    /// <summary>
    /// Número de slots disponibles para instalar módulos en este casco.
    /// Cog=4, Hulk=6, Carraca=5, Galera=3.
    /// </summary>
    public int capacidadModulos;

    /// <summary>Unidades de madera necesarias para construir este tipo de barco.</summary>
    public int costeMadera;

    /// <summary>Unidades de hierro necesarias para construir este tipo de barco.</summary>
    public int costeHierro;

    /// <summary>Unidades de herramientas necesarias para construir este tipo de barco.</summary>
    public int costeHerramientas;

    /// <summary>Precio base en oro que cobra el astillero por la mano de obra.</summary>
    public int costeOro;

    /// <summary>Número máximo de tripulantes que puede embarcar este tipo de casco.</summary>
    public int capacidadTripulacion;

    /// <summary>Icono del casco para mostrar en la interfaz del astillero.</summary>
    public Sprite iconoCasco;

    // ─── IBarco ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public int    IdTipoCasco          => idTipoCasco;
    /// <inheritdoc/>
    public string NombreCasco          => nombreCasco;
    /// <inheritdoc/>
    public int    VidaBase             => vidaBase;
    /// <inheritdoc/>
    public int    VelocidadBase        => velocidadBase;
    /// <inheritdoc/>
    public int    ManiobrabilidadBase  => maniobrabilidadBase;
    /// <inheritdoc/>
    public int    CapacidadCargaBase   => capacidadCargaBase;
    /// <inheritdoc/>
    public int    CapacidadModulos     => capacidadModulos;
    /// <inheritdoc/>
    public int    CapacidadTripulacion => capacidadTripulacion;
    /// <inheritdoc/>
    public int    CosteOro             => costeOro;
}
