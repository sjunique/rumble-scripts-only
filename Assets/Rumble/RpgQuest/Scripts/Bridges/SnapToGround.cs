
 
using UnityEngine;
 
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SnapToGround : EditorWindow
{
    public LayerMask groundLayers = ~0; // Default: all layers
    public string requiredTag = "";     // Leave blank to hit anything

    [MenuItem("Tools/Snap Selected To Ground")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SnapToGround), true, "Snap To Ground");
    }

    void OnGUI()
    {
        groundLayers = EditorGUILayout.MaskField("Ground Layers", groundLayers, GetLayerNames());
        requiredTag = EditorGUILayout.TextField("Only Snap To Tag (optional)", requiredTag);

        if (GUILayout.Button("Snap Selected Objects"))
        {
            int snapped = 0;
            foreach (GameObject obj in Selection.gameObjects)
            {
                RaycastHit hit;
                Vector3 origin = obj.transform.position + Vector3.up * 100f;

                if (Physics.Raycast(origin, Vector3.down, out hit, 1000f, groundLayers))
                {
                    if (string.IsNullOrEmpty(requiredTag) || hit.collider.CompareTag(requiredTag))
                    {
                        Undo.RecordObject(obj.transform, "Snap To Ground");
                        obj.transform.position = hit.point + Vector3.up * 0.05f;
                        snapped++;
                    }
                }
            }
            Debug.Log("Snapped " + snapped + " object(s) to ground.");
        }
    }

    // Helper for layer mask selection
    private string[] GetLayerNames()
    {
        string[] layers = new string[32];
        for (int i = 0; i < 32; i++)
            layers[i] = LayerMask.LayerToName(i);
        return layers;
    }
}
#endif

