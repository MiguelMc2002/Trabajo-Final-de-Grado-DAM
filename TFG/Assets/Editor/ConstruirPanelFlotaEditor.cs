#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script de editor desechable. Genera la jerarquía completa de UI para
/// PanelFlota en la escena activa y cablea todos los campos [SerializeField]
/// de PanelFlotaUI.
/// Menú: TFG → Construir Panel Flota Ciudad.
/// </summary>
public static class ConstruirPanelFlotaEditor
{
    private static readonly Color ColorDorado = new Color(0.545f, 0.412f, 0.078f, 1f);  // #8B6914
    private static readonly Color ColorRojo   = new Color(0.6f,   0.1f,   0.1f,   1f);

    // =========================================================================
    //  MENÚ
    // =========================================================================

    [MenuItem("TFG/Construir Panel Flota Ciudad")]
    static void BuildPanelFlota()
    {
        GameObject root = BuscarIncluyendoInactivos("PanelFlota");
        if (root == null)
        {
            Debug.LogError("[UIBuilder] No se encontró 'PanelFlota' en la escena.");
            return;
        }

        LimpiarHijos(root.transform);
        AddVLG(root, 0f);

        PanelFlotaUI ui = root.GetComponent<PanelFlotaUI>() ?? root.AddComponent<PanelFlotaUI>();
        var so = new SerializedObject(ui);

        Prop(so, "_panelFlota").objectReferenceValue = root;

        TMP_FontAsset cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/Cinzel-Regular SDF.asset");

        // ── SubpanelInfo ──────────────────────────────────────────────────────
        var subInfo = CreateChild(root.transform, "SubpanelInfo", 600f);
        AddVLG(subInfo, 6f);
        Prop(so, "_panelInfoBarco").objectReferenceValue = subInfo;

        // Fila título + botón cerrar
        var filaTitulo = CreateChild(subInfo.transform, "FilaTitulo", 35f);
        AddHLG(filaTitulo);
        AddTMP(CreateFlexChild(filaTitulo.transform, "TxtTituloFlota"), "— FLOTA —", 15, cinzel);
        Prop(so, "_btnCerrar").objectReferenceValue =
            AddColorButton(CreateFixedChild(filaTitulo.transform, "BtnCerrar", 60f), "X", ColorRojo, cinzel);

        // Fila navegación: < NombreBarco >
        var filaNombre = CreateChild(subInfo.transform, "FilaNombreBarco", 30f);
        AddHLG(filaNombre);
        Prop(so, "_btnAnterior").objectReferenceValue =
            AddColorButton(CreateFixedChild(filaNombre.transform, "BtnAnterior", 36f), "<", ColorDorado, cinzel);
        Prop(so, "_textoNombreBarco").objectReferenceValue =
            AddTMP(CreateFlexChild(filaNombre.transform, "TxtNombreBarco"), "Nombre barco", 13, cinzel);
        Prop(so, "_btnSiguiente").objectReferenceValue =
            AddColorButton(CreateFixedChild(filaNombre.transform, "BtnSiguiente", 36f), ">", ColorDorado, cinzel);

        // Índice barco
        Prop(so, "_txtIndiceBarco").objectReferenceValue =
            AddTMP(CreateChild(subInfo.transform, "TxtIndiceBarco", 18f), "Barco 1 / 1", 10, cinzel);

        // Textos de estadísticas
        Prop(so, "_textoCasco").objectReferenceValue       = AddTMP(CreateChild(subInfo.transform, "TxtCasco",       18f), "Tipo casco",  11, cinzel);
        Prop(so, "_textoVida").objectReferenceValue        = AddTMP(CreateChild(subInfo.transform, "TxtVida",        18f), "Vida: 0/0",   11, cinzel);
        Prop(so, "_textoVelocidad").objectReferenceValue   = AddTMP(CreateChild(subInfo.transform, "TxtVelocidad",   18f), "Vel: 0",      11, cinzel);
        Prop(so, "_textoManiobra").objectReferenceValue    = AddTMP(CreateChild(subInfo.transform, "TxtManiobra",    18f), "Manio: 0",    11, cinzel);
        Prop(so, "_textoCarga").objectReferenceValue       = AddTMP(CreateChild(subInfo.transform, "TxtCarga",       18f), "Carga: 0/0",  11, cinzel);
        Prop(so, "_textoFuerza").objectReferenceValue      = AddTMP(CreateChild(subInfo.transform, "TxtFuerza",      18f), "Fuerza: 0",   11, cinzel);
        Prop(so, "_textoTripulacion").objectReferenceValue = AddTMP(CreateChild(subInfo.transform, "TxtTripulacion", 18f), "Trip: 0/0",   11, cinzel);
        Prop(so, "_textoModulos").objectReferenceValue     = AddTMP(CreateChild(subInfo.transform, "TxtModulos",     16f), "Módulos: —",  10, cinzel);
        Prop(so, "_textoCapitan").objectReferenceValue     = AddTMP(CreateChild(subInfo.transform, "TxtCapitan",     16f), "Sin capitán", 10, cinzel);
        Prop(so, "_textoConvoy").objectReferenceValue      = AddTMP(CreateChild(subInfo.transform, "TxtConvoy",      16f), "Sin convoy",  10, cinzel);

        // Botón ver bodega
        Prop(so, "_btnVerBodega").objectReferenceValue =
            AddColorButton(CreateChild(subInfo.transform, "BtnVerBodega", 40f), "Ver Bodega", ColorDorado, cinzel);

        // Fila modo pirata (toggle + etiqueta)
        var filaPirata = CreateChild(subInfo.transform, "FilaModoPirata", 30f);
        AddHLG(filaPirata);
        var toggleGo = CreateFixedChild(filaPirata.transform, "ToggleModoPirata", 24f);
        Prop(so, "_toggleModoPirata").objectReferenceValue = toggleGo.AddComponent<Toggle>();
        AddTMP(CreateFlexChild(filaPirata.transform, "LblModoPirata"), "Modo Pirata", 11, cinzel);

        // ── SubpanelBodega ────────────────────────────────────────────────────
        var subBodega = CreateChild(root.transform, "SubpanelBodega", 300f);
        AddVLG(subBodega, 6f);
        Prop(so, "_panelBodega").objectReferenceValue = subBodega;

        AddTMP(CreateChild(subBodega.transform, "TxtTituloBodega", 28f), "Bodega del jugador", 14, cinzel);

        // ScrollRect
        var scrollGO = new GameObject("ScrollRectBodega", typeof(RectTransform));
        scrollGO.transform.SetParent(subBodega.transform, false);
        scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
        var leScroll = scrollGO.AddComponent<LayoutElement>();
        leScroll.flexibleHeight = 1f;
        leScroll.flexibleWidth  = 1f;

        ScrollRect scrollRect   = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal   = false;
        scrollRect.vertical     = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        viewportGO.AddComponent<Image>().color = Color.clear;
        var viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        var viewportRT   = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        // Contenedor de filas de bodega
        var contenedorGO = new GameObject("ContenedorBodega", typeof(RectTransform));
        contenedorGO.transform.SetParent(viewportGO.transform, false);
        var vlgCont = contenedorGO.AddComponent<VerticalLayoutGroup>();
        vlgCont.childControlWidth     = true;
        vlgCont.childControlHeight    = true;
        vlgCont.childForceExpandWidth = true;
        vlgCont.spacing = 2f;
        var csf = contenedorGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        var contRT   = contenedorGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0f, 1f);
        contRT.anchorMax = new Vector2(1f, 1f);
        contRT.pivot     = new Vector2(0.5f, 1f);
        contRT.offsetMin = Vector2.zero;
        contRT.offsetMax = Vector2.zero;

