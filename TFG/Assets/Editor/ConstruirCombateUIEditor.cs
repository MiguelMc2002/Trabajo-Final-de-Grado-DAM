#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script de editor desechable. Genera la jerarquía completa de UI para los paneles
/// de combate naval (EncuentroNavalUI y ResultadoCombateUI) en la escena activa y
/// cablea todos los campos [SerializeField] de ambos componentes.
/// Menú: TFG → Construir UIs Combate Mapamundi.
/// </summary>
public static class ConstruirCombateUIEditor
{
    // =========================================================================
    //  MENÚ
    // =========================================================================

    [MenuItem("TFG/Construir UIs Combate Mapamundi")]
    static void BuildCombateUI()
    {
        // ── Canvas principal ──────────────────────────────────────────────────
        GameObject canvas = BuscarIncluyendoInactivos("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[CombateUIBuilder] No se encontró 'Canvas' en la escena.");
            return;
        }

        // ── Controlador raíz de la UI de combate ──────────────────────────────
        GameObject controller = BuscarEnHijos(canvas.transform, "CombateUIController");
        if (controller == null)
        {
            controller = new GameObject("CombateUIController", typeof(RectTransform));
            controller.transform.SetParent(canvas.transform, false);
            var rt        = controller.GetComponent<RectTransform>();
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
        }

        // ── 2A: Panel EncuentroNaval ──────────────────────────────────────────
        GameObject panelEncuentro = ConstruirPanelEncuentro(canvas);

        // ── 2B: Panel ResultadoCombate ────────────────────────────────────────
        GameObject panelResultado = ConstruirPanelResultado(canvas);

        // ── Componentes MonoBehaviour ─────────────────────────────────────────
        EncuentroNavalUI  encuentroUI  = controller.GetComponent<EncuentroNavalUI>()  ?? controller.AddComponent<EncuentroNavalUI>();
        ResultadoCombateUI resultadoUI = controller.GetComponent<ResultadoCombateUI>() ?? controller.AddComponent<ResultadoCombateUI>();

        // ── 2A: Cablear EncuentroNavalUI ──────────────────────────────────────
        var soEncuentro = new SerializedObject(encuentroUI);
        Prop(soEncuentro, "_panelEncuentro").objectReferenceValue   = panelEncuentro;
        Prop(soEncuentro, "_textoNarrativo").objectReferenceValue   = BuscarTMP(panelEncuentro, "TxtNarrativo");
        Prop(soEncuentro, "_btnLuchar").objectReferenceValue        = BuscarBoton(panelEncuentro, "BtnLuchar");
        Prop(soEncuentro, "_btnHuir").objectReferenceValue          = BuscarBoton(panelEncuentro, "BtnHuir");
        Prop(soEncuentro, "_resultadoCombateUI").objectReferenceValue = resultadoUI;
        soEncuentro.ApplyModifiedProperties();

        // ── 2B: Cablear ResultadoCombateUI ────────────────────────────────────
        var soResultado = new SerializedObject(resultadoUI);
        Prop(soResultado, "_panelResultado").objectReferenceValue = panelResultado;
        Prop(soResultado, "_txtTitulo").objectReferenceValue      = BuscarTMP(panelResultado, "TxtTitulo");
        Prop(soResultado, "_txtNarrativo").objectReferenceValue   = BuscarTMP(panelResultado, "TxtNarrativo");
        Prop(soResultado, "_txtBotin").objectReferenceValue       = BuscarTMP(panelResultado, "TxtBotin");
        Prop(soResultado, "_txtBajas").objectReferenceValue       = BuscarTMP(panelResultado, "TxtBajas");
        Prop(soResultado, "_btnContinuar").objectReferenceValue   = BuscarBoton(panelResultado, "BtnContinuar");
        soResultado.ApplyModifiedProperties();

