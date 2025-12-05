using UnityEngine;
 
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;   // new input
#endif

[DefaultExecutionOrder(-10000)]
public class EventSystemGovernor : MonoBehaviour
{
    [Header("Project Setup")]
    [Tooltip("If TRUE, we keep exactly one EventSystem for UGUI menus. If FALSE and your whole UI is UITK, we remove all EventSystems.")]
    public bool needUGUIEventSystem = true;   // set TRUE if MainMenu/Selection use UGUI

    [Tooltip("Print what created/replaced/removed to Console for debugging.")]
    public bool verboseLogs = true;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Reconcile once now in case Bootstrap already has UGUI
        ReconcileEventSystems("Awake");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Wait one frame so any prefabs that spawn an ES get a chance to instantiate
        StartCoroutine(DelayAndReconcile());
    }

    System.Collections.IEnumerator DelayAndReconcile()
    {
        yield return null;
        ReconcileEventSystems("SceneLoaded");
    }

    void ReconcileEventSystems(string reason)
    {
        var all = FindObjectsOfType<EventSystem>(true);

        if (!needUGUIEventSystem)
        {
            // UITK-only project: remove all ES to avoid interference
            foreach (var es in all)
            {
                if (verboseLogs) Debug.Log($"[ES-Governor] ({reason}) Removing EventSystem '{es.name}' from scene '{es.gameObject.scene.name}'");
                Destroy(es.gameObject);
            }
            return;
        }

        // We need exactly one ES. Prefer the oldest (first created) and reconfigure it; remove the rest.
        EventSystem keep = null;
        foreach (var es in all)
        {
            if (keep == null) keep = es;
            else
            {
                if (verboseLogs) Debug.Log($"[ES-Governor] ({reason}) Destroying duplicate EventSystem '{es.name}' in '{es.gameObject.scene.name}'");
                Destroy(es.gameObject);
            }
        }

        // If none existed, create one now.
        if (keep == null)
        {
            var go = new GameObject("GlobalEventSystem");
            keep = go.AddComponent<EventSystem>();
            DontDestroyOnLoad(go);
            if (verboseLogs) Debug.Log($"[ES-Governor] ({reason}) Created GlobalEventSystem");
        }

        // Ensure correct input module
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New input system: must have InputSystemUIInputModule; remove legacy
        var legacy = keep.GetComponent<StandaloneInputModule>();
        if (legacy) { if (verboseLogs) Debug.Log("[ES-Governor] Removing StandaloneInputModule (old input)"); Destroy(legacy); }
        var baseInput = keep.GetComponent<BaseInput>(); // sometimes auto-added with legacy
        if (baseInput) Destroy(baseInput);
        if (!keep.GetComponent<InputSystemUIInputModule>())
        {
            keep.gameObject.AddComponent<InputSystemUIInputModule>();
            if (verboseLogs) Debug.Log("[ES-Governor] Added InputSystemUIInputModule");
        }
#else
        // Legacy input: must have StandaloneInputModule; remove new
        var newMod = keep.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (newMod) { if (verboseLogs) Debug.Log("[ES-Governor] Removing InputSystemUIInputModule (new input)"); Destroy(newMod); }
        if (!keep.GetComponent<StandaloneInputModule>())
        {
            keep.gameObject.AddComponent<StandaloneInputModule>();
            if (verboseLogs) Debug.Log("[ES-Governor] Added StandaloneInputModule");
        }
#endif

        if (verboseLogs)
        {
            Debug.Log($"[ES-Governor] ({reason}) Active ES: '{keep.name}' in scene '{keep.gameObject.scene.name}' | module={(keep.GetComponent<StandaloneInputModule>()? "Legacy":"New")}");
        }
    }
}
