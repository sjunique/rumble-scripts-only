using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif



using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
 using UnityEngine.Events;
 
 
 

/// <summary>
/// UITK pause overlay:
/// - Toggle with Esc (no EventSystem required)
/// - Resume / Restart (Addressables) / Quit (GameFlow or Addressables)
/// - Save / Load Checkpoint / Reset to Spawn:
///     1) auto-detect common managers and invoke by reflection (non-breaking)
///     2) or expose UnityEvents to wire your own logic in the Inspector
/// </summary>
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

using System;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// UITK pause overlay:
/// - Toggle with Esc
/// - Resume / Restart (Addressables) / Quit (MainMenu)
/// - Save / Load Checkpoint / Reset to Spawn
/// - NEW: Reset Progress (clear prefs + savefiles + quest state, then go to MainMenu)
/// </summary>
public class PauseOverlayController : MonoBehaviour
{

    GameFlow _flow;
    [Header("Assign UIDocument with PauseOverlay.uxml")]
    public UIDocument uiDocument;

    [Header("Scene keys (Addressables)")]
    public string menuKey = "MainMenu";   // used if GameFlow is not present

    [Header("Optional: Events to integrate your systems")]
    public UnityEvent onSaveRequested;
    public UnityEvent onLoadCheckpointRequested;
    public UnityEvent onResetToSpawnRequested;

    VisualElement _root;  // overlay
    Button _resume, _save, _load, _resetToSpawn, _restart, _quit;
    Button _resetProgress;                 // NEW
    bool _visible;


 
    // ---------------- Quit to Menu ----------------
    public async void OnQuitToMenus()
    {
        // 0) Hard reset pause state
        Time.timeScale = 1f;
        AudioListener.pause = false;
        UnityEngine.Cursor.visible = true;
       UnityEngine. Cursor.lockState = CursorLockMode.None;

        SetVisible(false);

        // 1) Load MainMenu (single) – unloads gameplay + pause scenes
        AsyncOperationHandle<SceneInstance> h =
            Addressables.LoadSceneAsync(menuKey, LoadSceneMode.Single);
        await h.Task;

        if (h.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[Pause] QuitToMenu failed to load '{menuKey}'");
            return;
        }

        // 2) Ensure there is an EventSystem for uGUI menus
        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (!es)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            UnityEngine.Object.DontDestroyOnLoad(go);
            Debug.Log("[Pause] Created EventSystem in MainMenu.");
        }

