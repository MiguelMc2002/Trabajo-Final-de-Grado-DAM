using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Herramienta de editor que construye y cablea automáticamente el HUD de dinero
/// en la escena activa. Compatible con Ciudad y Mapamundi. Menú: TFG → Construir HUD Dinero.
/// </summary>
public static class ConstruirHUDDineroEditor
{
    private const string RutaFuenteCinzel = "Assets/Fonts/static/Cinzel-Regular SDF.asset";

    [MenuItem("TFG/Construir HUD Dinero")]
    public static void ConstruirHUDDinero()
    {
        string nombreEscena = SceneManager.GetActiveScene().name;

        // Avisar si la escena no es una de las dos escenas objetivo
        if (nombreEscena != "Ciudad" && nombreEscena != "Mapamundi")
            Debug.LogWarning($"[HUD Dinero] Escena inesperada '{nombreEscena}'. Se esperaba Ciudad o Mapamundi. El HUD se construirá igualmente.");

        // ── Canvas ───────────────────────────────────────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[HUD Dinero] Canvas no encontrado — se ha creado uno nuevo.");
        }

        // ── Contenedor HUDDinero ─────────────────────────────────────────────
        Transform hudTransform = canvas.transform.Find("HUDDinero");
        GameObject hudGO;
        if (hudTransform != null)
        {
            hudGO = hudTransform.gameObject;
            Debug.Log($"[HUD Dinero] HUDDinero ya existía en '{nombreEscena}' — se recablea.");
        }
        else
        {
            hudGO = new GameObject("HUDDinero", typeof(RectTransform));
            hudGO.transform.SetParent(canvas.transform, false);
        }

        // Anclar arriba a la derecha
        RectTransform hudRect = hudGO.GetComponent<RectTransform>();
        hudRect.anchorMin        = new Vector2(1f, 1f);
        hudRect.anchorMax        = new Vector2(1f, 1f);
        hudRect.pivot            = new Vector2(1f, 1f);
        hudRect.anchoredPosition = new Vector2(-20f, -20f);
        hudRect.sizeDelta        = Vector2.zero; // ContentSizeFitter controla el tamaño

        // HorizontalLayoutGroup
        HorizontalLayoutGroup hlg = hudGO.GetComponent<HorizontalLayoutGroup>()
                                 ?? hudGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 8f;
        hlg.childAlignment         = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(8, 8, 4, 4);

        // ContentSizeFitter para que el panel se adapte al texto
        ContentSizeFitter csf = hudGO.GetComponent<ContentSizeFitter>()
                              ?? hudGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // ── Icono de moneda (24×24, dorado #FFD700) ──────────────────────────
        Transform iconoTransform = hudGO.transform.Find("IconoMoneda");
        GameObject iconoGO;
        if (iconoTransform != null)
            iconoGO = iconoTransform.gameObject;
        else
        {
            iconoGO = new GameObject("IconoMoneda", typeof(RectTransform));
            iconoGO.transform.SetParent(hudGO.transform, false);
        }

        Image imgMoneda = iconoGO.GetComponent<Image>() ?? iconoGO.AddComponent<Image>();
        imgMoneda.color = new Color(1f, 215f / 255f, 0f); // #FFD700

        LayoutElement iconoLayout = iconoGO.GetComponent<LayoutElement>()
                                  ?? iconoGO.AddComponent<LayoutElement>();
        iconoLayout.preferredWidth  = 24f;
        iconoLayout.preferredHeight = 24f;
        iconoLayout.minWidth        = 24f;
        iconoLayout.minHeight       = 24f;

        // ── Texto de dinero ──────────────────────────────────────────────────
        Transform textoTransform = hudGO.transform.Find("TxtDinero");
        GameObject textoGO;
        if (textoTransform != null)
            textoGO = textoTransform.gameObject;
        else
        {
            textoGO = new GameObject("TxtDinero", typeof(RectTransform));
            textoGO.transform.SetParent(hudGO.transform, false);
        }

        TextMeshProUGUI txtDinero = textoGO.GetComponent<TextMeshProUGUI>()
                                  ?? textoGO.AddComponent<TextMeshProUGUI>();
        txtDinero.text      = "0 ƒ";
        txtDinero.fontSize   = 22f;
        txtDinero.color      = Color.white;
        txtDinero.alignment  = TextAlignmentOptions.Right;

        // Asignar fuente Cinzel si está disponible
        TMP_FontAsset cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuenteCinzel);
        if (cinzel != null)
            txtDinero.font = cinzel;
        else
            Debug.LogWarning($"[HUD Dinero] Fuente no encontrada en '{RutaFuenteCinzel}'. Se usará la fuente por defecto de TMP.");

        // ── Componente HUDDinero + cableado de campos privados ───────────────
        HUDDinero hudDinero = hudGO.GetComponent<HUDDinero>() ?? hudGO.AddComponent<HUDDinero>();

        SerializedObject so = new SerializedObject(hudDinero);
        so.FindProperty("_txtDinero").objectReferenceValue = txtDinero;
        so.FindProperty("_imgMoneda").objectReferenceValue = imgMoneda;
        so.ApplyModifiedProperties();

        // Marcar la escena como modificada para que Unity pida guardarla
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log($"[HUD Dinero] HUD construido y cableado correctamente en la escena '{nombreEscena}'.");
    }
}
