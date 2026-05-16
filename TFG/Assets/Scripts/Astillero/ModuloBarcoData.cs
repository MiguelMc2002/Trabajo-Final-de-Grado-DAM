using UnityEngine;

/// <summary>
/// ScriptableObject que define las estadísticas y costes de un módulo instalable en un barco.
/// Crear desde el menú Assets → Create → TFG → Astillero → ModuloBarco.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Astillero/ModuloBarco", fileName = "NuevoModulo")]
public class ModuloBarcoData : ScriptableObject
{
    /// <summary>Año de juego a partir del cual la pólvora está disponible.</summary>
    public const int AnioDesbloqueoPolvoraJuego = 1380;

    /// <summary>Nombre del módulo mostrado en la UI del astillero.</summary>
    public string nombreModulo;

    /// <summary>Categoría funcional del módulo: Armamento, Velas o Bodega.</summary>
    public TipoModulo tipoModulo;

    /// <summary>Número de slots del casco que ocupa este módulo (normalmente 1 o 2).</summary>
    public int slotsCosto;

    /// <summary>Modificador de puntos de vida que aplica este módulo. Puede ser negativo.</summary>
    public int deltaVida;

    /// <summary>Modificador de velocidad que aplica este módulo.</summary>
    public int deltaVelocidad;

    /// <summary>Modificador de maniobrabilidad que aplica este módulo.</summary>
    public int deltaManiobrabilidad;

    /// <summary>Modificador de capacidad máxima de carga que aplica este módulo.</summary>
    public int deltaCargaMaxima;

    /// <summary>Modificador de fuerza de combate (cañones) que aplica este módulo.</summary>
    public int deltaFuerzaCombate;

    /// <summary>Unidades de madera necesarias para instalar este módulo.</summary>
    public int costeMadera;

    /// <summary>Unidades de hierro necesarias para instalar este módulo.</summary>
    public int costeHierro;

    /// <summary>Unidades de herramientas necesarias para instalar este módulo.</summary>
    public int costeHerramientas;

    /// <summary>Precio en oro que cobra el astillero por instalar este módulo.</summary>
    public int costeOro;

    /// <summary>
    /// Si <c>true</c>, este módulo solo puede instalarse cuando
    /// <see cref="SimulacionTiempo.AñoActual"/> >= <see cref="AnioDesbloqueoPolvoraJuego"/>.
    /// </summary>
    public bool requierePolvora;

    /// <summary>Icono del módulo para mostrar en la interfaz del astillero.</summary>
    public Sprite iconoModulo;
}