        // 3) Safety: disable any stray pause canvases if they were DontDestroyOnLoad’d
        foreach (var cg in Resources.FindObjectsOfTypeAll<CanvasGroup>())
        {
            if (!cg || !cg.gameObject) continue;
            if (cg.gameObject.name.IndexOf("Pause", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cg.alpha = 0;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        Debug.Log("[Pause] Returned to MainMenu cleanly.");
    }


public void OnQuitToMenu()
{
    // Common pause clean-up
    Time.timeScale = 1f;
    AudioListener.pause = false;
    UnityEngine.Cursor.visible = true;
      UnityEngine.Cursor.lockState = CursorLockMode.None;
    SetVisible(false);

    if (_flow != null)
    {
        // ✅ Normal path – let GameFlow + SceneLoadService handle unload/load
        _flow.QuitToMenu();
    }
    else
    {
        // 🟡 Fallback – use your original Addressables + EventSystem logic
        OnQuitToMenus();
    }
}




    Transform FindPlayerByTag(string tag = "Player")
    {
        var go = GameObject.FindGameObjectWithTag(tag);
        return go ? go.transform : null;
    }

    void Awake()
    {
  _flow = FindObjectOfType<GameFlow>(true);

    if (!uiDocument) uiDocument = GetComponent<UIDocument>();
    var root = uiDocument ? uiDocument.rootVisualElement : null;


        if (!uiDocument) uiDocument = GetComponent<UIDocument>();
         root = uiDocument ? uiDocument.rootVisualElement : null;

        _root          = root?.Q<VisualElement>("overlay");
        _resume        = root?.Q<Button>("resumeBtn");
        _save          = root?.Q<Button>("saveBtn");
        _load          = root?.Q<Button>("loadBtn");
        _resetToSpawn  = root?.Q<Button>("resetBtn");
        _restart       = root?.Q<Button>("restartBtn");
        _quit          = root?.Q<Button>("quitBtn");
        _resetProgress = root?.Q<Button>("resetProgressBtn");  // NEW (see UXML below)

        SetVisible(false);

        _resume?.RegisterCallback<ClickEvent>(_ => OnResume());
        _save?.RegisterCallback<ClickEvent>(_ => OnSave());
        _load?.RegisterCallback<ClickEvent>(_ => OnLoadCheckpoint());
        _resetToSpawn?.RegisterCallback<ClickEvent>(_ => OnResetToSpawn());
        _restart?.RegisterCallback<ClickEvent>(_ => OnRestart());
        _quit?.RegisterCallback<ClickEvent>(_ => OnQuitToMenu());
        _resetProgress?.RegisterCallback<ClickEvent>(_ => OnResetProgress());  // NEW
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetPaused(!_visible);
    }

    // ---------- Public control ----------
    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        AudioListener.pause = paused;
        UnityEngine.Cursor.visible = paused;
        UnityEngine.Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        SetVisible(paused);
    }

    void SetVisible(bool on)
    {
        _visible = on;
        if (_root != null)
            _root.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------- Button handlers ----------
    void OnResume() => SetPaused(false);

    async void OnRestart()
    {
        SetPaused(false);

        var player = GameObject.FindGameObjectWithTag("Player");
        SceneCameraUtil.RestoreGameplayCamera(player);

        var active = SceneManager.GetActiveScene();
        if (!active.IsValid()) return;

        var h = Addressables.LoadSceneAsync(active.name, LoadSceneMode.Single);
        await h.Task;

        // Re-add pause overlay scene (if you keep it as a separate additive scene)
        Addressables.LoadSceneAsync("PauseMenu", LoadSceneMode.Additive);
    }

    void OnSave()
    {
        if (InvokeIfHasListeners(onSaveRequested)) return;

        var player = FindPlayerByTag("Player");
        if (!player)
        {
            Debug.LogWarning("[Pause] Save aborted: no Player (tag).");
            return;
        }

        var scene = SceneManager.GetActiveScene().name;
        SaveService.SaveSceneCheckpoint(scene, player.position, player.rotation);
        Debug.Log($"[Pause] Saved checkpoint in '{scene}' at {player.position}");
    }

    async void OnLoadCheckpoint()
    {
        if (InvokeIfHasListeners(onLoadCheckpointRequested)) return;

        var activeScene = SceneManager.GetActiveScene().name;

        if (!SaveService.TryLoadSceneCheckpoint(activeScene, out var cp) || cp == null || !cp.has)
        {
            Debug.LogWarning("[Pause] No checkpoint saved for this scene.");
            return;
        }

        string targetScene = SaveService.LoadProfile().lastPlayedScene;
        if (!string.IsNullOrEmpty(targetScene) && targetScene != activeScene)
        {
            Debug.Log($"[Pause] Loading scene '{targetScene}' for checkpoint…");
            var h = Addressables.LoadSceneAsync(targetScene, LoadSceneMode.Single);
            await h.Task;

            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[Pause] Failed to load scene '{targetScene}'.");
                return;
            }
        }

        var player = FindPlayerByTag("Player");
        if (!player)
        {
            Debug.LogWarning("[Pause] Load aborted: no Player after scene load.");
            return;
        }

        SaveService.TryLoadSceneCheckpoint(SceneManager.GetActiveScene().name, out cp);
        player.SetPositionAndRotation(cp.pos, cp.rot);
        Debug.Log($"[Pause] Loaded checkpoint @ {cp.pos}");
    }

    void OnResetToSpawn()
    {
        if (InvokeIfHasListeners(onResetToSpawnRequested)) return;

        var loc = UnityEngine.Object.FindObjectOfType<GameLocationManager>(true);
        var player = FindPlayerByTag("Player");

        if (!loc || !player)
        {
            Debug.LogWarning("[Pause] Reset aborted: missing GameLocationManager or Player.");
            return;
        }

        bool ok = loc.TeleportPlayer("PlayerSpawnPoint", player);
        if (ok) Debug.Log("[Pause] Reset to scene spawn (PlayerSpawnPoint).");
    }

    /// <summary>
    /// NEW: full game reset – clear prefs + savefiles + quest state, then go to MainMenu.
    /// </summary>
void OnResetProgress()
{
    Debug.Log("[Pause] Reset Progress clicked");

    // 1) Clear PlayerPrefs
    PlayerPrefs.DeleteAll();
    PlayerPrefs.Save();

    // 2) Clear SaveService data
    SaveService.DeleteProfile();
    SaveService.DeleteAllSceneCheckpoints();

    // 3) Reset quests in this session
    var qm = QuestManager.Instance ?? UnityEngine.Object.FindObjectOfType<QuestManager>(true);
    qm?.ResetAllQuests();

    // 4) Unpause and go to Main Menu via GameFlow
    Time.timeScale = 1f;
    AudioListener.pause = false;

    if (_flow != null)
        _flow.QuitToMenu();
    else
        Addressables.LoadSceneAsync(menuKey, LoadSceneMode.Single);
}


    // ---------- Helpers ----------
    bool InvokeIfHasListeners(UnityEvent evt)
    {
        if (evt == null) return false;
        try { evt.Invoke(); return true; }
        catch { return false; }
    }

    bool TryInvokeServiceMethod(string typeName, string methodName)
    {
        var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
        var target = allBehaviours.FirstOrDefault(mb => mb != null && mb.GetType().Name == typeName);
        if (target == null) return false;

        var mi = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi == null) return false;

        try
        {
            mi.Invoke(target, null);
            Debug.Log($"[Pause] Invoked {typeName}.{methodName}()");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Pause] Failed to invoke {typeName}.{methodName}(): {e.Message}");
            return false;
        }
    }
}
