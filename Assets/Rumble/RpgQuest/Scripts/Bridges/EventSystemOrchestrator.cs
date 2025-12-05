using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class EventSystemOrchestrator : MonoBehaviour
{
    static EventSystemOrchestrator _instance;
    public static EventSystemOrchestrator I => _instance;

    // Remember original enable states to restore correctly
    readonly List<EventSystem> _systems = new();
    readonly List<bool> _wasEnabled = new();
    bool _suspended;

    void Awake()
    {
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call when a Toolkit modal opens
    public void SuspendAllUGUI()
    {
        RefreshList();

        for (int i = 0; i < _systems.Count; i++)
        {
            var es = _systems[i];
            if (!es) continue;
            _wasEnabled[i] = es.enabled;
            es.enabled = false;
        }
        _suspended = true;
        Debug.Log("[EventSystemOrchestrator] Suspended all uGUI EventSystems.");
    }

    // Call when the Toolkit modal closes
    public void ResumeAllUGUI()
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            var es = _systems[i];
            if (!es) continue;
            es.enabled = _wasEnabled[i];
            EnsureInputModule(es); // make sure it’s the right module when it comes back
        }
        _suspended = false;
        Debug.Log("[EventSystemOrchestrator] Resumed all uGUI EventSystems.");
    }

    // Optional: call at spawn if you want to keep ONE primary only
    public void DisableAllBut(EventSystem keep)
    {
        foreach (var es in FindObjectsOfType<EventSystem>(true))
        {
            if (es == keep) continue;
            es.gameObject.SetActive(false);
        }
        Debug.Log("[EventSystemOrchestrator] Disabled all EventSystems except: " + keep.name);
    }

    void RefreshList()
    {
        _systems.Clear();
        _systems.AddRange(FindObjectsOfType<EventSystem>(true));
        _wasEnabled.Clear();
        _wasEnabled.AddRange(Enumerable.Repeat(true, _systems.Count));
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
