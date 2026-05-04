using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de editor que busca todos los GameObjects llamados "BotonesSlot"
/// en la escena activa y los activa. Útil para reparar escenas donde el script
/// de generación los dejó desactivados, impidiendo que los botones sean visibles.
/// Ejecutar manualmente en cada escena afectada desde TFG → Reparar → Activar BotonesSlot.
/// </summary>
public static class ActivarBotonesSlot
{
    [MenuItem("TFG/Reparar/Activar BotonesSlot en escena activa")]
    public static void Reparar()
    {
        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

        int contador = 0;
        foreach (Transform t in todos)
        {
            if (t.name != "BotonesSlot") continue;

            Undo.RecordObject(t.gameObject, "Activar BotonesSlot");
            t.gameObject.SetActive(true);
            EditorUtility.SetDirty(t.gameObject);

            foreach (Transform hijo in t)
            {
                Undo.RecordObject(hijo.gameObject, "Activar botón slot");
                hijo.gameObject.SetActive(true);
                EditorUtility.SetDirty(hijo.gameObject);
            }

            contador++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ActivarBotonesSlot] {contador} GameObject(s) 'BotonesSlot' activados. " +
                  "Guarda la escena con Ctrl+S.");
    }
}