        // ── 2D: Posiciones válidas para piratas (fallback hardcodeado) ─────────
        AsignarPosicionesPiratas();

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("[CombateUIBuilder] ✓ UIs de combate construidas. Guarda la escena (Ctrl+S).");
    }

    // =========================================================================
    //  2A — Construir PanelEncuentroNaval
    // =========================================================================

    static GameObject ConstruirPanelEncuentro(GameObject canvas)
    {
        GameObject panel = BuscarEnHijos(canvas.transform, "PanelEncuentroNaval");
        if (panel != null)
            LimpiarHijos(panel.transform);
        else
        {
            panel = new GameObject("PanelEncuentroNaval", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
        }

        // Imagen de fondo marrón oscuro medieval
        var img   = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        img.color = new Color(0.15f, 0.10f, 0.05f, 0.92f);

        // Anclaje central
        var rt        = panel.GetComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0.2f, 0.3f);
        rt.anchorMax  = new Vector2(0.8f, 0.7f);
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        AddVLG(panel, 10f, 20);

        // Texto narrativo
        var goTxt = CreateChild(panel.transform, "TxtNarrativo", 60f);
        var tmp   = goTxt.AddComponent<TextMeshProUGUI>();
        tmp.text                = "Una flota pirata os intercepta en alta mar.";
        tmp.fontSize            = 16;
        tmp.color               = Color.white;
        tmp.alignment           = TextAlignmentOptions.Center;
        tmp.enableWordWrapping  = true;

        // Fila de botones
        var filaBotones = CreateChild(panel.transform, "FilaBotones", 55f);
        var hlg = filaBotones.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing                = 15f;

        Color colorBotonDorado = new Color(0.6f, 0.4f, 0.1f, 1f);
        CrearBotonCombate(filaBotones.transform, "BtnLuchar", "⚔ LUCHAR", colorBotonDorado);
        CrearBotonCombate(filaBotones.transform, "BtnHuir",   "🏃 HUIR",   colorBotonDorado);

        panel.SetActive(false);
        return panel;
    }

    // =========================================================================
    //  2B — Construir PanelResultadoCombate
    // =========================================================================

    static GameObject ConstruirPanelResultado(GameObject canvas)
    {
        GameObject panel = BuscarEnHijos(canvas.transform, "PanelResultadoCombate");
        if (panel != null)
            LimpiarHijos(panel.transform);
        else
        {
            panel = new GameObject("PanelResultadoCombate", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
        }

        // Imagen de fondo casi negro medieval
        var img   = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.08f, 0.04f, 0.95f);

        // Anclaje central amplio
        var rt        = panel.GetComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0.15f, 0.15f);
        rt.anchorMax  = new Vector2(0.85f, 0.85f);
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        AddVLG(panel, 12f, 25);

        // TxtTitulo — dorado, grande, bold
        var goTitulo = CreateChild(panel.transform, "TxtTitulo", 40f);
        var tmpTitulo = goTitulo.AddComponent<TextMeshProUGUI>();
        tmpTitulo.text      = "VICTORIA";
        tmpTitulo.fontSize  = 24;
        tmpTitulo.color     = new Color(1f, 0.84f, 0.2f);
        tmpTitulo.fontStyle = FontStyles.Bold;
        tmpTitulo.alignment = TextAlignmentOptions.Center;

        // TxtNarrativo — blanco, wrapping
        var goNarr = CreateChild(panel.transform, "TxtNarrativo", 60f);
        var tmpNarr = goNarr.AddComponent<TextMeshProUGUI>();
        tmpNarr.text               = "El texto narrativo del combate aparecerá aquí.";
        tmpNarr.fontSize           = 14;
        tmpNarr.color              = Color.white;
        tmpNarr.enableWordWrapping = true;
        tmpNarr.alignment          = TextAlignmentOptions.Center;

        // TxtBotin — amarillo tenue, con altura fija
        var goBotin = CreateChild(panel.transform, "TxtBotin", 80f);
        var leBotin = goBotin.GetComponent<LayoutElement>() ?? goBotin.AddComponent<LayoutElement>();
        leBotin.preferredHeight = 80f;
        var tmpBotin = goBotin.AddComponent<TextMeshProUGUI>();
        tmpBotin.text               = "Botín: 0 oro";
        tmpBotin.fontSize           = 13;
        tmpBotin.color              = new Color(0.9f, 0.85f, 0.4f);
        tmpBotin.enableWordWrapping = true;

        // TxtBajas — rojizo, con altura fija
        var goBajas = CreateChild(panel.transform, "TxtBajas", 60f);
        var leBajas = goBajas.GetComponent<LayoutElement>() ?? goBajas.AddComponent<LayoutElement>();
        leBajas.preferredHeight = 60f;
        var tmpBajas = goBajas.AddComponent<TextMeshProUGUI>();
        tmpBajas.text               = "Bajas: —";
        tmpBajas.fontSize           = 13;
        tmpBajas.color              = new Color(0.9f, 0.5f, 0.5f);
        tmpBajas.enableWordWrapping = true;

        // BtnContinuar — verde medieval, ancho completo
        var goContinuar = CreateChild(panel.transform, "BtnContinuar", 50f);
        goContinuar.AddComponent<Image>().color = new Color(0.4f, 0.6f, 0.2f, 1f);
        goContinuar.AddComponent<Button>();
        var goTexto = new GameObject("Text", typeof(RectTransform));
        goTexto.transform.SetParent(goContinuar.transform, false);
        var rtTexto      = goTexto.GetComponent<RectTransform>();
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = Vector2.zero;
        rtTexto.offsetMax = Vector2.zero;
        var tmpCont       = goTexto.AddComponent<TextMeshProUGUI>();
        tmpCont.text      = "CONTINUAR →";
        tmpCont.fontSize  = 16;
        tmpCont.color     = Color.white;
        tmpCont.alignment = TextAlignmentOptions.Center;

        panel.SetActive(false);
        return panel;
    }

    // =========================================================================
    //  2D — Posiciones piratas
    // =========================================================================

    static void AsignarPosicionesPiratas()
    {
        // BuscarCasillaMarValida es privado en MapamundiController, por lo que
        // se usan las posiciones de fallback especificadas en el diseño.
        var flotaManager = Object.FindFirstObjectByType<FlotaManager>();
        if (flotaManager == null)
        {
            Debug.LogWarning("[CombateUIBuilder] FlotaManager no encontrado en escena — se omite reubicación de piratas.");
            return;
        }

        // Verificar si el tilemap/RutaCalculador está disponible para fallback dinámico
        var rutaCalc = Object.FindFirstObjectByType<RutaCalculadorTilemap>();
        bool tilemapAccesible = false;
        if (rutaCalc != null)
        {
            var vecinos = rutaCalc.GetVecinosNavegables(new Vector3Int(5, 5, 0));
            tilemapAccesible = vecinos != null && vecinos.Count > 0;
        }

        if (!tilemapAccesible)
            Debug.LogWarning("[CombateUIBuilder] Tilemap no accesible o sin vecinos navegables — usando posiciones hardcodeadas para piratas.");

        // Posiciones hardcodeadas de fallback: (3,8), (7,4), (5,2)
        Vector2[] posiciones = { new Vector2(3, 8), new Vector2(7, 4), new Vector2(5, 2) };
        int[] idsPiratas = { 2001, 2002, 2003 };

        for (int i = 0; i < idsPiratas.Length; i++)
        {
            FlotaRuntimeData flota = flotaManager.ObtenerFlota(idsPiratas[i]);
            if (flota != null)
            {
                flota.PosicionActual = posiciones[i];
                Debug.Log($"[CombateUIBuilder] Pirata {idsPiratas[i]} reubicado en {posiciones[i]}.");
            }
        }
    }

    // =========================================================================
    //  HELPERS — construcción específica de combate
    // =========================================================================

    static void CrearBotonCombate(Transform parent, string nombre, string label, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var le              = go.AddComponent<LayoutElement>();
        le.preferredWidth   = 140f;
        le.preferredHeight  = 45f;

        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var rt        = textGo.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        var tmp       = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
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

    /// <summary>Busca un hijo directo o descendiente del transform dado por nombre.</summary>
    static GameObject BuscarEnHijos(Transform parent, string nombre)
    {
        Transform found = parent.Find(nombre);
        return found != null ? found.gameObject : null;
    }

    /// <summary>Busca un TextMeshProUGUI por nombre de GameObject hijo del panel.</summary>
    static TextMeshProUGUI BuscarTMP(GameObject panel, string nombre)
    {
        Transform t = panel.transform.Find(nombre);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    /// <summary>Busca un Button por nombre de GameObject hijo del panel (busca en hijos directos y un nivel más).</summary>
    static Button BuscarBoton(GameObject panel, string nombre)
    {
        // Búsqueda directa
        Transform t = panel.transform.Find(nombre);
        if (t != null) return t.GetComponent<Button>();

        // Búsqueda en hijos de segundo nivel (FilaBotones)
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            Transform hijo = panel.transform.GetChild(i);
            Transform nieto = hijo.Find(nombre);
            if (nieto != null) return nieto.GetComponent<Button>();
        }
        return null;
    }

    // =========================================================================
    //  HELPERS — construcción genérica
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

    /// <summary>VerticalLayoutGroup con padding configurable y childForceExpandHeight=false.</summary>
    static VerticalLayoutGroup AddVLG(GameObject go, float spacing = 5f, int padding = 8)
    {
        var vlg = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing                = spacing;
        vlg.padding                = new RectOffset(padding, padding, padding, padding);
        return vlg;
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
            Debug.LogWarning($"[CombateUIBuilder] Campo no encontrado en el componente: '{fieldName}'");
        return prop;
    }
}
#endif
