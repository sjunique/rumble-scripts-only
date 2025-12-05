using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class EventSystemSentry : MonoBehaviour
{
    [Tooltip("If true, disables extra EventSystems automatically.")]
    public bool autoFix = true;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InvokeRepeating(nameof(Scan), 0.1f, 0.5f); // catch late spawns (Addressables, etc.)
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CancelInvoke();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Scan();

    void Scan()
    {
        var all = FindObjectsOfType<EventSystem>(true);
        if (all.Length <= 1) return;

        Debug.LogWarning($"[EventSystemSentry] {all.Length} EventSystems detected:");
        for (int i = 0; i < all.Length; i++)
        {
            var es = all[i];
            var path = GetPath(es.transform);
            Debug.Log($"  [{i}] {es.name}  (active={es.isActiveAndEnabled})  scene={es.gameObject.scene.name}  path={path}");
        }

        if (!autoFix) return;

        // Keep the oldest/first, disable the rest
        var keep = all.OrderBy(e => e.GetInstanceID()).First();
        foreach (var es in all)
        {
            if (es == keep) continue;
            Debug.LogWarning($"[EventSystemSentry] Disabling duplicate EventSystem '{es.name}'");
            es.gameObject.SetActive(false);
        }
    }

    static string GetPath(Transform t)
    {
        var s = t.name;
        while (t.parent) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
