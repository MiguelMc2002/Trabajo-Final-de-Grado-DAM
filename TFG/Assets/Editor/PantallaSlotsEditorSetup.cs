using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Herramienta de editor que genera automáticamente la jerarquía completa del panel
/// de guardado/carga (PantallaSlotsUI) en el Canvas activo de la escena.
/// Accesible desde TFG → Generar Panel de Slots (Pantalla Completa / Popup).
/// </summary>
public static class PantallaSlotsEditorSetup
{
    // ─── Colores ──────────────────────────────────────────────────────────────

    private static readonly Color ColorOverlay        = Hex("00000099");
    private static readonly Color ColorFondoPanel     = Hex("2C1A0EFF");
    private static readonly Color ColorFondoFila      = Hex("1A0F05FF");
    private static readonly Color ColorFondoConf      = Hex("1A0F05FF");
    private static readonly Color ColorDorado         = Hex("C8A84BFF");
    private static readonly Color ColorGris           = Hex("888888FF");
    private static readonly Color ColorBotonMarron    = Hex("8B4513FF");
    private static readonly Color ColorBotonAzul      = Hex("1B3A5AFF");
    private static readonly Color ColorBotonRojo      = Hex("5A1B1BFF");
    private static readonly Color ColorBotonVerde     = Hex("2D5A1BFF");

    // ─── Menú ─────────────────────────────────────────────────────────────────

    [MenuItem("TFG/Generar Panel de Slots (Pantalla Completa)")]
    public static void GenerarPantallaCompleta() => Generar(esPopup: false);

    [MenuItem("TFG/Generar Panel de Slots (Popup)")]
    public static void GenerarPopup() => Generar(esPopup: true);

    // ─── Construcción ─────────────────────────────────────────────────────────

