#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidad de editor para generar los <see cref="CiudadData"/> ScriptableObjects
/// de las ciudades de la beta con su mercado inicial preconfigurado.
/// Acceder desde: menú Unity → TFG → Crear Assets de Ciudades.
/// Solo se ejecuta en el editor; no afecta a las builds.
/// </summary>
public static class CiudadesEditorSetup
{
    private const string RutaBienes   = "Assets/ScriptableObjects/Bienes";
    private const string RutaCiudades = "Assets/ScriptableObjects/Ciudades";
    private const string RutaPadre    = "Assets/ScriptableObjects";

    /// <summary>
    /// Crea o sobreescribe el asset <c>Lubeck.asset</c> dentro de
    /// <c>Assets/ScriptableObjects/Ciudades/</c> con los datos de mercado iniciales
    /// de Lübeck para la beta.
    /// Crea la carpeta <c>Ciudades</c> si no existe.
    /// </summary>
    [MenuItem("TFG/Crear Assets de Ciudades")]
    public static void CrearAssetsCiudades()
    {
        AsegurarCarpeta();
        CrearLubeck();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CiudadesEditorSetup] Assets de ciudades generados correctamente.");
    }

    // ─── Creación de assets ───────────────────────────────────────────────────

    /// <summary>
    /// Crea o actualiza el asset de la ciudad de Lübeck con sus cinco bienes de mercado.
    /// </summary>
    private static void CrearLubeck()
    {
        string ruta = $"{RutaCiudades}/Lubeck.asset";

        CiudadData asset = AssetDatabase.LoadAssetAtPath<CiudadData>(ruta);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CiudadData>();
            AssetDatabase.CreateAsset(asset, ruta);
        }

        asset.NombreCiudad = "Lübeck";
        asset.Mercado.Clear();

        // (nombreFichero, stockActual, stockMax, produccionDiaria, consumoDiario)
        var entradas = new (string fichero, int stockActual, int stockMax, int produccion, int consumo)[]
        {
            ("Grano",              250, 500, 10, 8),
            ("Madera",             200, 400,  8, 6),
            ("Pescado",            225, 450,  9, 7),
            ("Lana",               175, 350,  7, 5),
            ("Mineral de hierro",  150, 300,  6, 4),
        };

        foreach (var (fichero, stockActual, stockMax, produccion, consumo) in entradas)
        {
            BienData bien = AssetDatabase.LoadAssetAtPath<BienData>($"{RutaBienes}/{fichero}.asset");

            if (bien == null)
            {
                Debug.LogWarning($"[CiudadesEditorSetup] No se encontró el BienData '{fichero}.asset' en {RutaBienes}. Ejecuta primero 'TFG/Crear Bienes Primarios'.");
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
