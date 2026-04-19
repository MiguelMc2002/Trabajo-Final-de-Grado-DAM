using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Herramienta de editor que regenera el prefab MarketRow con la estructura de columnas
/// del mercado al estilo Patrician III.
/// Accesible desde el menú TFG → Regenerar Prefab MarketRow.
/// </summary>
public static class MarketRowEditorSetup
{
    private const string RutaPrefab = "Assets/Prefabs/UI/MarketRow.prefab";

    [MenuItem("TFG/Regenerar Prefab MarketRow")]
    public static void RegenerarPrefab()
    {
        // Crear el GameObject raíz en escena para configurarlo antes de guardarlo como prefab
        GameObject raiz = new GameObject("MarketRow");

        // ─── Layout raíz ─────────────────────────────────────────────────────

        RectTransform rtRaiz = raiz.AddComponent<RectTransform>();
        rtRaiz.sizeDelta = new Vector2(860f, 40f);

        HorizontalLayoutGroup hlgRaiz = raiz.AddComponent<HorizontalLayoutGroup>();
        hlgRaiz.spacing              = 5f;
        hlgRaiz.childAlignment       = TextAnchor.MiddleLeft;
        hlgRaiz.childControlWidth    = true;
        hlgRaiz.childControlHeight   = true;
        hlgRaiz.childForceExpandWidth  = true;
        hlgRaiz.childForceExpandHeight = false;

        LayoutElement leRaiz = raiz.AddComponent<LayoutElement>();
        leRaiz.preferredHeight = 40f;
        leRaiz.minHeight       = 40f;

        // ─── Columnas de texto ────────────────────────────────────────────────

        TextMeshProUGUI textoNombre      = CrearTexto(raiz, "TextoNombre",      200f, TextAlignmentOptions.MidlineLeft);
        TextMeshProUGUI textoStockCiudad = CrearTexto(raiz, "TextoStockCiudad",  80f, TextAlignmentOptions.Midline);

        // ─── Grupo Comprar ────────────────────────────────────────────────────

        GameObject grupoComprar = CrearGrupoHorizontal(raiz, "GrupoComprar", 170f);
        Button btnComprar1   = CrearBoton(grupoComprar, "BtnComprar1",   "+1",   50f);
        Button btnComprar10  = CrearBoton(grupoComprar, "BtnComprar10",  "+10",  55f);
        Button btnComprar100 = CrearBoton(grupoComprar, "BtnComprar100", "+100", 60f);

        // ─── Precio ───────────────────────────────────────────────────────────

        TextMeshProUGUI textoPrecio = CrearTexto(raiz, "TextoPrecio", 100f, TextAlignmentOptions.Midline);

        // ─── Grupo Vender ─────────────────────────────────────────────────────

        GameObject grupoVender = CrearGrupoHorizontal(raiz, "GrupoVender", 170f);
        Button btnVender1   = CrearBoton(grupoVender, "BtnVender1",   "-1",   50f);
        Button btnVender10  = CrearBoton(grupoVender, "BtnVender10",  "-10",  55f);
        Button btnVender100 = CrearBoton(grupoVender, "BtnVender100", "-100", 60f);

        // ─── Stock almacén ────────────────────────────────────────────────────

        TextMeshProUGUI textoStockAlmacen = CrearTexto(raiz, "TextoStockAlmacen", 80f, TextAlignmentOptions.Midline);

        // ─── Componente MarketRowUI ───────────────────────────────────────────

        MarketRowUI marketRowUI = raiz.AddComponent<MarketRowUI>();

        // Asignar referencias mediante reflexión para acceder a los campos privados serializados
        AsignarCampo(marketRowUI, "_textoNombre",      textoNombre);
        AsignarCampo(marketRowUI, "_textoStockCiudad", textoStockCiudad);
        AsignarCampo(marketRowUI, "_textoStockAlmacen",textoStockAlmacen);
        AsignarCampo(marketRowUI, "_textoPrecio",      textoPrecio);
        AsignarCampo(marketRowUI, "_btnComprar1",      btnComprar1);
        AsignarCampo(marketRowUI, "_btnComprar10",     btnComprar10);
        AsignarCampo(marketRowUI, "_btnComprar100",    btnComprar100);
        AsignarCampo(marketRowUI, "_btnVender1",       btnVender1);
        AsignarCampo(marketRowUI, "_btnVender10",      btnVender10);
        AsignarCampo(marketRowUI, "_btnVender100",     btnVender100);

        // ─── Guardar prefab ───────────────────────────────────────────────────

        // Eliminar el prefab anterior si existe
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            AssetDatabase.DeleteAsset(RutaPrefab);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);

        AssetDatabase.Refresh();
        Debug.Log($"[MarketRowEditorSetup] Prefab regenerado correctamente en {RutaPrefab}");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un hijo con TextMeshProUGUI y LayoutElement de ancho fijo.
    /// </summary>
    private static TextMeshProUGUI CrearTexto(GameObject padre, string nombre, float ancho, TextAlignmentOptions alineacion)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = ancho;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 18;
        tmp.color     = Color.black;
        tmp.alignment = alineacion;
        tmp.text      = nombre; // placeholder visible en el editor

        return tmp;
    }

    /// <summary>
    /// Crea un contenedor hijo con HorizontalLayoutGroup y LayoutElement de ancho fijo.
    /// </summary>
    private static GameObject CrearGrupoHorizontal(GameObject padre, string nombre, float ancho)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform, false);
        go.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 5f;
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.childControlWidth    = true;
        hlg.childControlHeight   = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = ancho;

        return go;
    }

    /// <summary>
    /// Crea un hijo Button con imagen de fondo clickable, etiqueta TextMeshProUGUI y LayoutElement de ancho fijo.
    /// </summary>
    private static Button CrearBoton(GameObject padre, string nombre, string etiqueta, float ancho)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform, false);
        go.AddComponent<RectTransform>();

        // Image es el targetGraphic del Button — sin ella el botón no recibe clicks
        Image imagen = go.AddComponent<Image>();
        imagen.color = new Color(0.78f, 0.78f, 0.78f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = imagen;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = ancho;
        le.minWidth        = ancho;
        le.preferredHeight = 35f;

        // Texto hijo con stretch completo para centrarse sobre el fondo
        GameObject textoGo = new GameObject("Texto");
        textoGo.transform.SetParent(go.transform, false);

        TextMeshProUGUI tmp = textoGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = 16;
        tmp.color     = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rtTexto = textoGo.GetComponent<RectTransform>();
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = Vector2.zero;
        rtTexto.offsetMax = Vector2.zero;

        return btn;
    }

    /// <summary>
    /// Asigna un valor a un campo privado serializado mediante reflexión.
    /// </summary>
    private static void AsignarCampo(object objetivo, string nombreCampo, object valor)
    {
        var campo = objetivo.GetType().GetField(
            nombreCampo,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (campo == null)
        {
            Debug.LogWarning($"[MarketRowEditorSetup] Campo '{nombreCampo}' no encontrado en {objetivo.GetType().Name}.");
            return;
        }

        campo.SetValue(objetivo, valor);
    }
}
