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

        // ── SubpanelInfo ──────────────────────────────────────────────────────
        var subInfo = CreateChild(root.transform, "SubpanelInfo", 600f);
        AddVLG(subInfo, 6f);
        Prop(so, "_panelInfoBarco").objectReferenceValue = subInfo;

        // Fila título
        var filaTitulo = CreateChild(subInfo.transform, "FilaTitulo", 35f);
        AddHLG(filaTitulo);
        AddTMP(CreateFlexChild(filaTitulo.transform, "TxtTituloFlota"), "— FLOTA —", 15);
        Prop(so, "_btnCerrar").objectReferenceValue =
            AddButton(CreateFixedChild(filaTitulo.transform, "BtnCerrar", 60f), "X");

        // Textos de info
        Prop(so, "_textoNombreBarco").objectReferenceValue  = AddTMP(CreateChild(subInfo.transform, "TxtNombreBarco",  22f), "Nombre barco", 13);
        Prop(so, "_textoCasco").objectReferenceValue        = AddTMP(CreateChild(subInfo.transform, "TxtCasco",        18f), "Tipo casco",   11);
        Prop(so, "_textoVida").objectReferenceValue         = AddTMP(CreateChild(subInfo.transform, "TxtVida",         18f), "Vida: 0/0",    11);
        Prop(so, "_textoVelocidad").objectReferenceValue    = AddTMP(CreateChild(subInfo.transform, "TxtVelocidad",    18f), "Vel: 0",       11);
        Prop(so, "_textoManiobra").objectReferenceValue     = AddTMP(CreateChild(subInfo.transform, "TxtManiobra",     18f), "Manio: 0",     11);
        Prop(so, "_textoCarga").objectReferenceValue        = AddTMP(CreateChild(subInfo.transform, "TxtCarga",        18f), "Carga: 0/0",   11);
        Prop(so, "_textoFuerza").objectReferenceValue       = AddTMP(CreateChild(subInfo.transform, "TxtFuerza",       18f), "Fuerza: 0",    11);
        Prop(so, "_textoTripulacion").objectReferenceValue  = AddTMP(CreateChild(subInfo.transform, "TxtTripulacion",  18f), "Trip: 0/0",    11);
        Prop(so, "_textoModulos").objectReferenceValue      = AddTMP(CreateChild(subInfo.transform, "TxtModulos",      16f), "Módulos: —",   10);
        Prop(so, "_textoCapitan").objectReferenceValue      = AddTMP(CreateChild(subInfo.transform, "TxtCapitan",      16f), "Sin capitán",  10);
        Prop(so, "_textoConvoy").objectReferenceValue       = AddTMP(CreateChild(subInfo.transform, "TxtConvoy",       16f), "Sin convoy",   10);

        // Botones de acción
        Prop(so, "_btnVerBodega").objectReferenceValue    = AddButton(CreateChild(subInfo.transform, "BtnVerBodega",    40f), "Ver Bodega");
        Prop(so, "_btnFormarConvoy").objectReferenceValue = AddButton(CreateChild(subInfo.transform, "BtnFormarConvoy", 40f), "Formar Convoy");
        Prop(so, "_btnUnirseConvoy").objectReferenceValue = AddButton(CreateChild(subInfo.transform, "BtnUnirseConvoy", 40f), "Unirse a Convoy");

        // Fila modo pirata
        var filaPirata = CreateChild(subInfo.transform, "FilaModoPirata", 30f);
        AddHLG(filaPirata);
        var toggleGo = CreateFixedChild(filaPirata.transform, "ToggleModoPirata", 24f);
        Prop(so, "_toggleModoPirata").objectReferenceValue = toggleGo.AddComponent<Toggle>();
        AddTMP(CreateFlexChild(filaPirata.transform, "LblModoPirata"), "Modo Pirata", 11);

        // ── SubpanelConvoy ────────────────────────────────────────────────────
        var subConvoy = CreateChild(root.transform, "SubpanelConvoy", 200f);
        AddVLG(subConvoy, 6f);
        Prop(so, "_panelUnirseConvoy").objectReferenceValue = subConvoy;

        AddTMP(CreateChild(subConvoy.transform, "TxtTituloConvoyes", 25f), "Convoyes disponibles", 12);

        var contenedor = CreateChild(subConvoy.transform, "ContenedorListaConvoyes", 120f);
        AddVLG(contenedor, 4f);
        Prop(so, "_contenedorListaConvoyes").objectReferenceValue = contenedor.transform;

        Prop(so, "_btnVolverConvoy").objectReferenceValue =
            AddButton(CreateChild(subConvoy.transform, "BtnVolverConvoy", 40f), "◄ Volver");

        so.ApplyModifiedProperties();
        subConvoy.SetActive(false);
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
            UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
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

    /// <summary>TMP con texto, tamaño y color negro centrado.</summary>
    static TextMeshProUGUI AddTMP(GameObject go, string texto, int size = 14)
    {
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = size;
        tmp.color     = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    /// <summary>Button + Image gris + hijo Text con TMP.</summary>
    static Button AddButton(GameObject go, string label)
    {
        go.AddComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);
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
        tmp.color     = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    /// <summary>VerticalLayoutGroup con childControlWidth=true y childForceExpandHeight=false.</summary>
    static VerticalLayoutGroup AddVLG(GameObject go, float spacing = 5f)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
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
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
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
