using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DefaultExecutionOrder(-10000)]
public class PrimaryEventSystem : MonoBehaviour
{
    [Tooltip("Disable any other EventSystems that appear at runtime.")]
    public bool autoFix = true;

    [Tooltip("Convert StandaloneInputModule -> InputSystemUIInputModule on THIS EventSystem.")]
    public bool enforceNewInputSystem = true;

    [Tooltip("Optionally make this survive scene loads.")]
    public bool dontDestroyOnLoad = false;

    EventSystem primary;

    void Awake()
    {
        primary = GetComponent<EventSystem>();

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        if (enforceNewInputSystem)
            EnsureInputSystemModule(primary);

        // Immediately disable any others that already exist
        Scan(disableExtras:true);

        // Catch future spawns
        SceneManager.sceneLoaded += (_, __) => Scan(disableExtras:true);
        InvokeRepeating(nameof(Poll), 0.1f, 0.5f); // catches Addressables / late instantiation
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= (_, __) => Scan(disableExtras:true);
        CancelInvoke();
    }

    void Poll() => Scan(disableExtras:autoFix);

    void Scan(bool disableExtras)
    {
        var all = FindObjectsOfType<EventSystem>(true);

        // Prefer this component as primary, even if others are in DontDestroyOnLoad
        foreach (var es in all)
        {
            if (es == null) continue;
            if (es == primary) continue;

            // If we shouldn't auto-fix, just log and continue
            if (!disableExtras)
            {
                Debug.LogWarning($"[PrimaryEventSystem] Extra EventSystem detected: {Describe(es)}");
                continue;
            }

            // Disable or destroy the extra one
            Debug.LogWarning($"[PrimaryEventSystem] Disabling duplicate EventSystem: {Describe(es)}");
            es.gameObject.SetActive(false);
        }
    }

    static string Describe(EventSystem es)
    {
        var go = es.gameObject;
        var scene = go.scene.IsValid() ? go.scene.name : "NoScene";
        return $"{go.name} | scene={scene} | path={GetPath(go.transform)}";
    }

    static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent) { t = t.parent; s = t.parent.name + "/" + s; }
        return s;
    }

    static void EnsureInputSystemModule(EventSystem es)
    {
        if (es == null) return;

        // Remove legacy module
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);

        #if ENABLE_INPUT_SYSTEM
        var modern = es.GetComponent<InputSystemUIInputModule>();
        if (!modern) es.gameObject.AddComponent<InputSystemUIInputModule>();
        #endif
    }
}
