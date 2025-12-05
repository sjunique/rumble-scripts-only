using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders; // <- for SceneInstance
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

 
 
 
 
 

public class NewGameFlow : MonoBehaviour
{
    [Header("Scene Keys (Addressables) — no MainMenu here")]
    public string selectionSceneKey = "SelectionScreen";
    public string pauseMenuSceneKey = "PauseMenu";
    public string defaultGameplayKey = "Level_0_Main";
public string menuSceneKey = "MainMenu";
    private SceneLoadService Loader => SceneLoadService.Instance;

    // Pause overlay tracking
    private Scene? _pauseScene;
    private CanvasGroup _pauseCG;
    private bool _isPaused;

    // Track currently loaded gameplay (so we can unload on restart/quit)
    private string _currentGameplayScene;

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (!SceneLoadService.Instance)
        {
            var go = new GameObject("SceneLoadService");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneLoadService>();
        }
    }

    async void Start()
    {
        // Initialize loader only — do NOT auto load any MainMenu here
        await Loader.InitializeAsync();
        OnEnable();
    }

    void OnEnable()
    {
        var lives = FindObjectOfType<LivesService>(true);
        if (lives != null)
        {
            lives.OnOutOfLives -= HandleOutOfLives;
            lives.OnOutOfLives += HandleOutOfLives;
        }
    }

    void HandleOutOfLives()
    {
        // For now, exit game (no MainMenu flow inside GameFlow).
        QuitToMenu();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Pause overlay helpers (works if your pause has a CanvasGroup; harmless otherwise)

    void CachePauseCanvasGroup()
    {
        if (_pauseCG != null) return;
        if (!_pauseScene.HasValue || !_pauseScene.Value.isLoaded) return;

        foreach (var root in _pauseScene.Value.GetRootGameObjects())
        {
            _pauseCG = root.GetComponentInChildren<CanvasGroup>(true);
            if (_pauseCG) break;
        }
        if (_pauseCG == null)
            Debug.LogWarning("[GameFlow] PauseMenu has no CanvasGroup. (OK if you use pure UITK)");
    }

    void SetPauseCanvasVisible(bool visible)
    {
        CachePauseCanvasGroup();
        if (_pauseCG == null) return;

        _pauseCG.alpha = visible ? 1f : 0f;
        _pauseCG.interactable = visible;
        _pauseCG.blocksRaycasts = visible;
    }

    // If you prefer a per-scene CanvasGroup toggle:
    static void SetSceneCanvasActive(Scene s, bool active)
    {
        if (!s.IsValid() || !s.isLoaded) return;
        foreach (var root in s.GetRootGameObjects())
        {
            var cg = root.GetComponentInChildren<CanvasGroup>(true);
            if (cg)
            {
                cg.alpha = active ? 1f : 0f;
                cg.interactable = active;
                cg.blocksRaycasts = active;
            }
            else
            {
                foreach (var c in root.GetComponentsInChildren<Canvas>(true))
                    c.enabled = active;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API (no MainMenu load/unload here)

    public async void PlayLevelsssss(string gameplayKey)
    {
        _currentGameplayScene = gameplayKey;

        // Ensure selection isn’t lingering

        
        await Loader.UnloadScene(selectionSceneKey);

        // Load gameplay
        var gameplay = await Loader.LoadSceneAdditive(gameplayKey, activate: true);

        // Load pause overlay additively, keep hidden until used
        var pause = await Loader.LoadSceneAdditive(pauseMenuSceneKey, activate: true);
        if (pause.HasValue) _pauseScene = pause.Value.Scene;

        _isPaused = false;
        Time.timeScale = 1f;
        SetPauseCanvasVisible(false);

        if (gameplay.HasValue)
            SceneManager.SetActiveScene(gameplay.Value.Scene);
    }
 

public async void PlayLevel(string gameplayKey)
{
    _currentGameplayScene = gameplayKey;

    // Unload Selection if lingering
    await Loader.UnloadScene(selectionSceneKey);

    // Load gameplay
    var gameplay = await Loader.LoadSceneAdditive(gameplayKey, activate: true);
    if (gameplay.HasValue) SceneManager.SetActiveScene(gameplay.Value.Scene);

    // NEW: auto-unload MainMenu (by marker) now that gameplay is active
    if (TryFindLoadedSceneWith<MenuSceneMarker>(out var menuScene))
        await UnloadSceneSafeAsync(menuScene);

    // Load Pause overlay, keep hidden
    var pause = await Loader.LoadSceneAdditive(pauseMenuSceneKey, activate: true);
    if (pause.HasValue) _pauseScene = pause.Value.Scene;

    _isPaused = false;
    Time.timeScale = 1f;
    SetPauseCanvasVisible(false);
}





    public async Task RestartActiveLevelAsync()
    {
        var active = SceneManager.GetActiveScene();
        if (!active.IsValid())
        {
            Debug.LogWarning("[GameFlow] No active scene to restart.");
            return;
        }

        Debug.Log($"[GameFlow] Restarting '{active.name}'…");

        // Reload gameplay (Single clears additives)
        var reload = Addressables.LoadSceneAsync(active.name, LoadSceneMode.Single);
        await reload.Task;
        if (reload.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[GameFlow] Restart failed: {active.name}");
            return;
        }

        // Re-load PauseMenu overlay and keep it hidden
        var pause = await Loader.LoadSceneAdditive(pauseMenuSceneKey, activate: true);
        if (pause.HasValue) _pauseScene = pause.Value.Scene;
        SetPauseCanvasVisible(false);

        _isPaused = false;
        Time.timeScale = 1f;
    }

    // Selection hub (kept if you still use a level selection scene)
    public async void GoToSelection()
    {
        var si = await Loader.LoadSceneAdditive(selectionSceneKey, activate: true);

        // Ensure pause isn’t stacked on selection
     
        await Loader.UnloadScene(pauseMenuSceneKey);

        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);

    // NEW: auto-unload MainMenu (by marker) once Selection is active
    if (TryFindLoadedSceneWith<MenuSceneMarker>(out var menuScene))
        await UnloadSceneSafeAsync(menuScene);



    }

    // Exit app (Editor-safe). This replaces any “return to MainMenu” inside GameFlow.
    public void QuitToMenu() => QuitUtil.Quit();

    // ─────────────────────────────────────────────────────────────────────────────
    // (Optional) Legacy variants removed to avoid confusion:
    //  - ShowMenu()
    //  - ContinueLast()
    //  - PlayLevelprev()/PlayLevels()
    //  - QuitToMenup()/QuitToMenuorig()/QuitToMenuprev()








// --- Helpers: find & unload a loaded scene that contains a specific component type ---

static bool TryFindLoadedSceneWith<T>(out Scene found) where T : Component
{
    for (int i = 0; i < SceneManager.sceneCount; i++)
    {
        var s = SceneManager.GetSceneAt(i);
        if (!s.IsValid() || !s.isLoaded) continue;

        var roots = s.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            if (roots[r].GetComponentInChildren<T>(true))
            {
                found = s;
                return true;
            }
        }
    }
    found = default;
    return false;
}

// Put at top if missing:
// using System.Threading.Tasks;
// using UnityEngine.SceneManagement;

static async Task UnloadSceneSafeAsync(Scene s)
{
    if (!s.IsValid() || !s.isLoaded) return;

    // If it's the active scene, pick another active first
    if (SceneManager.GetActiveScene() == s)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var candidate = SceneManager.GetSceneAt(i);
            if (candidate.isLoaded && candidate != s)
            {
                SceneManager.SetActiveScene(candidate);
                break;
            }
        }
    }

    // Unload via SceneManager (works regardless of how the scene was loaded)
    var op = SceneManager.UnloadSceneAsync(s);
    if (op == null) return;
    while (!op.isDone) await Task.Yield();
}















}


