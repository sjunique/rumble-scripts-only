using UnityEngine;
public class CollectibleRuntimeProbe : MonoBehaviour
{
    public string collectibleRootName = "CollectiblesRoot"; // parent you spawn under
    public string prefabKeyword = "collectible";            // name contains...

    void Start()
    {
        var root = GameObject.Find(collectibleRootName);
        if (!root) { Debug.LogWarning("[COLLECT] No root found."); return; }

        int total = 0, hidden = 0, editorOnly = 0, culled = 0, inactive = 0;
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (!r || !r.gameObject.name.ToLower().Contains(prefabKeyword)) continue;
            total++;

            if (!r.gameObject.activeInHierarchy) { inactive++; continue; }
            if (!r.enabled) { hidden++; continue; }

            if (r.gameObject.CompareTag("EditorOnly")) editorOnly++;

            var cam = Camera.main;
            if (cam && (cam.cullingMask & (1 << r.gameObject.layer)) == 0) culled++;
        }
        Debug.Log($"[COLLECT] total={total} inactive={inactive} hidden={hidden} editorOnly={editorOnly} culledByCamera={culled}");
    }
}
