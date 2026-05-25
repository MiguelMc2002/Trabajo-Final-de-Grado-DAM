#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script de editor que construye y cablea automáticamente el PanelInspeccionFlota
/// en la escena Mapamundi activa. Crea el panel, el prefab de fila con 7 columnas,
/// y cablea todos los campos de PanelInspeccionFlota y MapamundiController.
/// Menú: TFG → Construir Panel Inspección Flota
/// </summary>
public static class ConstruirPanelInspeccionFlotaEditor
{
    [MenuItem("TFG/Construir Panel Inspección Flota")]
    public static void Build()
    {
        Canvas canvas = BuscarCanvas();
        if (canvas == null) { Debug.LogError("[PanelInspeccionEditor] No se encontró Canvas en la escena."); return; }

        // ── Eliminar panel anterior si existe ─────────────────────────────────
        GameObject anterior = BuscarIncluyendoInactivos("PanelInspeccionFlota");
        if (anterior != null) Object.DestroyImmediate(anterior);

        // ── Panel raíz ────────────────────────────────────────────────────────
        GameObject panelGO = new GameObject("PanelInspeccionFlota");
        panelGO.transform.SetParent(canvas.transform, false);
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.08f, 0.05f, 0.95f);
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(900f, 400f);
        panelRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8f;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        PanelInspeccionFlota comp = panelGO.AddComponent<PanelInspeccionFlota>();

        // ── Fila título + botón cerrar ─────────────────────────────────────────
        GameObject filaHeader = new GameObject("FilaHeader");
        filaHeader.transform.SetParent(panelGO.transform, false);
        HorizontalLayoutGroup hlgHeader = filaHeader.AddComponent<HorizontalLayoutGroup>();
        hlgHeader.childControlHeight = true;
        hlgHeader.childForceExpandWidth = true;

        LayoutElement leHeader = filaHeader.AddComponent<LayoutElement>();
        leHeader.preferredHeight = 35f;

        // Título
        GameObject txtTituloGO = new GameObject("TxtTituloFlota");
        txtTituloGO.transform.SetParent(filaHeader.transform, false);
        TextMeshProUGUI txtTitulo = txtTituloGO.AddComponent<TextMeshProUGUI>();
        txtTitulo.text      = "Flota PNJ";
        txtTitulo.fontSize  = 18;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.color     = new Color(1f, 0.85f, 0.4f);
        txtTitulo.alignment = TextAlignmentOptions.MidlineLeft;

        LayoutElement leTitulo = txtTituloGO.AddComponent<LayoutElement>();
        leTitulo.flexibleWidth = 1f;

        // Botón cerrar
        GameObject btnCerrarGO = new GameObject("BtnCerrar");
        btnCerrarGO.transform.SetParent(filaHeader.transform, false);
        Image btnImg = btnCerrarGO.AddComponent<Image>();
        btnImg.color = new Color(0.6f, 0.1f, 0.1f, 1f);
        Button btnCerrar = btnCerrarGO.AddComponent<Button>();
        LayoutElement leBtnCerrar = btnCerrarGO.AddComponent<LayoutElement>();
        leBtnCerrar.preferredWidth  = 60f;
        leBtnCerrar.flexibleWidth   = 0f;

        GameObject txtCerrarGO = new GameObject("Text");
        txtCerrarGO.transform.SetParent(btnCerrarGO.transform, false);
        TextMeshProUGUI txtCerrar = txtCerrarGO.AddComponent<TextMeshProUGUI>();
        txtCerrar.text      = "X";
        txtCerrar.fontSize  = 16;
        txtCerrar.fontStyle = FontStyles.Bold;
        txtCerrar.color     = Color.white;
        txtCerrar.alignment = TextAlignmentOptions.Center;

        RectTransform txtCerrarRT = txtCerrarGO.GetComponent<RectTransform>();
        txtCerrarRT.anchorMin = Vector2.zero;
        txtCerrarRT.anchorMax = Vector2.one;
        txtCerrarRT.offsetMin = Vector2.zero;
        txtCerrarRT.offsetMax = Vector2.zero;

