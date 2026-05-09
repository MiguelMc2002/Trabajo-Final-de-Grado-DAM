#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class DebugCasillaHex
{
    private static bool _activo = false;
    private static Grid _grid;

    static DebugCasillaHex()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("TFG/Debug Casilla Hex/Activar")]
    public static void Activar()
    {
        _activo = true;
        _grid = Object.FindFirstObjectByType<Grid>();
        Debug.Log("[DebugCasillaHex] Activado. Haz clic en la Scene view para ver coordenadas.");
    }

    [MenuItem("TFG/Debug Casilla Hex/Desactivar")]
    public static void Desactivar()
    {
        _activo = false;
        Debug.Log("[DebugCasillaHex] Desactivado.");
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_activo || _grid == null) return;

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector3 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            worldPos.z = 0;
            Vector3Int casilla = _grid.WorldToCell(worldPos);
            Debug.Log($"[DebugCasillaHex] Casilla: ({casilla.x}, {casilla.y}, {casilla.z})");
            e.Use();
        }
    }
}
#endif
