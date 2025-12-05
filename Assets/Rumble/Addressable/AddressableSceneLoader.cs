


using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;



public static class AddressableSceneLoader
{
    static AsyncOperationHandle<SceneInstance>? _currentGameplay;
    static AsyncOperationHandle<SceneInstance>? _currentPauseUI;

    public static GameAddressableConfig Config { get; private set; }

    /// <summary>Call once at boot (or first use). Provide your config asset.</summary>
    public static async Task InitializeAsync(GameAddressableConfig config)
    {
        if (Config == null) Config = config;
        await Addressables.InitializeAsync().Task;
    }

    /// <summary>Load a gameplay scene (Single) and then additively load the PauseUI scene.</summary>
    public static async Task LoadGameplayWithPauseUIAsync(string gameplayKey, string pauseUIKey)
    {
        // Load gameplay scene as SINGLE (replaces whatever was running)
        var handleGame = Addressables.LoadSceneAsync(gameplayKey, LoadSceneMode.Single, activateOnLoad: true);
        _currentGameplay = handleGame;
        await handleGame.Task;

   if (!_currentPauseUI.HasValue || !_currentPauseUI.Value.IsValid())
    {
        var handlePause = Addressables.LoadSceneAsync(pauseUIKey, LoadSceneMode.Additive, true);
        _currentPauseUI = handlePause;
        await handlePause.Task;
    }


        // // Then load Pause UI additively on top
        // var handlePause = Addressables.LoadSceneAsync(pauseUIKey, LoadSceneMode.Additive, activateOnLoad: true);
        // _currentPauseUI = handlePause;
        // await handlePause.Task;






    }












    /// <summary>Return to main menu (unload PauseUI if present, then load menu Single).</summary>
    public static async Task LoadMainMenuAsyncpppp(string mainMenuKey, bool unloadPauseUI = true)
    {
        if (unloadPauseUI && _currentPauseUI.HasValue && _currentPauseUI.Value.IsValid())
        {
            await Addressables.UnloadSceneAsync(_currentPauseUI.Value, autoReleaseHandle: true).Task;
            _currentPauseUI = null;
        }

        // Load Main Menu as SINGLE
        var handleMenu = Addressables.LoadSceneAsync(mainMenuKey, LoadSceneMode.Single, activateOnLoad: true);
        await handleMenu.Task;

        // Clear gameplay handle (menu replaced it)
        _currentGameplay = null;
    }

public static async System.Threading.Tasks.Task LoadMainMenuAsync(string mainMenuKey, bool unloadPauseUI = true)
{
    // Always reset time & audio here too (double safety)
    Time.timeScale = 1f;
    AudioListener.pause = false;

    if (unloadPauseUI && _currentPauseUI.HasValue && _currentPauseUI.Value.IsValid())
    {
        await Addressables.UnloadSceneAsync(_currentPauseUI.Value, autoReleaseHandle: true).Task;
        _currentPauseUI = null;
    }

    var handleMenu = Addressables.LoadSceneAsync(mainMenuKey, LoadSceneMode.Single, activateOnLoad: true);
    await handleMenu.Task;

    _currentGameplay = null;
}










    /// <summary>Reload the current gameplay scene (keep PauseUI layered after reload).</summary>
    public static async Task RestartLevelAsync(string pauseUIKey)
    {
        string current = SceneManager.GetActiveScene().name;
        // Reload gameplay scene Single
        var handleGame = Addressables.LoadSceneAsync(current, LoadSceneMode.Single, true);
        _currentGameplay = handleGame;
        await handleGame.Task;

        // Re-layer PauseUI
        var handlePause = Addressables.LoadSceneAsync(pauseUIKey, LoadSceneMode.Additive, true);
        _currentPauseUI = handlePause;
        await handlePause.Task;
    }
}
