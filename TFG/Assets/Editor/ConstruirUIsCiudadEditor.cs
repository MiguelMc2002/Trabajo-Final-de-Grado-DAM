#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script de editor desechable. Genera la jerarquía completa de UI para
/// PanelAstillero y PanelTaberna en la escena Ciudad y cablea todos los
/// campos [SerializeField] de AstilleroUI y TabernaUI.
/// Menú: TFG → Construir UI Astillero / TFG → Construir UI Taberna.
/// </summary>
public static class ConstruirUIsCiudadEditor
{
    // =========================================================================
    //  MENÚ: Astillero
    // =========================================================================

    [MenuItem("TFG/Construir UI Astillero")]
    static void BuildAstillero()
    {
        GameObject root = BuscarIncluyendoInactivos("PanelAstillero");
        if (root == null)
        {
            Debug.LogError("[UIBuilder] No se encontró 'PanelAstillero' en la escena.");
            return;
        }

        LimpiarHijos(root.transform);

        // VLG raíz
        AddVLG(root, 0f);

        AstilleroUI ui = root.GetComponent<AstilleroUI>() ?? root.AddComponent<AstilleroUI>();
        var so = new SerializedObject(ui);

        // _panelAstillero → el propio root
        Prop(so, "_panelAstillero").objectReferenceValue = root;

        // ── SubpanelMenu ──────────────────────────────────────────────────────
        var panelMenu = CreateChild(root.transform, "SubpanelMenu", 230f);
        AddVLG(panelMenu, 8f);
        Prop(so, "_panelMenu").objectReferenceValue = panelMenu;

        AddTMP(CreateChild(panelMenu.transform, "TxtTitulo", 40f), "ASTILLERO", 20);
        AddButton(CreateChild(panelMenu.transform, "BtnMenuConstruir", 50f), "CONSTRUIR");
        AddButton(CreateChild(panelMenu.transform, "BtnMenuModificar", 50f), "MODIFICAR");
        AddButton(CreateChild(panelMenu.transform, "BtnMenuReparar",   50f), "REPARAR");
        AddButton(CreateChild(panelMenu.transform, "BtnMenuVender",    50f), "VENDER");
        AddButton(CreateChild(panelMenu.transform, "BtnCerrarAstillero", 40f), "X");

        // ── SubpanelConstruir ─────────────────────────────────────────────────
        var panelCons = CreateChild(root.transform, "SubpanelConstruir", 400f);
        AddVLG(panelCons, 8f);
        Prop(so, "_panelConstruir").objectReferenceValue = panelCons;

        // Fila título
        var filaTituloCons = CreateChild(panelCons.transform, "FilaTitulo", 35f);
        AddHLG(filaTituloCons);
        AddTMP(CreateFlexChild(filaTituloCons.transform, "TxtTituloConstruir"), "CONSTRUIR BARCO", 15);
        AddButton(CreateFixedChild(filaTituloCons.transform, "BtnVolverConstruir", 80f), "Volver");

        // Selector de casco
        {
            var (izq, txt, der) = AddSelector(panelCons.transform, "FilaSelectorCasco", "Cog", 40f);
            Prop(so, "_btnCascoIzq").objectReferenceValue         = izq;
            Prop(so, "_textoCascoConstruir").objectReferenceValue = txt;
            Prop(so, "_btnCascoDer").objectReferenceValue         = der;
        }

        // Fila de columnas: Casco | Velas | Bodega | Armamento
        var filaColumnas = CreateChild(panelCons.transform, "FilaColumnas", 160f);
        AddHLG(filaColumnas, 6f);

        // Columna Casco
        var colCasco = CreateFlexChild(filaColumnas.transform, "ColumnaCasco");
        AddVLG(colCasco, 3f);
        AddTMP(CreateChild(colCasco.transform, "LblCasco",  20f), "CASCO", 12);
        Prop(so, "_statsVidaCasco").objectReferenceValue            = AddTMP(CreateChild(colCasco.transform, "TxtVida",  18f), "Vida: —");
        Prop(so, "_statsVelocidadCasco").objectReferenceValue       = AddTMP(CreateChild(colCasco.transform, "TxtVel",   18f), "Vel: —");
        Prop(so, "_statsManiobrabilidadCasco").objectReferenceValue = AddTMP(CreateChild(colCasco.transform, "TxtMan",   18f), "Man: —");
        Prop(so, "_statsCargaCasco").objectReferenceValue           = AddTMP(CreateChild(colCasco.transform, "TxtCarga", 18f), "Carga: —");
        Prop(so, "_statsSlotsDisponibles").objectReferenceValue     = AddTMP(CreateChild(colCasco.transform, "TxtSlots", 18f), "Slots: —");

        // Columna Velas
        var colVelas = CreateFlexChild(filaColumnas.transform, "ColumnaVelas");
        AddVLG(colVelas, 3f);
        AddTMP(CreateChild(colVelas.transform, "LblVelas", 20f), "VELAS", 12);
        {
            var (izq, txt, der) = AddSelector(colVelas.transform, "SelectorVelas", "—", 35f);
            Prop(so, "_btnVelasIzq").objectReferenceValue      = izq;
            Prop(so, "_textoModuloVelas").objectReferenceValue = txt;
            Prop(so, "_btnVelasDer").objectReferenceValue      = der;
        }
        Prop(so, "_statsVelas").objectReferenceValue = AddTMP(CreateChild(colVelas.transform, "TxtStatsVelas", 18f), "—");

        // Columna Bodega
        var colBodega = CreateFlexChild(filaColumnas.transform, "ColumnaBodega");
        AddVLG(colBodega, 3f);
        AddTMP(CreateChild(colBodega.transform, "LblBodega", 20f), "BODEGA", 12);
        {
            var (izq, txt, der) = AddSelector(colBodega.transform, "SelectorBodega", "—", 35f);
            Prop(so, "_btnBodegaIzq").objectReferenceValue      = izq;
            Prop(so, "_textoModuloBodega").objectReferenceValue = txt;
            Prop(so, "_btnBodegaDer").objectReferenceValue      = der;
        }
        Prop(so, "_statsBodega").objectReferenceValue = AddTMP(CreateChild(colBodega.transform, "TxtStatsBodega", 18f), "—");

        // Columna Armamento
        var colArm = CreateFlexChild(filaColumnas.transform, "ColumnaArmamento");
        AddVLG(colArm, 3f);
        AddTMP(CreateChild(colArm.transform, "LblArm", 20f), "ARMAMENTO", 12);
        {
            var (izq, txt, der) = AddSelector(colArm.transform, "SelectorArm", "—", 35f);
            Prop(so, "_btnArmamentoIzq").objectReferenceValue      = izq;
            Prop(so, "_textoModuloArmamento").objectReferenceValue = txt;
            Prop(so, "_btnArmamentoDer").objectReferenceValue      = der;
        }
        Prop(so, "_statsArmamento").objectReferenceValue = AddTMP(CreateChild(colArm.transform, "TxtStatsArm", 18f), "—");

        // Precio y botón Construir
        Prop(so, "_textoPrecioConstruir").objectReferenceValue = AddTMP(CreateChild(panelCons.transform, "TxtPrecio", 25f), "Precio: 0 oro");
        Prop(so, "_btnConstruir").objectReferenceValue         = AddButton(CreateChild(panelCons.transform, "BtnConstruir", 45f), "CONSTRUIR");

        // ── SubpanelModificar ─────────────────────────────────────────────────
        var panelMod = CreateChild(root.transform, "SubpanelModificar", 420f);
        AddVLG(panelMod, 6f);
        Prop(so, "_panelModificar").objectReferenceValue = panelMod;

        // Fila título
        var filaTituloMod = CreateChild(panelMod.transform, "FilaTitulo", 35f);
        AddHLG(filaTituloMod);
        AddTMP(CreateFlexChild(filaTituloMod.transform, "TxtTituloMod"), "MODIFICAR BARCO", 15);
        AddButton(CreateFixedChild(filaTituloMod.transform, "BtnVolverMod", 80f), "Volver");
        // _btnVolverModificar no existe en AstilleroUI — botón solo visual

        // Selector de barco
        {
            var (izq, txt, der) = AddSelector(panelMod.transform, "SelectorBarcoMod", "Barco", 40f);
            Prop(so, "_btnBarcoModIzq").objectReferenceValue      = izq;
            Prop(so, "_textoBarcoModificar").objectReferenceValue = txt;
            Prop(so, "_btnBarcoModDer").objectReferenceValue      = der;
        }

        // Selectores de módulos Armamento / Velas / Bodega
        {
            var (izq, txt, der) = AddSelector(panelMod.transform, "SelectorArmMod", "—", 35f);
            Prop(so, "_btnArmamentoModIzq").objectReferenceValue      = izq;
            Prop(so, "_textoModuloArmamentoMod").objectReferenceValue = txt;
            Prop(so, "_btnArmamentoModDer").objectReferenceValue      = der;
        }
        Prop(so, "_statsArmamentoMod").objectReferenceValue = AddTMP(CreateChild(panelMod.transform, "TxtStatsArmMod", 18f), "—");

        {
            var (izq, txt, der) = AddSelector(panelMod.transform, "SelectorVelMod", "—", 35f);
            Prop(so, "_btnVelasModIzq").objectReferenceValue      = izq;
            Prop(so, "_textoModuloVelasMod").objectReferenceValue = txt;
            Prop(so, "_btnVelasModDer").objectReferenceValue      = der;
        }
        Prop(so, "_statsVelasMod").objectReferenceValue = AddTMP(CreateChild(panelMod.transform, "TxtStatsVelMod", 18f), "—");

        {
            var (izq, txt, der) = AddSelector(panelMod.transform, "SelectorBodMod", "—", 35f);
            Prop(so, "_btnBodegaModIzq").objectReferenceValue      = izq;
            Prop(so, "_textoModuloBodegaMod").objectReferenceValue = txt;
            Prop(so, "_btnBodegaModDer").objectReferenceValue      = der;
        }
        Prop(so, "_statsBodegaMod").objectReferenceValue = AddTMP(CreateChild(panelMod.transform, "TxtStatsBodMod", 18f), "—");

        Prop(so, "_textoPrecioModificar").objectReferenceValue  = AddTMP(CreateChild(panelMod.transform, "TxtCosteNeto",  25f), "Coste: 0");
        Prop(so, "_btnAplicarModificacion").objectReferenceValue = AddButton(CreateChild(panelMod.transform, "BtnInstalar", 45f), "INSTALAR");

        // ── SubpanelReparar ───────────────────────────────────────────────────
        var panelRep = CreateChild(root.transform, "SubpanelReparar", 260f);
        AddVLG(panelRep, 6f);
        Prop(so, "_panelReparar").objectReferenceValue = panelRep;

        var filaTituloRep = CreateChild(panelRep.transform, "FilaTitulo", 35f);
        AddHLG(filaTituloRep);
        AddTMP(CreateFlexChild(filaTituloRep.transform, "TxtTituloRep"), "REPARAR BARCO", 15);
        AddButton(CreateFixedChild(filaTituloRep.transform, "BtnVolverRep", 80f), "Volver");
        // _btnVolverReparar no existe en AstilleroUI — botón solo visual

        {
            var (izq, txt, der) = AddSelector(panelRep.transform, "SelectorBarcoRep", "Barco", 40f);
            Prop(so, "_btnBarcoRepIzq").objectReferenceValue    = izq;
            Prop(so, "_textoBarcoReparar").objectReferenceValue = txt;
            Prop(so, "_btnBarcoRepDer").objectReferenceValue    = der;
        }
        Prop(so, "_textoVidaReparar").objectReferenceValue   = AddTMP(CreateChild(panelRep.transform, "TxtVida",  25f), "Vida: 0/0");
        Prop(so, "_textoPrecioReparar").objectReferenceValue = AddTMP(CreateChild(panelRep.transform, "TxtCoste", 25f), "Coste: 0");
        Prop(so, "_btnReparar").objectReferenceValue         = AddButton(CreateChild(panelRep.transform, "BtnReparar", 45f), "REPARAR");

        // ── SubpanelVender ────────────────────────────────────────────────────
        var panelVen = CreateChild(root.transform, "SubpanelVender", 220f);
        AddVLG(panelVen, 6f);
        Prop(so, "_panelVender").objectReferenceValue = panelVen;

        var filaTituloVen = CreateChild(panelVen.transform, "FilaTitulo", 35f);
        AddHLG(filaTituloVen);
        AddTMP(CreateFlexChild(filaTituloVen.transform, "TxtTituloVen"), "VENDER BARCO", 15);
        AddButton(CreateFixedChild(filaTituloVen.transform, "BtnVolverVen", 80f), "Volver");
        // _btnVolverVender no existe en AstilleroUI — botón solo visual

        {
            var (izq, txt, der) = AddSelector(panelVen.transform, "SelectorBarcoVen", "Barco", 40f);
            Prop(so, "_btnBarcoVenIzq").objectReferenceValue    = izq;
            Prop(so, "_textoBarcoVender").objectReferenceValue  = txt;
            Prop(so, "_btnBarcoVenDer").objectReferenceValue    = der;
        }
        Prop(so, "_textoValorVenta").objectReferenceValue = AddTMP(CreateChild(panelVen.transform, "TxtValorVenta", 25f), "Valor: 0 oro");
        Prop(so, "_btnVender").objectReferenceValue       = AddButton(CreateChild(panelVen.transform, "BtnVender", 45f), "VENDER");

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[UIBuilder] ✓ PanelAstillero construido y cableado. Guarda la escena (Ctrl+S).");
    }