        // ── Fila cabeceras de columnas ─────────────────────────────────────────
        string[] cabeceras = { "Nombre", "Casco", "Vida", "Velocidad", "Maniobra", "Carga", "Fuerza" };
        GameObject filaCabecera = CrearFilaTextos(panelGO.transform, "FilaCabeceras", cabeceras, 14, new Color(1f, 0.85f, 0.4f), 25f, FontStyles.Bold);

        // ── Contenedor de filas (directo bajo el panel, sin ScrollRect ni Viewport) ──
        GameObject contenedor = new GameObject("ContenedorFilas");
        contenedor.transform.SetParent(panelGO.transform, false);
        RectTransform contRT = contenedor.AddComponent<RectTransform>();
        contRT.anchorMin = Vector2.zero;
        contRT.anchorMax = Vector2.one;
        contRT.offsetMin = Vector2.zero;
        contRT.offsetMax = Vector2.zero;

        LayoutElement leContenedor = contenedor.AddComponent<LayoutElement>();
        leContenedor.flexibleHeight = 1f;

        VerticalLayoutGroup vlgCont = contenedor.AddComponent<VerticalLayoutGroup>();
        vlgCont.childControlWidth     = true;
        vlgCont.childControlHeight    = true;
        vlgCont.childForceExpandWidth = true;
        vlgCont.spacing = 2f;

        ContentSizeFitter csf = contenedor.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Prefab de fila ────────────────────────────────────────────────────
        string prefabPath = "Assets/Prefabs/UI/FilaBarcoInspeccion.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        GameObject filaTemplate = CrearFilaTextos(null, "FilaBarcoInspeccion",
            new string[]{ "Barco_X", "Cog", "100/100", "3", "2", "200", "0" },
            12, Color.white, 28f, FontStyles.Normal);

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(filaTemplate, prefabPath);
        Object.DestroyImmediate(filaTemplate);
        Debug.Log($"[PanelInspeccionEditor] Prefab guardado en {prefabPath}");

        // ── Cablear PanelInspeccionFlota ──────────────────────────────────────
        SerializedObject so = new SerializedObject(comp);
        so.FindProperty("txtTituloFlota").objectReferenceValue   = txtTitulo;
        so.FindProperty("contenedorFilas").objectReferenceValue  = contenedor.transform;
        so.FindProperty("prefabFila").objectReferenceValue       = prefabAsset;
        so.FindProperty("btnCerrar").objectReferenceValue        = btnCerrar;
        so.ApplyModifiedProperties();

        // ── Cablear MapamundiController ───────────────────────────────────────
        MapamundiController mc = Object.FindAnyObjectByType<MapamundiController>();
        if (mc != null)
        {
            SerializedObject soMC = new SerializedObject(mc);
            SerializedProperty prop = soMC.FindProperty("panelInspeccionFlota");
            if (prop != null)
            {
                prop.objectReferenceValue = comp;
                soMC.ApplyModifiedProperties();
                Debug.Log("[PanelInspeccionEditor] MapamundiController.panelInspeccionFlota cableado.");
            }
            else
                Debug.LogWarning("[PanelInspeccionEditor] Campo panelInspeccionFlota no encontrado en MapamundiController.");
        }

        panelGO.SetActive(false);
        EditorSceneManager.MarkSceneDirty(panelGO.scene);
        Debug.Log("[PanelInspeccionEditor] PanelInspeccionFlota construido y cableado. Ejecutar desde escena Mapamundi.");
    }

    private static GameObject CrearFilaTextos(Transform padre, string nombre, string[] textos, int fontSize, Color color, float altura, FontStyles style)
    {
        GameObject fila = new GameObject(nombre);
        if (padre != null) fila.transform.SetParent(padre, false);
        HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.spacing = 4f;

        LayoutElement le = fila.AddComponent<LayoutElement>();
        le.preferredHeight = altura;

        foreach (string texto in textos)
        {
            GameObject col = new GameObject(texto);
            col.transform.SetParent(fila.transform, false);
            TextMeshProUGUI tmp = col.AddComponent<TextMeshProUGUI>();
            tmp.text      = texto;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
        }
        return fila;
    }

    private static Canvas BuscarCanvas()
    {
        foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c.gameObject.scene.isLoaded) return c;
        return null;
    }

    private static GameObject BuscarIncluyendoInactivos(string nombre)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.scene.isLoaded && go.name == nombre) return go;
        return null;
    }
}
#endif