        scrollRect.viewport = viewportRT;
        scrollRect.content  = contRT;

        Prop(so, "_contenedorBodega").objectReferenceValue = contenedorGO.transform;

        // Botón volver desde bodega
        Prop(so, "_btnVolver").objectReferenceValue =
            AddColorButton(CreateChild(subBodega.transform, "BtnVolver", 40f), "◄ Volver", ColorDorado, cinzel);

        so.ApplyModifiedProperties();
        subBodega.SetActive(false);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[UIBuilder] ✓ PanelFlota construido y cableado. Guarda la escena (Ctrl+S).");
    }

    // =========================================================================
    //  HELPERS — búsqueda
    // =========================================================================

    /// <summary>Busca un GameObject por nombre incluyendo objetos inactivos de la escena activa.</summary>
    static GameObject BuscarIncluyendoInactivos(string nombre)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.scene.isLoaded && go.name == nombre)
                return go;
        return null;
    }

    // =========================================================================
    //  HELPERS — construcción
    // =========================================================================

    /// <summary>Elimina todos los hijos de <paramref name="t"/> con DestroyImmediate.</summary>
    static void LimpiarHijos(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    /// <summary>Hijo con LayoutElement de altura preferida y anchura flexible.</summary>
    static GameObject CreateChild(Transform parent, string nombre, float height = 30f)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le             = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;
        return go;
    }

    /// <summary>Hijo con anchura y altura flexibles (columnas dentro de un HLG).</summary>
    static GameObject CreateFlexChild(Transform parent, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le            = go.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1f;
        le.flexibleHeight = 1f;
        return go;
    }

    /// <summary>Hijo con anchura fija (botones laterales dentro de un HLG).</summary>
    static GameObject CreateFixedChild(Transform parent, string nombre, float width)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le            = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.flexibleWidth  = 0f;
        le.flexibleHeight = 1f;
        return go;
    }

    /// <summary>TMP blanco centrado con fuente opcional.</summary>
    static TextMeshProUGUI AddTMP(GameObject go, string texto, int size = 14, TMP_FontAsset font = null)
    {
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = size;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return tmp;
    }

    /// <summary>Button con color de fondo, texto blanco y fuente opcional.</summary>
    static Button AddColorButton(GameObject go, string label, Color bgColor, TMP_FontAsset font)
    {
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var rt       = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp       = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 13;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;

        return btn;
    }

    /// <summary>VerticalLayoutGroup con childControlWidth=true y childForceExpandHeight=false.</summary>
    static VerticalLayoutGroup AddVLG(GameObject go, float spacing = 5f)
    {
        // GetComponent evita el NPE de DisallowMultipleComponent en re-ejecuciones
        var vlg = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        return vlg;
    }

    /// <summary>HorizontalLayoutGroup con childForceExpandHeight=true.</summary>
    static HorizontalLayoutGroup AddHLG(GameObject go, float spacing = 4f)
    {
        // GetComponent evita el NPE de DisallowMultipleComponent en re-ejecuciones
        var hlg = go.GetComponent<HorizontalLayoutGroup>() ?? go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = spacing;
        return hlg;
    }

    // =========================================================================
    //  HELPERS — cableado
    // =========================================================================

    /// <summary>
    /// Devuelve la SerializedProperty con nombre <paramref name="fieldName"/>.
    /// Registra un warning si no se encuentra.
    /// </summary>
    static SerializedProperty Prop(SerializedObject so, string fieldName)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
            Debug.LogWarning($"[UIBuilder] Campo no encontrado en el componente: '{fieldName}'");
        return prop;
    }
}
#endif
