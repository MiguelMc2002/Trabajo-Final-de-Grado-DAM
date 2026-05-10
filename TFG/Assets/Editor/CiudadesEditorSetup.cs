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

        CrearCiudad("Lubeck", "Lübeck", new Vector3Int(-4, 0, 0), new[]
        {
            ("Grano",              250, 500, 10, 8),
            ("Madera",             200, 400,  8, 6),
            ("Pescado",            225, 450,  9, 7),
            ("Lana",               175, 350,  7, 5),
            ("Mineral de hierro",  150, 300,  6, 4),
        });

        CrearCiudad("Barcelona", "Barcelona", new Vector3Int(-16, -21, 0), new[]
        {
            ("Grano",              300, 600, 15, 10),
            ("Madera",             200, 400,  8,  5),
            ("Pescado",            350, 700, 20, 12),
            ("Lana",               400, 800, 25,  8),
            ("Mineral de hierro",  100, 200,  3,  6),
        });

        CrearCiudad("Genova", "Génova", new Vector3Int(-8, -16, 0), new[]
        {
            ("Grano",              200, 400, 3, 5),
            ("Madera",             150, 300, 2, 3),
            ("Pescado",            400, 800, 8, 4),
            ("Lana",               250, 500, 4, 6),
            ("Mineral de hierro",  100, 200, 1, 3),
        });

        CrearCiudad("Venecia", "Venecia", new Vector3Int(-5, -15, 0), new[]
        {
            ("Grano",              180, 360, 2, 6),
            ("Madera",             120, 240, 1, 4),
            ("Pescado",            350, 700, 6, 5),
            ("Lana",               300, 600, 5, 7),
            ("Mineral de hierro",   80, 160, 1, 4),
        });

        CrearCiudad("Ruan", "Ruan", new Vector3Int(-16, -8, 0), new[]
        {
            ("Grano",              450, 900, 10, 8),
            ("Madera",             380, 760,  8, 5),
            ("Pescado",            200, 400,  3, 7),
            ("Lana",               500,1000, 12, 6),
            ("Mineral de hierro",  150, 300,  3, 4),
        });

        CrearCiudad("Brujas", "Brujas", new Vector3Int(-12, -4, 0), new[]
        {
            ("Grano",              350, 700,  7, 7),
            ("Madera",             280, 560,  5, 5),
            ("Pescado",            300, 600,  6, 6),
            ("Lana",               600,1000, 15, 8),
            ("Mineral de hierro",  200, 400,  4, 5),
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
        Vector3Int casillaMapamundi,
        (string fichero, int stockActual, int stockMax, int produccion, int consumo)[] entradas)
    {
        string ruta = $"{RutaCiudades}/{nombreFichero}.asset";

        CiudadData asset = AssetDatabase.LoadAssetAtPath<CiudadData>(ruta);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CiudadData>();
            AssetDatabase.CreateAsset(asset, ruta);
        }

        asset.NombreCiudad      = nombreMostrar;
        asset.CasillaMapamundi  = casillaMapamundi;
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