    // =========================================================================
    //  MENÚ: Taberna
    // =========================================================================

    [MenuItem("TFG/Construir UI Taberna")]
    static void BuildTaberna()
    {
        GameObject root = BuscarIncluyendoInactivos("PanelTaberna");
        if (root == null)
        {
            Debug.LogError("[UIBuilder] No se encontró 'PanelTaberna' en la escena.");
            return;
        }

        LimpiarHijos(root.transform);
        AddVLG(root, 0f);

        TabernaUI ui = root.GetComponent<TabernaUI>() ?? root.AddComponent<TabernaUI>();
        var so = new SerializedObject(ui);

        // _panelTaberna → el propio root
        Prop(so, "_panelTaberna").objectReferenceValue = root;

        // ── SubpanelMenu ──────────────────────────────────────────────────────
        var panelMenu = CreateChild(root.transform, "SubpanelMenu", 190f);
        AddVLG(panelMenu, 8f);
        Prop(so, "_panelMenu").objectReferenceValue = panelMenu;

        AddTMP(CreateChild(panelMenu.transform, "TxtTituloTab",  40f), "TABERNA", 20);
        AddButton(CreateChild(panelMenu.transform, "BtnIrMarineros", 50f), "CONTRATAR MARINEROS");
        AddButton(CreateChild(panelMenu.transform, "BtnIrCapitan",   50f), "CONTRATAR CAPITÁN");
        AddButton(CreateChild(panelMenu.transform, "BtnCerrarTab",   40f), "CERRAR");

        // ── SubpanelMarineros ─────────────────────────────────────────────────
        var panelMar = CreateChild(root.transform, "SubpanelMarineros", 300f);
        AddVLG(panelMar, 6f);
        Prop(so, "_panelMarineros").objectReferenceValue = panelMar;

        // Fila título
        var filaTituloMar = CreateChild(panelMar.transform, "FilaTitulo", 35f);
        AddHLG(filaTituloMar);
        AddTMP(CreateFlexChild(filaTituloMar.transform, "TxtTituloMar"), "CONTRATAR MARINEROS", 15);
        Prop(so, "_btnVolverDesdeMarineros").objectReferenceValue =
            AddButton(CreateFixedChild(filaTituloMar.transform, "BtnVolverMar", 80f), "Volver");

        // Selector de barco
        {
            var (izq, txt, der) = AddSelector(panelMar.transform, "SelectorBarcoMar", "Barco", 40f);
            Prop(so, "_btnBarcoMarIzq").objectReferenceValue         = izq;
            Prop(so, "_textoBarcoMarineros").objectReferenceValue    = txt;
            Prop(so, "_btnBarcoMarDer").objectReferenceValue         = der;
        }

        Prop(so, "_textoTripulacion").objectReferenceValue          = AddTMP(CreateChild(panelMar.transform, "TxtTripulacion",   22f), "Tripulación: 0/0");
        Prop(so, "_textoMarinerosDisponibles").objectReferenceValue = AddTMP(CreateChild(panelMar.transform, "TxtDisponibles",   22f), "Disponibles: 0");

        // Fila cantidad: [−] [n] [+]
        var filaCantidad = CreateChild(panelMar.transform, "FilaCantidad", 40f);
        AddHLG(filaCantidad, 8f);
        Prop(so, "_btnCantidadMenos").objectReferenceValue = AddButton(CreateFixedChild(filaCantidad.transform, "BtnMenos", 40f), "-");
        Prop(so, "_textoCantidad").objectReferenceValue    = AddTMP(CreateFlexChild(filaCantidad.transform, "TxtCantidad"), "1");
        Prop(so, "_btnCantidadMas").objectReferenceValue   = AddButton(CreateFixedChild(filaCantidad.transform, "BtnMas",   40f), "+");

        Prop(so, "_textoCosteMar").objectReferenceValue         = AddTMP(CreateChild(panelMar.transform, "TxtCosteMar", 22f), "Coste: 5 oro");
        Prop(so, "_btnContratarMarineros").objectReferenceValue = AddButton(CreateChild(panelMar.transform, "BtnContratar", 45f), "CONTRATAR");

        // ── SubpanelCapitan ───────────────────────────────────────────────────
        var panelCap = CreateChild(root.transform, "SubpanelCapitan", 300f);
        AddVLG(panelCap, 6f);
        Prop(so, "_panelCapitan").objectReferenceValue = panelCap;

        // Fila título
        var filaTituloCap = CreateChild(panelCap.transform, "FilaTitulo", 35f);
        AddHLG(filaTituloCap);
        AddTMP(CreateFlexChild(filaTituloCap.transform, "TxtTituloCap"), "CONTRATAR CAPITÁN", 15);
        Prop(so, "_btnVolverDesdeCapitan").objectReferenceValue =
            AddButton(CreateFixedChild(filaTituloCap.transform, "BtnVolverCap", 80f), "Volver");

        // Selector de barco — nota: campo _btnBarcoCaplzq tiene 'l' minúscula
        {
            var (izq, txt, der) = AddSelector(panelCap.transform, "SelectorBarcoCap", "Barco", 40f);
            Prop(so, "_btnBarcoCaplzq").objectReferenceValue    = izq;
            Prop(so, "_textoBarcoCapitan").objectReferenceValue = txt;
            Prop(so, "_btnBarcoCapDer").objectReferenceValue    = der;
        }

        Prop(so, "_textoCapitanActual").objectReferenceValue = AddTMP(CreateChild(panelCap.transform, "TxtCapActual", 22f), "Sin capitán");

        // Contenedor de capitanes
        var contenedor = CreateChild(panelCap.transform, "ContenedorCapitanes", 200f);
        AddVLG(contenedor, 4f);
        Prop(so, "_contenedorListaCapitanes").objectReferenceValue = contenedor.transform;

        // _prefabFilaCapitan → null intencionalmente (se asignará al crear el prefab)

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[UIBuilder] ✓ PanelTaberna construido y cableado. Guarda la escena (Ctrl+S).");
    }

