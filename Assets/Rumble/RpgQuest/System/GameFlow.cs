using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders; // <- for SceneInstance

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;














public class GameFlow : MonoBehaviour
{
    [Header("Scene Keys (Addressables)")]
    public string menuSceneKey = "MainMenu";
    public string levelSelectSceneKey = "LevelSelection";
    public string selectionSceneKey = "SelectionScreen"; // Char/Car selection
    public string pauseMenuSceneKey = "PauseMenu";
    public string defaultGameplayKey = "Level_0_Main";

    private Scene _pauseScene;
    private string _currentGameplayKey;
    private bool _isPaused;

    private SceneLoadService _loader;
    private SceneLoadService Loader => _loader ??= FindObjectOfType<SceneLoadService>(true);
    private LivesService _lives;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // ensure SceneLoadService exists
        if (!SceneLoadService.Instance)
        {
            var go = new GameObject("SceneLoadService");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneLoadService>();
        }
    }

    async void Start()
    {
        if (SceneManager.GetSceneByName(menuSceneKey).isLoaded)
        {
            Debug.Log("[GameFlow] MainMenu already loaded, skipping ShowMenu().");
            return;
        }

        await Loader.InitializeAsync();
        HookLives();
        await ShowMenu();
    }



    void HookLives()
    {
        _lives = FindObjectOfType<LivesService>(true);
        if (_lives != null)
        {
            _lives.OnOutOfLives -= HandleOutOfLives;
            _lives.OnOutOfLives += HandleOutOfLives;
        }
    }

    void HandleOutOfLives()
    {
        Debug.LogWarning("[GameFlow] Out of lives detected → QuitToMenu()");
        QuitToMenu();
    }

    // ───────────────────────────────────────────────

    public async Task ShowMenu()
    {
        await Loader.UnloadScene(menuSceneKey);
        var si = await Loader.LoadSceneAdditive(menuSceneKey, activate: true);

        await Loader.UnloadScenes(selectionSceneKey, pauseMenuSceneKey);
        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
            _lives ??= FindObjectOfType<LivesService>(true);
   // _lives?.ResetLives();  // fresh run whenever menu shows
     //   Loader.DumpLoaded();
    }

    public async void GoToLevelSelection()
    {
        var si = await Loader.LoadSceneAdditive(levelSelectSceneKey, activate: true);
        await Loader.UnloadScenes(menuSceneKey, selectionSceneKey, pauseMenuSceneKey);

        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
    }

    public async void GoToSelection()
    {
        var si = await Loader.LoadSceneAdditive(selectionSceneKey, activate: true);
        await Loader.UnloadScenes(levelSelectSceneKey, menuSceneKey, pauseMenuSceneKey);

        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
    }

    // Called from LauncherUITKController after Char/Car pick
    public async void PlayLevel(string gameplayKey)
    {
        if (string.IsNullOrEmpty(gameplayKey))
            gameplayKey = defaultGameplayKey;

        _currentGameplayKey = gameplayKey;

        var gameplay = await Loader.LoadSceneAdditive(gameplayKey, activate: true);
var resetter = Object.FindAnyObjectByType<MonoBehaviour>(UnityEngine.FindObjectsInactive.Include) as ICameraReset; // 2022+
if (resetter != null) resetter.ResetCamera();

        // Always reload fresh pause menu
        if (!string.IsNullOrEmpty(pauseMenuSceneKey))
        {
            await Loader.UnloadScene(pauseMenuSceneKey);
            var pause = await Loader.LoadSceneAdditive(pauseMenuSceneKey, activate: true);
            if (pause.HasValue && pause.Value.Scene.IsValid())
                _pauseScene = pause.Value.Scene;
        }

        _isPaused = false;
        Time.timeScale = 1f;

        await Loader.UnloadScenes(selectionSceneKey, levelSelectSceneKey, menuSceneKey);

        if (gameplay.HasValue) SceneManager.SetActiveScene(gameplay.Value.Scene);
        Loader.DumpLoaded();
    }

   // Add a guard:
private bool _quitInProgress = false;

public async void QuitToMenu()
{
    if (_quitInProgress) { Debug.Log("[GameFlow] Quit already in progress."); return; }
    _quitInProgress = true;
    try
    {
        Debug.Log("[GameFlow] QuitToMenu invoked");

        Time.timeScale = 1f;
        _isPaused = false;

        if (_pauseScene.IsValid() && _pauseScene.isLoaded)
            await Loader.UnloadScene(pauseMenuSceneKey);

        if (!string.IsNullOrEmpty(_currentGameplayKey))
            await Loader.UnloadScene(_currentGameplayKey);

        await Loader.UnloadScenes(selectionSceneKey, levelSelectSceneKey);

        // Explicit re-load, but only once thanks to the guard
        await Loader.UnloadScene(menuSceneKey);
        await Loader.SwitchTo(menuSceneKey);

        _currentGameplayKey = null;

        await Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Loader.DumpLoaded();
    }
    finally
    {
        _quitInProgress = false;
    }
}

}



