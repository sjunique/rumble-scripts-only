using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuUITKController : MonoBehaviour
{
    [SerializeField] UIDocument uiDoc;

    // Scene keys (Addressables key or scene name)
    [SerializeField] string menuSceneKey = "MainMenu";
    [SerializeField] string levelSelectionSceneKey = "LevelSelection";

    SceneLoadService _loader;

    Button playBtn, continueBtn, aboutBtn, quitBtn, aboutBackBtn;
    VisualElement aboutPanel;

    void Awake()
    {
        if (!uiDoc) uiDoc = GetComponent<UIDocument>();
        _loader = FindObjectOfType<SceneLoadService>(true);

        var root = uiDoc.rootVisualElement;
        playBtn       = root.Q<Button>("PlayBtn");
        continueBtn   = root.Q<Button>("ContinueBtn");
        aboutBtn      = root.Q<Button>("AboutBtn");
        quitBtn       = root.Q<Button>("QuitBtn");
        aboutBackBtn  = root.Q<Button>("AboutBackBtn");
        aboutPanel    = root.Q<VisualElement>("AboutPanel");

        playBtn?.RegisterCallback<ClickEvent>(_ => GoToLevelSelection());
        continueBtn?.RegisterCallback<ClickEvent>(_ => OnClickContinue());
        aboutBtn?.RegisterCallback<ClickEvent>(_ => ShowAbout(true));
        aboutBackBtn?.RegisterCallback<ClickEvent>(_ => ShowAbout(false));
        quitBtn?.RegisterCallback<ClickEvent>(_ => Application.Quit());

        ShowAbout(false);
    }

    void ShowAbout(bool show) => aboutPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

    async void GoToLevelSelection()
    {
        if (!_loader) { Debug.LogError("[MainMenuUITK] SceneLoadService not found"); return; }

        var si = await _loader.LoadSceneAdditive(levelSelectionSceneKey, activate: true);
        await _loader.UnloadScene(menuSceneKey);

        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
    }

    void OnClickContinue()
    {
        // (Optional) Jump straight to last gameplay key if you store it.
        // FindObjectOfType<GameFlow>(true)?.PlayLevel(lastSavedGameplayKey);
    }
}
