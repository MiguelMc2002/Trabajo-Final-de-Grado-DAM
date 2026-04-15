using UnityEngine;

/// <summary>
/// Define las propiedades estáticas de un bien comerciable en los mercados de la Liga Hanseática.
/// Actúa como plantilla inmutable; el estado dinámico —stock y precio actual— vive en <see cref="MarketManager"/>.
/// Crear instancias desde el menú: Assets → Create → TFG → Bien Comercial.
/// </summary>
[CreateAssetMenu(fileName = "NuevoBien", menuName = "TFG/Bien Comercial")]
public class BienData : ScriptableObject
{
    // ─── Identificación ──────────────────────────────────────────────────────

    /// <summary>
    /// Nombre visible del bien en la interfaz del mercado y del almacén del jugador
    /// (p. ej. "Grano", "Madera", "Pescado").
    /// </summary>
    [Header("Identificación")]
    public string nombre;

    /// <summary>
    /// Clasificación del bien en la cadena productiva:
    /// primario (materias primas), intermedio (semielaborados) o avanzado (manufacturados).
    /// </summary>
    public CategoriaBien categoria;

    // ─── Economía ────────────────────────────────────────────────────────────

    /// <summary>
    /// Precio de referencia en monedas de oro cuando el stock de la ciudad
    /// está exactamente al máximo. A partir de aquí la fórmula escala el precio
    /// hacia arriba (escasez) o lo mantiene en el mínimo (abundancia extrema).
    /// </summary>
    [Header("Economía")]
    [Min(1f)]
    public float precioBase;

    /// <summary>
    /// Unidades máximas que puede almacenar una ciudad de este bien.
    /// Determina el rango dinámico del precio: con stock 0 el precio es
    /// <c>precioBase × stockMaximo</c>; al llegar al máximo el precio iguala a <c>precioBase</c>.
    /// </summary>
    [Min(1)]
    public int stockMaximo;
}

/// <summary>
/// Clasificación de un bien según su posición en la cadena de producción.
/// </summary>
public enum CategoriaBien
{
    /// <summary>Materia prima directamente extraída (grano, madera, pescado…).</summary>
    Primario,
    /// <summary>Bien semielaborado fabricado a partir de materias primas (harina, tela…).</summary>
    Intermedio,
    /// <summary>Bien manufacturado de alto valor que requiere varias transformaciones (pan, ropa…).</summary>
    Avanzado
}
