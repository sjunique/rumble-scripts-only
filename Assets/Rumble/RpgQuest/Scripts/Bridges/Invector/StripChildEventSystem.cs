using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class StripChildEventSystem : MonoBehaviour
{
    [Tooltip("If no other EventSystem exists, keep the child as a fallback.")]
    public bool keepIfAlone = true;

    [Tooltip("When keeping a global EventSystem, ensure it uses the new Input System.")]
    public bool enforceInputSystemOnGlobal = true;

    void Awake()
    {
        // Find any existing EventSystem in the scene (could be in a bootstrap object)
        var globals = FindObjectsOfType<EventSystem>(true);

        // Find the child ES under this prefab, if any
        var childES = GetComponentInChildren<EventSystem>(true);

        // If no child, nothing to do
        if (!childES) return;

        // If there is already another ES in the scene, we can safely remove the child
        bool thereIsAnother = globals.Length > 0 && System.Array.IndexOf(globals, childES) < 0;

        if (thereIsAnother)
        {
            Debug.Log("[StripChildEventSystem] Removing child EventSystem from player prefab (another ES exists).", this);
            Destroy(childES.gameObject);
        }
        else if (!keepIfAlone)
        {
            Debug.Log("[StripChildEventSystem] Removing child EventSystem (forced), leaving scene without ES.", this);
            Destroy(childES.gameObject);
        }
        else
        {
            // Keep the child as the only ES; make sure it’s the right module
            EnsureInputModule(childES);
        }

        // If we’re relying on a global ES, ensure it uses the Input System
        if (thereIsAnother && enforceInputSystemOnGlobal)
        {
            foreach (var es in globals)
                EnsureInputModule(es);
        }
    }

    static void EnsureInputModule(EventSystem es)
    {
        if (!es) return;
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);

        #if ENABLE_INPUT_SYSTEM
        if (!es.GetComponent<InputSystemUIInputModule>())
            es.gameObject.AddComponent<InputSystemUIInputModule>();
        #endif
    }
}
