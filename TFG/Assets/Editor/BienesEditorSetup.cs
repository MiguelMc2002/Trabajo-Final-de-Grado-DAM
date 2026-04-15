#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidad de editor para generar los <see cref="BienData"/> ScriptableObjects
/// de los bienes primarios de la beta con sus valores predeterminados.
/// Acceder desde: menú Unity → TFG → Crear Bienes Primarios.
/// Solo se ejecuta en el editor; no afecta a las builds.
/// </summary>
public static class BienesEditorSetup
{
    private const string RutaBase = "Assets/ScriptableObjects/Bienes";

    /// <summary>
    /// Crea o sobreescribe los assets de bienes primarios de la beta
    /// con sus valores de precio base y stock máximo predeterminados.
    /// </summary>
    [MenuItem("TFG/Crear Bienes Primarios")]
    public static void CrearBienesPrimarios()
    {
        // (nombre, precioBase, stockMaximo)
        var bienes = new (string nombre, float precioBase, int stockMaximo)[]
        {
            ("Grano",              10f,  500),
            ("Madera",             15f,  400),
            ("Pescado",            12f,  450),
            ("Lana",               18f,  350),
            ("Mineral de hierro",  25f,  300),
        };

        foreach (var (nombre, precioBase, stockMaximo) in bienes)
        {
            string ruta = $"{RutaBase}/{nombre}.asset";

            BienData asset = AssetDatabase.LoadAssetAtPath<BienData>(ruta);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BienData>();
                AssetDatabase.CreateAsset(asset, ruta);
            }

            asset.nombre      = nombre;
            asset.categoria   = CategoriaBien.Primario;
            asset.precioBase  = precioBase;
            asset.stockMaximo = stockMaximo;

            EditorUtility.SetDirty(asset);
            Debug.Log($"[BienesEditorSetup] Asset creado/actualizado: {ruta}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BienesEditorSetup] Bienes primarios generados correctamente.");
    }
}
#endif
