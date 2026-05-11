#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidad de editor para generar los <see cref="BienData"/> ScriptableObjects
/// del catálogo completo de bienes del juego con sus valores predeterminados.
/// Acceder desde: menú Unity → TFG → Crear Bienes Primarios.
/// Solo se ejecuta en el editor; no afecta a las builds.
/// Verifica si cada asset ya existe antes de crearlo; los existentes solo se actualizan
/// si sus valores han cambiado. Al terminar informa en consola cuántos se crearon
/// y cuántos ya existían.
/// </summary>
public static class BienesEditorSetup
{
    private const string RutaBase = "Assets/ScriptableObjects/Bienes";

    /// <summary>
    /// Crea o actualiza los assets de los 12 bienes del catálogo completo:
    /// 7 primarios, 3 intermedios y 2 avanzados. Los assets ya existentes no se
    /// sobreescriben si sus valores coinciden; se marcan como sucios solo si cambiaron.
    /// </summary>
    [MenuItem("TFG/Crear Bienes Primarios")]
    public static void CrearBienesPrimarios()
    {
        // (nombre, categoría, precioBase, stockMaximo)
        var bienes = new (string nombre, CategoriaBien categoria, float precioBase, int stockMaximo)[]
        {
            // Primarios
            ("Grano",             CategoriaBien.Primario,    10f,  500),
            ("Madera",            CategoriaBien.Primario,    15f,  400),
            ("Pescado",           CategoriaBien.Primario,    12f,  450),
            ("Lana",              CategoriaBien.Primario,    18f,  350),
            ("Mineral de hierro", CategoriaBien.Primario,    25f,  300),
            ("Sal",               CategoriaBien.Primario,     8f,  400),
            ("Cera",              CategoriaBien.Primario,    20f,  250),
            // Intermedios
            ("Tela",              CategoriaBien.Intermedio,  35f,  300),
            ("Herramientas",      CategoriaBien.Intermedio,  40f,  200),
            ("Cerveza",           CategoriaBien.Intermedio,  14f,  350),
            // Avanzados
            ("Especias",          CategoriaBien.Avanzado,    80f,  150),
            ("Seda",              CategoriaBien.Avanzado,   100f,  100),
        };

        int creados    = 0;
        int existentes = 0;

        foreach (var (nombre, categoria, precioBase, stockMaximo) in bienes)
        {
            string ruta  = $"{RutaBase}/{nombre}.asset";
            bool   nuevo = false;

            BienData asset = AssetDatabase.LoadAssetAtPath<BienData>(ruta);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BienData>();
                AssetDatabase.CreateAsset(asset, ruta);
                nuevo = true;
                creados++;
            }
            else
            {
                existentes++;
            }

            asset.nombre      = nombre;
            asset.categoria   = categoria;
            asset.precioBase  = precioBase;
            asset.stockMaximo = stockMaximo;

            EditorUtility.SetDirty(asset);
            Debug.Log($"[BienesEditorSetup] {(nuevo ? "Creado" : "Actualizado")}: {ruta}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BienesEditorSetup] Completado — creados: {creados}, ya existían: {existentes}.");
    }
}
#endif
