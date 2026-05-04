using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de editor que corrige el color de todos los textos dentro de los
/// paneles de slots (PanelSlots) en la escena activa.
/// Aplica colores medievales coherentes según el nombre de cada texto:
/// dorado para títulos y nombres de partida, gris para fechas y días, blanco para botones.
/// Ejecutar desde TFG → Reparar → Colorear todos los textos del PanelSlots.
/// </summary>
public static class RepararColoresSlots
{
    private static readonly Color32 ColorDorado = new Color32(0xC8, 0xA8, 0x4B, 0xFF);
    private static readonly Color32 ColorGris   = new Color32(0xAA, 0xAA, 0xAA, 0xFF);
    private static readonly Color32 ColorBlanco = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    [MenuItem("TFG/Reparar/Colorear todos los textos del PanelSlots")]
    public static void Reparar()
    {
        GameObject[] raices = new[]
        {
            GameObject.Find("PanelSlots (Pantalla Completa)"),
            GameObject.Find("PanelSlots (Popup)")
        };

        int totalProcesados = 0;

        foreach (GameObject raiz in raices)
        {
            if (raiz == null) continue;

            TMP_Text[] textos = raiz.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text texto in textos)
            {
                Color32 color = ResolverColor(texto.gameObject.name);

                Undo.RecordObject(texto, "Colorear texto slot");
                texto.color = color;
                texto.SetAllDirty();
                EditorUtility.SetDirty(texto);

                totalProcesados++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[RepararColoresSlots] {totalProcesados} texto(s) procesados. Guarda la escena con Ctrl+S.");
    }

    /// <summary>
    /// Devuelve el color que corresponde a un texto según su nombre en la jerarquía.
    /// </summary>
    /// <param name="nombreGO">Nombre del GameObject que contiene el TMP_Text.</param>
    /// <returns>Color asignado al texto.</returns>
    private static Color32 ResolverColor(string nombreGO)
    {
        switch (nombreGO)
        {
            case "Titulo":
            case "TextoNombre":
                return ColorDorado;
            case "TextoFecha":
            case "TextoDias":
                return ColorGris;
            default:
                return ColorBlanco;
        }
    }
}
