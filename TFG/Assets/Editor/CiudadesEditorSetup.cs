#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidad de editor para generar los <see cref="CiudadData"/> ScriptableObjects
/// de las ciudades de la beta con su mercado inicial preconfigurado.
/// Acceder desde: menú Unity → TFG → Crear Assets de Ciudades.
/// Solo se ejecuta en el editor; no afecta a las builds.
/// Para añadir una ciudad nueva basta con llamar a <see cref="CrearCiudad"/> una vez más
/// dentro de <see cref="CrearAssetsCiudades"/>.
/// </summary>
public static class CiudadesEditorSetup
{
    private const string RutaBienes   = "Assets/ScriptableObjects/Bienes";
    private const string RutaCiudades = "Assets/ScriptableObjects/Ciudades";
    private const string RutaPadre    = "Assets/ScriptableObjects";

    /// <summary>
    /// Crea o sobreescribe los assets de todas las ciudades de la beta dentro de
    /// <c>Assets/ScriptableObjects/Ciudades/</c>.
    /// Crea la carpeta <c>Ciudades</c> si no existe.
    /// </summary>
    [MenuItem("TFG/Crear Assets de Ciudades")]
    public static void CrearAssetsCiudades()
    {
        AsegurarCarpeta();

        CrearCiudad("Lubeck", "Lübeck", new[]
        {
            ("Grano",              250, 500, 10, 8),
            ("Madera",             200, 400,  8, 6),
            ("Pescado",            225, 450,  9, 7),
            ("Lana",               175, 350,  7, 5),
            ("Mineral de hierro",  150, 300,  6, 4),
        });

        CrearCiudad("Barcelona", "Barcelona", new[]
        {
            ("Grano",              300, 600, 15, 10),
            ("Madera",             200, 400,  8,  5),
            ("Pescado",            350, 700, 20, 12),
            ("Lana",               400, 800, 25,  8),
            ("Mineral de hierro",  100, 200,  3,  6),
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CiudadesEditorSetup] Assets de ciudades generados correctamente.");
    }

    // ─── Creación de assets ───────────────────────────────────────────────────

    /// <summary>
    /// Crea o actualiza el asset <see cref="CiudadData"/> de una ciudad con los bienes
    /// de mercado indicados.
    /// </summary>
    /// <param name="nombreFichero">Nombre del fichero .asset sin extensión (p. ej. "Lubeck").</param>
    /// <param name="nombreMostrar">Nombre de la ciudad que verá el jugador (p. ej. "Lübeck").</param>
    /// <param name="entradas">
    /// Tuplas con los datos de cada bien: nombre del fichero BienData, stock actual,
    /// stock máximo, producción diaria y consumo diario.
    /// </param>
    private static void CrearCiudad(
        string nombreFichero,
        string nombreMostrar,
        (string fichero, int stockActual, int stockMax, int produccion, int consumo)[] entradas)
    {
        string ruta = $"{RutaCiudades}/{nombreFichero}.asset";

        CiudadData asset = AssetDatabase.LoadAssetAtPath<CiudadData>(ruta);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CiudadData>();
            AssetDatabase.CreateAsset(asset, ruta);
        }

        asset.NombreCiudad = nombreMostrar;
        asset.Mercado.Clear();

        foreach (var (fichero, stockActual, stockMax, produccion, consumo) in entradas)
        {
            BienData bien = AssetDatabase.LoadAssetAtPath<BienData>($"{RutaBienes}/{fichero}.asset");

            if (bien == null)
            {
                Debug.LogWarning($"[CiudadesEditorSetup] No se encontró '{fichero}.asset' en {RutaBienes}. Ejecuta primero 'TFG/Crear Bienes Primarios'.");
                continue;
            }

            asset.Mercado.Add(new EntradaMercado
            {
                Bien             = bien,
                StockActual      = stockActual,
                StockMax         = stockMax,
                ProduccionDiaria = produccion,
                ConsumoDiario    = consumo
            });
        }

        EditorUtility.SetDirty(asset);
        Debug.Log($"[CiudadesEditorSetup] Asset creado/actualizado: {ruta}");
    }

    // ─── Utilidades ───────────────────────────────────────────────────────────

    /// <summary>
    /// Crea la carpeta <c>Assets/ScriptableObjects/Ciudades</c> si no existe.
    /// Usa <see cref="AssetDatabase.CreateFolder"/> para que Unity registre el asset de carpeta.
    /// </summary>
    private static void AsegurarCarpeta()
    {
        if (!AssetDatabase.IsValidFolder(RutaCiudades))
        {
            AssetDatabase.CreateFolder(RutaPadre, "Ciudades");
            Debug.Log($"[CiudadesEditorSetup] Carpeta creada: {RutaCiudades}");
        }
    }
}
#endif