// public class GameFlow : MonoBehaviour
// {
//     [Header("Scene Keys (Addressables)")]
//     public string menuSceneKey            = "MainMenu";
//     public string levelSelectSceneKey     = "LevelSelection";   // Level picker (UITK)
//     public string selectionSceneKey       = "SelectionScreen";  // Char/Car selection (already implemented)
//     public string pauseMenuSceneKey       = "PauseMenu";
//     public string defaultGameplayKey      = "Level_0_Main";

//     // Internals
//     private Scene _pauseScene;
//     private string _currentGameplayKey = null;
//     private bool _isPaused = false;

//     private SceneLoadService _loader;
//     private SceneLoadService Loader => _loader ??= FindObjectOfType<SceneLoadService>(true);

//     void Awake()
//     {
//         DontDestroyOnLoad(gameObject);

//         if (!SceneLoadService.Instance)
//         {
//             var go = new GameObject("SceneLoadService");
//             DontDestroyOnLoad(go);
//             go.AddComponent<SceneLoadService>();
//         }
//     }

//     async void Start()
//     {
//         await Loader.InitializeAsync();
//         HookLives();
//         await ShowMenu();
//     }

//     void HookLives()
//     {
//         var lives = FindObjectOfType<LivesService>(true);
//         if (lives != null)
//         {
//             lives.OnOutOfLives -= HandleOutOfLives;
//             lives.OnOutOfLives += HandleOutOfLives;
//         }
//     }

//     void HandleOutOfLives() => QuitToMenu();

//     // ─────────────────────────────────────────────────────────────────────────────

//     public async Task ShowMenu()
//     {
//         var si = await Loader.LoadSceneAdditive(menuSceneKey, activate: true);
//         await Loader.UnloadScene(selectionSceneKey);
//         await Loader.UnloadScene(pauseMenuSceneKey);

//         if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
//     }

//     // MAIN MENU → LEVEL SELECTION
//     public async void GoToLevelSelection()
//     {
//         var si = await Loader.LoadSceneAdditive(levelSelectSceneKey, activate: true);
//         await Loader.UnloadScene(menuSceneKey);
//         await Loader.UnloadScene(selectionSceneKey);
//         await Loader.UnloadScene(pauseMenuSceneKey);

//         if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
//     }

//     // LEVEL SELECTION → CHARACTER/VEHICLE SELECTION
//     public async void GoToSelection()
//     {
//         var si = await Loader.LoadSceneAdditive(selectionSceneKey, activate: true);
//         await Loader.UnloadScene(levelSelectSceneKey);
//         await Loader.UnloadScene(menuSceneKey);
//         await Loader.UnloadScene(pauseMenuSceneKey);

//         if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
//     }

//     // CHARACTER/VEHICLE SELECTION → GAMEPLAY (+ PAUSE)
//     // Called by your LauncherUITKController after it resolves the gameplay key from MenuBridge.PendingLevelIndex
//     public async void PlayLevel(string gameplayKey)
//     {
//         if (string.IsNullOrEmpty(gameplayKey)) gameplayKey = defaultGameplayKey;

//         _currentGameplayKey = gameplayKey;

//         // var gameplay = await Loader.LoadSceneAdditive(gameplayKey, activate: true);
//         // var pause    = await Loader.LoadSceneAdditive(pauseMenuSceneKey, activate: true);
//         // if (pause.HasValue) _pauseScene = pause.Value.Scene;

// var gameplay = await Loader.LoadSceneAdditive(gameplayKey, activate: true);

// // Always reload pause menu fresh (unloaded by QuitToMenu)
// if (!string.IsNullOrEmpty(pauseMenuSceneKey))
// {
//     await Loader.UnloadScene(pauseMenuSceneKey); // ensure it's gone first
//     var pause = await Loader.LoadSceneAdditive(pauseMenuSceneKey, activate: true);
//     if (pause.HasValue && pause.Value.Scene.IsValid())
//         _pauseScene = pause.Value.Scene;
// }





//         _isPaused = false;
//         Time.timeScale = 1f;
//         // If you still use a CanvasGroup pause panel, hide it here; UITK overlay can ignore.

//         // IMPORTANT: clear all menus now that gameplay is running
//         await Loader.UnloadScenes(selectionSceneKey, levelSelectSceneKey, menuSceneKey);

//         if (gameplay.HasValue) SceneManager.SetActiveScene(gameplay.Value.Scene);
//         Loader.DumpLoaded(); // debug
//     }

//     public async void ContinueLast()
//     {
//         PlayLevel(defaultGameplayKey);
//         await Task.CompletedTask;
//     }

//     // ANYWHERE → MAIN MENU
//     public async void QuitToMenu()
//     {
//         Time.timeScale = 1f;
//         _isPaused = false;

//         if (_pauseScene.IsValid() && _pauseScene.isLoaded)
//             await Loader.UnloadScene(pauseMenuSceneKey);

//         if (!string.IsNullOrEmpty(_currentGameplayKey))
//             await Loader.UnloadScene(_currentGameplayKey);

//         await Loader.UnloadScenes(selectionSceneKey, levelSelectSceneKey);


//         await Loader.UnloadScene(pauseMenuSceneKey);  // always clear pause


//         await Loader.SwitchTo(menuSceneKey);
//         _currentGameplayKey = null;

//         await Resources.UnloadUnusedAssets();
//         System.GC.Collect();

//         Loader.DumpLoaded(); // debug
//     }
// }



