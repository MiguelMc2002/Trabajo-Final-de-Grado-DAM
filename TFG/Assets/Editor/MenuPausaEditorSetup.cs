using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Herramienta de editor que genera el panel de pausa completo en la escena activa
/// y crea el GestorPausa con el componente MenuPausa listo para usar.
/// Accesible desde el menú TFG → Generar Menú de Pausa.
/// </summary>
public static class MenuPausaEditorSetup
{
    private const string RutaBoton = "Assets/Sprites/Kenney/UI/PNG/buttonLong_brown.png";

    // Colores reutilizables
    private static readonly Color ColorDorado  = new Color(212f/255f, 175f/255f,  55f/255f, 1f);
    private static readonly Color ColorTexto   = new Color(1f,        245f/255f, 220f/255f, 1f);
    private static readonly Color ColorOverlay = new Color(0f, 0f, 0f, 180f/255f);

    [MenuItem("TFG/Generar Menú de Pausa")]
    public static void GenerarMenuPausa()
    {
        // ─── 1. Canvas de la escena ───────────────────────────────────────────

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MenuPausaEditorSetup] No se encontró Canvas en la escena. Se creará uno nuevo.");
            canvas = CrearCanvas();
        }

        // Crear EventSystem si la escena no tiene ninguno
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
            Debug.Log("[MenuPausaEditorSetup] EventSystem creado automáticamente.");
        }

        // ─── 2. Sprite del botón ──────────────────────────────────────────────

        Sprite spriteBoton = AssetDatabase.LoadAssetAtPath<Sprite>(RutaBoton);
        if (spriteBoton == null)
            Debug.LogWarning($"[MenuPausaEditorSetup] No se encontró el sprite en '{RutaBoton}'. " +
                             "Los botones usarán color sólido como fallback.");

        // ─── 3. PanelPausa ────────────────────────────────────────────────────

        GameObject panelPausa = new GameObject("PanelPausa");
        panelPausa.transform.SetParent(canvas.transform, false);

        Image imgPanel = panelPausa.AddComponent<Image>();
        imgPanel.color = ColorOverlay;

        // Stretch total sobre el Canvas
        RectTransform rtPanel = panelPausa.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;

        panelPausa.SetActive(false);

        // ─── 4. Título ────────────────────────────────────────────────────────

        GameObject tituloGo = new GameObject("TituloPausa");
        tituloGo.transform.SetParent(panelPausa.transform, false);

        TextMeshProUGUI titulo = tituloGo.AddComponent<TextMeshProUGUI>();
        titulo.text      = "PAUSA";
        titulo.fontSize  = 72;
        titulo.color     = ColorDorado;
        titulo.alignment = TextAlignmentOptions.Center;

        RectTransform rtTitulo = tituloGo.GetComponent<RectTransform>();
        // Anclaje superior-centro
        rtTitulo.anchorMin = new Vector2(0.5f, 1f);
        rtTitulo.anchorMax = new Vector2(0.5f, 1f);
        rtTitulo.pivot     = new Vector2(0.5f, 1f);
        rtTitulo.sizeDelta = new Vector2(400f, 100f);
        rtTitulo.anchoredPosition = new Vector2(0f, -150f);

        // ─── 5. Botones ───────────────────────────────────────────────────────

        Button btnContinuar     = CrearBoton(panelPausa, "BotonContinuar",      "Continuar",           spriteBoton,  60f);
        Button btnMenuPrincipal = CrearBoton(panelPausa, "BotonMenuPrincipal",  "Menú Principal",      spriteBoton,   0f);
        Button btnSalir         = CrearBoton(panelPausa, "BotonSalir",          "Salir al escritorio", spriteBoton, -60f);

        // ─── 6. GestorPausa ───────────────────────────────────────────────────

        GameObject gestorGo = new GameObject("GestorPausa");
        MenuPausa menuPausa = gestorGo.AddComponent<MenuPausa>();

        // Asignar _panelPausa mediante reflexión (campo privado serializado)
        AsignarCampo(menuPausa, "_panelPausa", panelPausa);

        // ─── 7. Conectar eventos OnClick ──────────────────────────────────────

        UnityEventTools.AddPersistentListener(
            btnContinuar.onClick,
            menuPausa.Continuar);

        UnityEventTools.AddPersistentListener(
            btnMenuPrincipal.onClick,
            menuPausa.IrAMenuPrincipal);

        UnityEventTools.AddPersistentListener(
            btnSalir.onClick,
            menuPausa.SalirAlEscritorio);

        // ─── 8. Marcar escena como modificada ────────────────────────────────

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[MenuPausaEditorSetup] Panel de pausa generado correctamente. " +
                  "Recuerda guardar la escena (Ctrl+S).");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un Canvas con Screen Space Overlay, CanvasScaler 1920×1080 y GraphicRaycaster.
    /// </summary>
    private static Canvas CrearCanvas()
    {
        GameObject go = new GameObject("Canvas");

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        Debug.Log("[MenuPausaEditorSetup] Canvas creado automáticamente.");
        return canvas;
    }

    /// <summary>
    /// Crea un botón centrado verticalmente en el panel con posición Y fija.
    /// </summary>
    private static Button CrearBoton(GameObject padre, string nombre, string etiqueta,
                                     Sprite sprite, float posY)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform, false);

        Image imagen = go.AddComponent<Image>();
        if (sprite != null)
        {
            imagen.sprite = sprite;
            imagen.type   = Image.Type.Sliced;
        }
        else
        {
            imagen.color = new Color(0.45f, 0.28f, 0.12f, 1f);
        }

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = imagen;

        // Anclaje al centro
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 50f);
        rt.anchoredPosition = new Vector2(0f, posY);

        // Texto del botón
        GameObject textoGo = new GameObject("Texto");
        textoGo.transform.SetParent(go.transform, false);

        TextMeshProUGUI tmp = textoGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = 24;
        tmp.color     = ColorTexto;
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
            Debug.LogWarning($"[MenuPausaEditorSetup] Campo '{nombreCampo}' no encontrado en " +
                             $"{objetivo.GetType().Name}.");
            return;
        }

        campo.SetValue(objetivo, valor);
    }
}
