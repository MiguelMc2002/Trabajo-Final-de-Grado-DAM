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
/// También crea el singleton GestorCombatesActivos en la escena si no existe.
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
            var rt       = controller.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── 2A: Panel EncuentroNaval ──────────────────────────────────────────
        GameObject panelEncuentro = ConstruirPanelEncuentro(canvas);

        // ── 2B: Panel ResultadoCombate ────────────────────────────────────────
        GameObject panelResultado = ConstruirPanelResultado(canvas);

        // ── Componentes MonoBehaviour ─────────────────────────────────────────
        EncuentroNavalUI   encuentroUI  = controller.GetComponent<EncuentroNavalUI>()   ?? controller.AddComponent<EncuentroNavalUI>();
        ResultadoCombateUI resultadoUI  = controller.GetComponent<ResultadoCombateUI>() ?? controller.AddComponent<ResultadoCombateUI>();

        // ── 2A: Cablear EncuentroNavalUI ──────────────────────────────────────
        // Nota: _resultadoCombateUI fue eliminado del script; ya no se cablea aquí.
        var soEncuentro = new SerializedObject(encuentroUI);
        SetProp(soEncuentro, "_panelEncuentro", panelEncuentro);
        SetProp(soEncuentro, "_textoNarrativo", BuscarTMP(panelEncuentro, "TxtNarrativo"));
        SetProp(soEncuentro, "_btnLuchar",      BuscarBotonRecursivo(panelEncuentro, "BtnLuchar"));
        SetProp(soEncuentro, "_btnHuir",        BuscarBotonRecursivo(panelEncuentro, "BtnHuir"));
        soEncuentro.ApplyModifiedProperties();

        // ── 2B: Cablear ResultadoCombateUI ────────────────────────────────────
        // Panel Acciones Victoria y sus botones
        GameObject panelAcciones = BuscarEnHijos(panelResultado.transform, "PanelAccionesVictoria");
        GameObject panelSaqueo   = BuscarEnHijos(panelResultado.transform, "SubpanelSaqueo");
        GameObject panelCaptura  = BuscarEnHijos(panelResultado.transform, "SubpanelCaptura");

        var soResultado = new SerializedObject(resultadoUI);

        // Panel principal
        SetProp(soResultado, "_panelResultado", panelResultado);
        SetProp(soResultado, "_txtTitulo",      BuscarTMP(panelResultado, "TxtTitulo"));
        SetProp(soResultado, "_txtNarrativo",   BuscarTMP(panelResultado, "TxtNarrativo"));
        SetProp(soResultado, "_txtBajas",       BuscarTMP(panelResultado, "TxtBajas"));
        SetProp(soResultado, "_btnContinuar",   BuscarBotonRecursivo(panelResultado, "BtnContinuar"));

        // Botones post-victoria
        SetProp(soResultado, "_panelAccionesVictoria", panelAcciones);
        SetProp(soResultado, "_btnDestruir",   BuscarBotonRecursivo(panelResultado, "BtnDestruir"));
        SetProp(soResultado, "_btnSaquear",    BuscarBotonRecursivo(panelResultado, "BtnSaquear"));
        SetProp(soResultado, "_btnCapturar",   BuscarBotonRecursivo(panelResultado, "BtnCapturar"));

        // Sub-panel saqueo
        SetProp(soResultado, "_panelSaqueo",       panelSaqueo);
        SetProp(soResultado, "_txtBotinSaqueo",     BuscarTMPRecursivo(panelSaqueo,  "TxtBotinSaqueo"));
        SetProp(soResultado, "_btnConfirmarSaqueo", BuscarBotonRecursivo(panelSaqueo, "BtnConfirmarSaqueo"));

        // Sub-panel captura
        SetProp(soResultado, "_panelCaptura",       panelCaptura);
        SetProp(soResultado, "_txtInfoCaptura",     BuscarTMPRecursivo(panelCaptura,  "TxtInfoCaptura"));
        SetProp(soResultado, "_btnConfirmarCaptura", BuscarBotonRecursivo(panelCaptura, "BtnConfirmarCaptura"));

        soResultado.ApplyModifiedProperties();

        // ── 2D: Posiciones válidas para piratas (fallback hardcodeado) ─────────
        AsignarPosicionesPiratas();

        // ── 2E: GestorCombatesActivos en escena ───────────────────────────────
        AsegurarGestorCombatesActivos();

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

        // Fondo marrón oscuro medieval
        var img   = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        img.color = new Color(0.15f, 0.10f, 0.05f, 0.92f);

        var rt       = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.3f);
        rt.anchorMax = new Vector2(0.8f, 0.7f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        AddVLG(panel, 10f, 20);

        // Texto narrativo
        var goTxt = CreateChild(panel.transform, "TxtNarrativo", 60f);
        var tmp   = goTxt.AddComponent<TextMeshProUGUI>();
        tmp.text               = "Una flota pirata os intercepta en alta mar.";
        tmp.fontSize           = 16;
        tmp.color              = Color.white;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;

        // Fila de botones
        var filaBotones = CreateChild(panel.transform, "FilaBotones", 55f);
        var hlg = filaBotones.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing               = 15f;

        Color colorDorado = new Color(0.6f, 0.4f, 0.1f, 1f);
        CrearBoton(filaBotones.transform, "BtnLuchar", "⚔ LUCHAR", colorDorado, 140f, 45f, 14);
        CrearBoton(filaBotones.transform, "BtnHuir",   "🏃 HUIR",  colorDorado, 140f, 45f, 14);

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

        // Fondo casi negro medieval
        var img   = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.08f, 0.04f, 0.95f);

        var rt       = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, 0.10f);
        rt.anchorMax = new Vector2(0.85f, 0.90f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

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
        var goNarr  = CreateChild(panel.transform, "TxtNarrativo", 60f);
        var tmpNarr = goNarr.AddComponent<TextMeshProUGUI>();
        tmpNarr.text               = "El texto narrativo del combate aparecerá aquí.";
        tmpNarr.fontSize           = 14;
        tmpNarr.color              = Color.white;
        tmpNarr.enableWordWrapping = true;
        tmpNarr.alignment          = TextAlignmentOptions.Center;

        // TxtBajas — rojizo
        var goBajas = CreateChild(panel.transform, "TxtBajas", 60f);
        var leBajas = goBajas.GetComponent<LayoutElement>() ?? goBajas.AddComponent<LayoutElement>();
        leBajas.preferredHeight = 60f;
        var tmpBajas = goBajas.AddComponent<TextMeshProUGUI>();
        tmpBajas.text               = "Bajas: —";
        tmpBajas.fontSize           = 13;
        tmpBajas.color              = new Color(0.9f, 0.5f, 0.5f);
        tmpBajas.enableWordWrapping = true;

        // BtnContinuar — verde medieval
        CrearBoton(panel.transform, "BtnContinuar", "CONTINUAR →",
                   new Color(0.4f, 0.6f, 0.2f, 1f), 0f, 50f, 16, anchoFlexible: true);

        // ── PanelAccionesVictoria ─────────────────────────────────────────────
        ConstruirPanelAccionesVictoria(panel.transform);

        // ── SubpanelSaqueo ────────────────────────────────────────────────────
        ConstruirSubpanelSaqueo(panel.transform);

        // ── SubpanelCaptura ───────────────────────────────────────────────────
        ConstruirSubpanelCaptura(panel.transform);

        panel.SetActive(false);
        return panel;
    }

    // ── PanelAccionesVictoria ─────────────────────────────────────────────────

    static void ConstruirPanelAccionesVictoria(Transform parent)
    {
        var panelGo = new GameObject("PanelAccionesVictoria", typeof(RectTransform));
        panelGo.transform.SetParent(parent, false);

        var img   = panelGo.AddComponent<Image>();
        img.color = new Color(0.12f, 0.09f, 0.05f, 0.0f); // transparente (solo layout)

        var le            = panelGo.GetComponent<LayoutElement>() ?? panelGo.AddComponent<LayoutElement>();
        le.preferredHeight = 110f;
        le.flexibleWidth   = 1f;

        AddVLG(panelGo, 8f, 0);

        // Texto cabecera
        var goHeader  = CreateChild(panelGo.transform, "TxtCabeceraAcciones", 22f);
        var tmpHeader = goHeader.AddComponent<TextMeshProUGUI>();
        tmpHeader.text      = "¿Qué hacéis con la flota enemiga?";
        tmpHeader.fontSize  = 13;
        tmpHeader.color     = Color.white;
        tmpHeader.alignment = TextAlignmentOptions.Center;

        // Fila con los tres botones
        var filaGo = CreateChild(panelGo.transform, "FilaBotonesVictoria", 55f);
        var hlg = filaGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing               = 10f;

        // Destruir — rojo
        CrearBoton(filaGo.transform, "BtnDestruir", "Destruir",
                   new Color(0.545f, 0.078f, 0.078f, 1f), 120f, 45f, 13);

        // Saquear — dorado oscuro
        Color doradoOscuro = new Color(0.545f, 0.412f, 0.078f, 1f);
        CrearBoton(filaGo.transform, "BtnSaquear",  "Saquear",  doradoOscuro, 120f, 45f, 13);

        // Capturar — dorado oscuro
        CrearBoton(filaGo.transform, "BtnCapturar", "Capturar", doradoOscuro, 120f, 45f, 13);

        panelGo.SetActive(false);
    }

    // ── SubpanelSaqueo ────────────────────────────────────────────────────────

    static void ConstruirSubpanelSaqueo(Transform parent)
    {
        var panelGo = new GameObject("SubpanelSaqueo", typeof(RectTransform));
        panelGo.transform.SetParent(parent, false);

        var img   = panelGo.AddComponent<Image>();
        img.color = new Color(0.102f, 0.055f, 0.031f, 0.95f); // #1A0E08

        var le             = panelGo.GetComponent<LayoutElement>() ?? panelGo.AddComponent<LayoutElement>();
        le.preferredHeight = 130f;
        le.flexibleWidth   = 1f;

        AddVLG(panelGo, 8f, 8);

        // Título
        var goTit  = CreateChild(panelGo.transform, "TxtTituloSaqueo", 22f);
        var tmpTit = goTit.AddComponent<TextMeshProUGUI>();
        tmpTit.text      = "Saqueo";
        tmpTit.fontSize  = 14;
        tmpTit.color     = new Color(1f, 0.84f, 0.2f); // dorado
        tmpTit.fontStyle = FontStyles.Bold;
        tmpTit.alignment = TextAlignmentOptions.Center;

        // Texto botín
        var goBotin = CreateChild(panelGo.transform, "TxtBotinSaqueo", 50f);
        var leBotin = goBotin.GetComponent<LayoutElement>() ?? goBotin.AddComponent<LayoutElement>();
        leBotin.preferredHeight = 50f;
        leBotin.flexibleHeight  = 1f;
        var tmpBotin = goBotin.AddComponent<TextMeshProUGUI>();
        tmpBotin.text               = "Contenido de la bodega enemiga...";
        tmpBotin.fontSize           = 11;
        tmpBotin.color              = Color.white;
        tmpBotin.enableWordWrapping = true;
        tmpBotin.alignment          = TextAlignmentOptions.TopLeft;

        // Botón confirmar saqueo
        Color doradoOscuro = new Color(0.545f, 0.412f, 0.078f, 1f);
        CrearBoton(panelGo.transform, "BtnConfirmarSaqueo", "Confirmar saqueo",
                   doradoOscuro, 0f, 38f, 13, anchoFlexible: true);

        panelGo.SetActive(false);
    }

    // ── SubpanelCaptura ───────────────────────────────────────────────────────

    static void ConstruirSubpanelCaptura(Transform parent)
    {
        var panelGo = new GameObject("SubpanelCaptura", typeof(RectTransform));
        panelGo.transform.SetParent(parent, false);

        var img   = panelGo.AddComponent<Image>();
        img.color = new Color(0.102f, 0.055f, 0.031f, 0.95f); // #1A0E08

        var le             = panelGo.GetComponent<LayoutElement>() ?? panelGo.AddComponent<LayoutElement>();
        le.preferredHeight = 130f;
        le.flexibleWidth   = 1f;

        AddVLG(panelGo, 8f, 8);

        // Título
        var goTit  = CreateChild(panelGo.transform, "TxtTituloCaptura", 22f);
        var tmpTit = goTit.AddComponent<TextMeshProUGUI>();
        tmpTit.text      = "Captura de barcos";
        tmpTit.fontSize  = 14;
        tmpTit.color     = new Color(1f, 0.84f, 0.2f);
        tmpTit.fontStyle = FontStyles.Bold;
        tmpTit.alignment = TextAlignmentOptions.Center;

        // Texto info captura
        var goInfo = CreateChild(panelGo.transform, "TxtInfoCaptura", 50f);
        var leInfo = goInfo.GetComponent<LayoutElement>() ?? goInfo.AddComponent<LayoutElement>();
        leInfo.preferredHeight = 50f;
        leInfo.flexibleHeight  = 1f;
        var tmpInfo = goInfo.AddComponent<TextMeshProUGUI>();
        tmpInfo.text               = "Información sobre la captura...";
        tmpInfo.fontSize           = 11;
        tmpInfo.color              = Color.white;
        tmpInfo.enableWordWrapping = true;
        tmpInfo.alignment          = TextAlignmentOptions.TopLeft;

        // Botón confirmar captura
        Color doradoOscuro = new Color(0.545f, 0.412f, 0.078f, 1f);
        CrearBoton(panelGo.transform, "BtnConfirmarCaptura", "Confirmar captura",
                   doradoOscuro, 0f, 38f, 13, anchoFlexible: true);

        panelGo.SetActive(false);
    }

    // =========================================================================
    //  2D — Posiciones piratas
    // =========================================================================

    static void AsignarPosicionesPiratas()
    {
        var flotaManager = Object.FindFirstObjectByType<FlotaManager>();
        if (flotaManager == null)
        {
            Debug.LogWarning("[CombateUIBuilder] FlotaManager no encontrado en escena — se omite reubicación de piratas.");
            return;
        }

        var rutaCalc = Object.FindFirstObjectByType<RutaCalculadorTilemap>();
        bool tilemapAccesible = false;
        if (rutaCalc != null)
        {
            var vecinos = rutaCalc.GetVecinosNavegables(new Vector3Int(5, 5, 0));
            tilemapAccesible = vecinos != null && vecinos.Count > 0;
        }

        if (!tilemapAccesible)
            Debug.LogWarning("[CombateUIBuilder] Tilemap no accesible — usando posiciones hardcodeadas para piratas.");

        Vector2[] posiciones = { new Vector2(3, 8), new Vector2(7, 4), new Vector2(5, 2) };
        int[]     idsPiratas = { 2001, 2002, 2003 };

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
    //  2E — GestorCombatesActivos en escena
    // =========================================================================

    static void AsegurarGestorCombatesActivos()
    {
        // Buscar GameObject existente por nombre (incluye inactivos)
        GameObject go = BuscarIncluyendoInactivos("GestorCombatesActivos");
        if (go == null)
        {
            go = new GameObject("GestorCombatesActivos");
            Debug.Log("[CombateUIBuilder] GestorCombatesActivos creado en la escena.");
        }

        // Añadir componente si falta
        if (go.GetComponent<GestorCombatesActivos>() == null)
        {
            go.AddComponent<GestorCombatesActivos>();
            Debug.Log("[CombateUIBuilder] Componente GestorCombatesActivos añadido.");
        }
        else
        {
            Debug.Log("[CombateUIBuilder] GestorCombatesActivos ya estaba presente en la escena.");
        }
    }

    // =========================================================================
    //  HELPERS — construcción de botones
    // =========================================================================

    /// <summary>
    /// Crea un botón con imagen de fondo y texto TMP.
    /// Si <paramref name="anchoFlexible"/> es true, el LayoutElement usa flexibleWidth=1 en lugar de preferredWidth fija.
    /// </summary>
    static void CrearBoton(Transform parent, string nombre, string label, Color color,
                           float ancho, float alto, int fontSize, bool anchoFlexible = false)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var le             = go.AddComponent<LayoutElement>();
        le.preferredHeight = alto;
        if (anchoFlexible)
            le.flexibleWidth = 1f;
        else
            le.preferredWidth = ancho;

        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var rt       = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp       = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
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

    static GameObject BuscarEnHijos(Transform parent, string nombre)
    {
        Transform found = parent.Find(nombre);
        return found != null ? found.gameObject : null;
    }

    static TextMeshProUGUI BuscarTMP(GameObject panel, string nombre)
    {
        Transform t = panel.transform.Find(nombre);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    /// <summary>Busca un TMP recursivamente en todos los descendientes del panel dado.</summary>
    static TextMeshProUGUI BuscarTMPRecursivo(GameObject panel, string nombre)
    {
        if (panel == null) return null;
        return BuscarTMPRecursivo(panel.transform, nombre);
    }

    static TextMeshProUGUI BuscarTMPRecursivo(Transform t, string nombre)
    {
        if (t.name == nombre)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }
        for (int i = 0; i < t.childCount; i++)
        {
            var res = BuscarTMPRecursivo(t.GetChild(i), nombre);
            if (res != null) return res;
        }
        return null;
    }

    /// <summary>Busca un Button recursivamente en todos los descendientes del panel dado.</summary>
    static Button BuscarBotonRecursivo(GameObject panel, string nombre)
    {
        if (panel == null) return null;
        return BuscarBotonRecursivo(panel.transform, nombre);
    }

    static Button BuscarBotonRecursivo(Transform t, string nombre)
    {
        if (t.name == nombre)
        {
            var btn = t.GetComponent<Button>();
            if (btn != null) return btn;
        }
        for (int i = 0; i < t.childCount; i++)
        {
            var res = BuscarBotonRecursivo(t.GetChild(i), nombre);
            if (res != null) return res;
        }
        return null;
    }

    // =========================================================================
    //  HELPERS — construcción genérica
    // =========================================================================

    static void LimpiarHijos(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    static GameObject CreateChild(Transform parent, string nombre, float height = 30f)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le             = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;
        return go;
    }

    static VerticalLayoutGroup AddVLG(GameObject go, float spacing = 5f, int padding = 8)
    {
        var vlg = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing               = spacing;
        vlg.padding               = new RectOffset(padding, padding, padding, padding);
        return vlg;
    }

    // =========================================================================
    //  HELPERS — cableado
    // =========================================================================

    /// <summary>Asigna un Object a la SerializedProperty con nombre dado; avisa si no existe.</summary>
    static void SetProp(SerializedObject so, string fieldName, Object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[CombateUIBuilder] Campo no encontrado: '{fieldName}'");
            return;
        }
        prop.objectReferenceValue = value;
    }
}
#endif
