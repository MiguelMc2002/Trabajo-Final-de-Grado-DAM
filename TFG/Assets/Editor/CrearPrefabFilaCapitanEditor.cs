#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Herramienta de editor para generar el prefab FilaCapitan y asignarlo
/// automáticamente al campo _prefabFilaCapitan del componente TabernaUI
/// presente en la escena activa.
/// </summary>
public static class CrearPrefabFilaCapitanEditor
{
    /// <summary>
    /// Crea el prefab Assets/Prefabs/UI/FilaCapitan.prefab con la estructura
    /// requerida por TabernaUI (un TextMeshProUGUI y un Button como hijos),
    /// lo guarda en disco y lo asigna al campo serializado _prefabFilaCapitan
    /// del primer TabernaUI encontrado en la escena activa.
    /// </summary>
    [MenuItem("TFG/Crear Prefab FilaCapitan")]
    public static void CrearPrefabFilaCapitan()
    {
        // ── Raíz ────────────────────────────────────────────────────────────
        GameObject raiz = new GameObject("FilaCapitan");

        RectTransform rtRaiz = raiz.AddComponent<RectTransform>();
        rtRaiz.sizeDelta = new Vector2(400f, 50f);

        Image imgFondo = raiz.AddComponent<Image>();
        imgFondo.color = new Color32(50, 50, 50, 200);

        HorizontalLayoutGroup hlg = raiz.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 0, 0);
        hlg.spacing = 8f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;

        // ── Hijo: TxtNombreCapitan ───────────────────────────────────────────
        GameObject goTexto = new GameObject("TxtNombreCapitan");
        goTexto.transform.SetParent(raiz.transform, false);
        goTexto.AddComponent<RectTransform>();

        TextMeshProUGUI tmp = goTexto.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 14f;
        tmp.color     = Color.white;
        tmp.text      = "Nombre Capitán";

        LayoutElement leTexto = goTexto.AddComponent<LayoutElement>();
        leTexto.flexibleWidth = 1f;

        // ── Hijo: BtnContratar ───────────────────────────────────────────────
        GameObject goBtn = new GameObject("BtnContratar");
        goBtn.transform.SetParent(raiz.transform, false);
        goBtn.AddComponent<RectTransform>();

        Image imgBtn = goBtn.AddComponent<Image>();
        imgBtn.color = new Color32(180, 140, 60, 255);

        goBtn.AddComponent<Button>();

        LayoutElement leBtn = goBtn.AddComponent<LayoutElement>();
        leBtn.preferredWidth = 90f;
        leBtn.flexibleWidth  = 0f;

        // Texto del botón
        GameObject goTxtBtn = new GameObject("Text");
        goTxtBtn.transform.SetParent(goBtn.transform, false);

        RectTransform rtTxtBtn = goTxtBtn.AddComponent<RectTransform>();
        rtTxtBtn.anchorMin = Vector2.zero;
        rtTxtBtn.anchorMax = Vector2.one;
        rtTxtBtn.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpBtn = goTxtBtn.AddComponent<TextMeshProUGUI>();
        tmpBtn.text      = "Contratar";
        tmpBtn.fontSize  = 13f;
        tmpBtn.color     = Color.white;
        tmpBtn.alignment = TextAlignmentOptions.Center;

        // ── Guardar prefab ───────────────────────────────────────────────────
        const string ruta = "Assets/Prefabs/UI/FilaCapitan.prefab";
        GameObject prefabGuardado = null;
        bool exito = false;
        string mensajeError = string.Empty;

        try
        {
            prefabGuardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            exito = prefabGuardado != null;
        }
        catch (System.Exception e)
        {
            mensajeError = e.Message;
        }
        finally
        {
            Object.DestroyImmediate(raiz);
        }

        if (!exito)
        {
            EditorUtility.DisplayDialog(
                "TFG — Error",
                $"No se pudo guardar el prefab en {ruta}.\n{mensajeError}",
                "Cerrar");
            return;
        }

        // ── Asignar al TabernaUI de la escena activa ─────────────────────────
        TabernaUI taberna = Object.FindFirstObjectByType<TabernaUI>();
        if (taberna == null)
        {
            EditorUtility.DisplayDialog(
                "TFG — Aviso",
                $"Prefab guardado en {ruta}, pero no se encontró ningún TabernaUI en la escena activa.\nAsígnalo manualmente.",
                "Cerrar");
            return;
        }

        SerializedObject so = new SerializedObject(taberna);
        SerializedProperty prop = so.FindProperty("_prefabFilaCapitan");
        if (prop == null)
        {
            EditorUtility.DisplayDialog(
                "TFG — Aviso",
                $"Prefab guardado en {ruta}, pero no se encontró la propiedad '_prefabFilaCapitan' en TabernaUI.\nAsígnalo manualmente.",
                "Cerrar");
            return;
        }

        prop.objectReferenceValue = prefabGuardado;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(taberna.gameObject.scene);

        EditorUtility.DisplayDialog(
            "TFG — Éxito",
            $"Prefab creado en {ruta} y asignado a TabernaUI en '{taberna.gameObject.scene.name}'.\nGuarda la escena para conservar el cambio.",
            "Aceptar");
    }
}
#endif
