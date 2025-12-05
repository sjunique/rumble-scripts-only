using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;   // New Input System UI module

[DefaultExecutionOrder(-10000)]
public class PersistentEventSystem : MonoBehaviour
{
    void Awake()
    {
        // If another EventSystem exists and it's not us, keep the existing one.
        if (EventSystem.current != null && EventSystem.current.gameObject != gameObject)
        {
            Destroy(gameObject);
            return;
        }

        // Ensure we have an EventSystem component and make it current.
        var es = GetComponent<EventSystem>();
        if (!es) es = gameObject.AddComponent<EventSystem>();
        EventSystem.current = es;

        // Remove legacy modules if present.
        var legacy = GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);
        var baseInput = GetComponent<BaseInput>();
        if (baseInput) Destroy(baseInput);

        // Ensure New Input System UI module exists.
        var newModule = GetComponent<InputSystemUIInputModule>();
        if (!newModule) newModule = gameObject.AddComponent<InputSystemUIInputModule>();

        // If the module has no actions assigned, create a default set so it works immediately.
#if UNITY_EDITOR
        if (newModule.actionsAsset == null)
        {
            // This creates & assigns a default UI actions asset in Editor so you can run right away.
            // In production, assign your own InputActionAsset with a proper "UI" action map.
            UnityEditor.Undo.RecordObject(newModule, "Assign default UI Actions");
            newModule.AssignDefaultActions();
            UnityEditor.EditorUtility.SetDirty(newModule);
        }
#endif

        DontDestroyOnLoad(gameObject);
    }
}