    // =========================================================================
    //  HELPERS — búsqueda
    // =========================================================================

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

    static void LimpiarHijos(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    // Hijo con LayoutElement de altura preferida y anchura flexible.
    static GameObject CreateChild(Transform parent, string nombre, float height = 30f)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;
        return go;
    }

    // Hijo con anchura flexible (para columnas dentro de un HLG).
    static GameObject CreateFlexChild(Transform parent, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1f;
        le.flexibleHeight = 1f;
        return go;
    }

    // Hijo con anchura fija (para botones laterales dentro de un HLG).
    static GameObject CreateFixedChild(Transform parent, string nombre, float width)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.flexibleWidth  = 0f;
        le.flexibleHeight = 1f;
        return go;
    }

    // TMP con texto, tamaño y color negro centrado.
    static TextMeshProUGUI AddTMP(GameObject go, string texto, int size = 14)
    {
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = size;
        tmp.color     = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    // Button + Image gris + hijo Text con TMP.
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

    // Selector ◄ [texto] ► dentro de un HLG.
    static (Button izq, TextMeshProUGUI txt, Button der) AddSelector(
        Transform parent, string nombre, string textoDefault, float height = 35f)
    {
        var go = CreateChild(parent, nombre, height);
        AddHLG(go, 4f);

        var goIzq = CreateChild(go.transform, "BtnIzq", height);
        goIzq.GetComponent<LayoutElement>().preferredWidth = 35f;
        goIzq.GetComponent<LayoutElement>().flexibleWidth  = 0f;
        var btnIzq = AddButton(goIzq, "◄");

        var goTxt = CreateFlexChild(go.transform, "Txt");
        var tmp   = AddTMP(goTxt, textoDefault);

        var goDer = CreateChild(go.transform, "BtnDer", height);
        goDer.GetComponent<LayoutElement>().preferredWidth = 35f;
        goDer.GetComponent<LayoutElement>().flexibleWidth  = 0f;
        var btnDer = AddButton(goDer, "►");

        return (btnIzq, tmp, btnDer);
    }

    static VerticalLayoutGroup AddVLG(GameObject go, float spacing = 5f)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        return vlg;
    }

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

    static SerializedProperty Prop(SerializedObject so, string fieldName)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
            Debug.LogWarning($"[UIBuilder] Campo no encontrado en el componente: '{fieldName}'");
        return prop;
    }
}
#endif