    private static void Generar(bool esPopup)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PantallaSlotsEditorSetup] No se encontró Canvas en la escena.");
            return;
        }

        string sufijo = esPopup ? " (Popup)" : " (Pantalla Completa)";

        // ── 1. Raíz PanelSlots ────────────────────────────────────────────────

        GameObject panelSlots = CrearImagen("PanelSlots" + sufijo, canvas.transform, ColorOverlay);
        Estirar(panelSlots.GetComponent<RectTransform>());

        // ── 2. PanelContenedor ────────────────────────────────────────────────

        float anchoContenedor = esPopup ? 600f : 700f;
        float altoContenedor  = esPopup ? 460f : 520f;

        GameObject contenedor = CrearImagen("PanelContenedor", panelSlots.transform, ColorFondoPanel);
        CentrarFijo(contenedor.GetComponent<RectTransform>(), anchoContenedor, altoContenedor);

        // ── 3. Título ─────────────────────────────────────────────────────────

        GameObject tituloGO = CrearTexto("Titulo", contenedor.transform,
            "— Partidas Guardadas —", 22, ColorDorado, TextAlignmentOptions.Center);
        RectTransform rtTitulo = tituloGO.GetComponent<RectTransform>();
        rtTitulo.anchorMin        = new Vector2(0f, 1f);
        rtTitulo.anchorMax        = new Vector2(1f, 1f);
        rtTitulo.pivot            = new Vector2(0.5f, 1f);
        rtTitulo.sizeDelta        = new Vector2(0f, 48f);
        rtTitulo.anchoredPosition = new Vector2(0f, -12f);

        // ── 4. ContenedorSlots ────────────────────────────────────────────────

        GameObject contenedorSlots = new GameObject("ContenedorSlots", typeof(RectTransform));
        contenedorSlots.transform.SetParent(contenedor.transform, false);
        RectTransform rtCS = contenedorSlots.GetComponent<RectTransform>();
        rtCS.anchorMin = Vector2.zero;
        rtCS.anchorMax = Vector2.one;
        rtCS.offsetMin = new Vector2(16f, 16f);
        rtCS.offsetMax = new Vector2(-16f, -64f);

        VerticalLayoutGroup vlg = contenedorSlots.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 8f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;

        // ── 5. Cinco filas SlotUI ─────────────────────────────────────────────

        SlotUI[] slotUIs = new SlotUI[5];
        for (int i = 0; i < 5; i++)
            slotUIs[i] = CrearFilaSlot(contenedorSlots.transform, i + 1);

        // ── 6. Botón cerrar ───────────────────────────────────────────────────

        GameObject botonCerrarGO = CrearBoton("BotonCerrar", contenedor.transform, "✕", ColorBotonMarron, 36f, 36f, 11f);
        RectTransform rtBC = botonCerrarGO.GetComponent<RectTransform>();
        rtBC.anchorMin        = new Vector2(1f, 1f);
        rtBC.anchorMax        = new Vector2(1f, 1f);
        rtBC.pivot            = new Vector2(1f, 1f);
        rtBC.sizeDelta        = new Vector2(36f, 36f);
        rtBC.anchoredPosition = new Vector2(-8f, -8f);
        Button botonCerrar = botonCerrarGO.GetComponent<Button>();

        // ── 7. PanelConfirmacion ──────────────────────────────────────────────

        GameObject panelConf = new GameObject("PanelConfirmacion", typeof(RectTransform));
        panelConf.transform.SetParent(contenedor.transform, false);
        CentrarFijo(panelConf.GetComponent<RectTransform>(), 400f, 160f);

        GameObject fondoConf = CrearImagen("FondoConfirmacion", panelConf.transform, ColorFondoConf);
        Estirar(fondoConf.GetComponent<RectTransform>());

        GameObject textoConfGO = CrearTexto("TextoConfirmacion", panelConf.transform,
            "¿Estás seguro?", 16, Color.white, TextAlignmentOptions.Center);
        RectTransform rtTC = textoConfGO.GetComponent<RectTransform>();
        rtTC.anchorMin = new Vector2(0f, 0.5f);
        rtTC.anchorMax = new Vector2(1f, 1f);
        rtTC.offsetMin = new Vector2(12f, 0f);
        rtTC.offsetMax = new Vector2(-12f, -8f);
        TextMeshProUGUI textoConf = textoConfGO.GetComponent<TextMeshProUGUI>();

        GameObject botonesConfGO = new GameObject("BotonesConfirmacion", typeof(RectTransform));
        botonesConfGO.transform.SetParent(panelConf.transform, false);
        RectTransform rtBConf = botonesConfGO.GetComponent<RectTransform>();
        rtBConf.anchorMin = new Vector2(0f, 0f);
        rtBConf.anchorMax = new Vector2(1f, 0.5f);
        rtBConf.offsetMin = new Vector2(8f, 8f);
        rtBConf.offsetMax = new Vector2(-8f, 0f);
        HorizontalLayoutGroup hlgConf = botonesConfGO.AddComponent<HorizontalLayoutGroup>();
        hlgConf.spacing              = 16f;
        hlgConf.childAlignment       = TextAnchor.MiddleCenter;
        hlgConf.childForceExpandWidth  = false;
        hlgConf.childForceExpandHeight = false;
        hlgConf.childControlWidth      = false;
        hlgConf.childControlHeight     = false;

        GameObject botonSiGO  = CrearBoton("BotonSi",  botonesConfGO.transform, "Sí", ColorBotonVerde, 100f, 36f);
        GameObject botonNoGO  = CrearBoton("BotonNo",  botonesConfGO.transform, "No", ColorBotonRojo,  100f, 36f);
        AplicarLayoutElement(botonSiGO, 100f, 36f);
        AplicarLayoutElement(botonNoGO, 100f, 36f);
        Button botonSi = botonSiGO.GetComponent<Button>();
        Button botonNo = botonNoGO.GetComponent<Button>();

        panelConf.SetActive(false);

        // ── 8. PantallaSlotsUI: añadir y cablear campos privados ──────────────

        PantallaSlotsUI pantalla = panelSlots.AddComponent<PantallaSlotsUI>();
        AsignarCampo(pantalla, "_slots",             slotUIs);
        AsignarCampo(pantalla, "_botonCerrar",       botonCerrar);
        AsignarCampo(pantalla, "_panelConfirmacion", panelConf);
        AsignarCampo(pantalla, "_textoConfirmacion", textoConf);
        AsignarCampo(pantalla, "_botonConfirmarSi",  botonSi);
        AsignarCampo(pantalla, "_botonConfirmarNo",  botonNo);

        // ── 9. Desactivar panel y marcar escena como modificada ───────────────

        panelSlots.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[PantallaSlotsEditorSetup] Panel '{panelSlots.name}' generado correctamente. " +
                  "Recuerda asignar _pantallaSlotsUI en MenuPrincipalUI / MenuPausa y guardar la escena (Ctrl+S).");
    }

    // ─── Creación de filas SlotUI ─────────────────────────────────────────────

    private static SlotUI CrearFilaSlot(Transform padre, int numero)
    {
        GameObject fila = CrearImagen($"SlotUI_{numero}", padre, ColorFondoFila);
        AplicarLayoutElement(fila, -1f, 72f);

        // TextoNombre
        GameObject textoNombreGO = CrearTexto("TextoNombre", fila.transform,
            $"Partida {numero}", 14, ColorDorado, TextAlignmentOptions.Left);
        RectTransform rtN = textoNombreGO.GetComponent<RectTransform>();
        rtN.anchorMin = new Vector2(0f, 0.1f);
        rtN.anchorMax = new Vector2(0.38f, 0.9f);
        rtN.offsetMin = new Vector2(8f, 0f);
        rtN.offsetMax = Vector2.zero;

        // TextoFecha
        GameObject textoFechaGO = CrearTexto("TextoFecha", fila.transform,
            "—", 11, ColorGris, TextAlignmentOptions.Center);
        RectTransform rtF = textoFechaGO.GetComponent<RectTransform>();
        rtF.anchorMin = new Vector2(0.34f, 0.1f);
        rtF.anchorMax = new Vector2(0.62f, 0.9f);
        rtF.offsetMin = Vector2.zero;
        rtF.offsetMax = Vector2.zero;

        // TextoDias
        GameObject textoDiasGO = CrearTexto("TextoDias", fila.transform,
            "— días", 11, ColorGris, TextAlignmentOptions.Right);
        RectTransform rtD = textoDiasGO.GetComponent<RectTransform>();
        rtD.anchorMin = new Vector2(0.58f, 0.1f);
        rtD.anchorMax = new Vector2(0.72f, 0.9f);
        rtD.offsetMin = Vector2.zero;
        rtD.offsetMax = Vector2.zero;

        // BotonesSlot
        GameObject botonesGO = new GameObject("BotonesSlot", typeof(RectTransform));
        botonesGO.transform.SetParent(fila.transform, false);
        RectTransform rtB = botonesGO.GetComponent<RectTransform>();
        rtB.anchorMin = new Vector2(0.70f, 0.1f);
        rtB.anchorMax = new Vector2(1f,    0.9f);
        rtB.offsetMin = Vector2.zero;
        rtB.offsetMax = new Vector2(-8f, 0f);
        HorizontalLayoutGroup hlg = botonesGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6f;
        hlg.childAlignment       = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;

        GameObject botonGuardarGO = CrearBoton("BotonGuardar", botonesGO.transform, "Guardar", ColorBotonMarron, 80f, 30f);
        GameObject botonCargarGO  = CrearBoton("BotonCargar",  botonesGO.transform, "Cargar",  ColorBotonAzul,   80f, 30f);
        GameObject botonBorrarGO  = CrearBoton("BotonBorrar",  botonesGO.transform, "Borrar",  ColorBotonRojo,   80f, 30f);
        AplicarLayoutElement(botonGuardarGO, 80f, 30f);
        AplicarLayoutElement(botonCargarGO,  80f, 30f);
        AplicarLayoutElement(botonBorrarGO,  80f, 30f);

        // Cablear SlotUI
        SlotUI slotUI = fila.AddComponent<SlotUI>();
        AsignarCampo(slotUI, "_contenedorBotones", botonesGO);
        AsignarCampo(slotUI, "_textoNombre",  textoNombreGO.GetComponent<TextMeshProUGUI>());
        AsignarCampo(slotUI, "_textoFecha",   textoFechaGO.GetComponent<TextMeshProUGUI>());
        AsignarCampo(slotUI, "_textoDias",    textoDiasGO.GetComponent<TextMeshProUGUI>());
        AsignarCampo(slotUI, "_botonGuardar", botonGuardarGO.GetComponent<Button>());
        AsignarCampo(slotUI, "_botonCargar",  botonCargarGO.GetComponent<Button>());
        AsignarCampo(slotUI, "_botonBorrar",  botonBorrarGO.GetComponent<Button>());

        return slotUI;
    }

    // ─── Helpers de creación ──────────────────────────────────────────────────

    private static GameObject CrearImagen(string nombre, Transform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject CrearTexto(string nombre, Transform padre, string texto,
                                         float tamano, Color color, TextAlignmentOptions alineacion)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = tamano;
        tmp.color     = color;
        tmp.alignment = alineacion;
        return go;
    }

    private static GameObject CrearBoton(string nombre, Transform padre, string etiqueta,
                                         Color colorFondo, float ancho, float alto, float tamanoFuente = 12f)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.color = colorFondo;
        go.GetComponent<Button>().targetGraphic = img;

        var textoGO = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(go.transform, false);
        Estirar(textoGO.GetComponent<RectTransform>());
        var tmp = textoGO.GetComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = tamanoFuente;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ancho, alto);

        return go;
    }

    private static void AplicarLayoutElement(GameObject go, float ancho, float alto)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        if (ancho > 0f) { le.preferredWidth  = ancho; le.minWidth  = ancho; }
        if (alto  > 0f) { le.preferredHeight = alto;  le.minHeight = alto;  }
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CentrarFijo(RectTransform rt, float ancho, float alto)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(ancho, alto);
        rt.anchoredPosition = Vector2.zero;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
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
            Debug.LogWarning($"[PantallaSlotsEditorSetup] Campo '{nombreCampo}' no encontrado en " +
                             $"{objetivo.GetType().Name}.");
            return;
        }

        campo.SetValue(objetivo, valor);
    }
}
