using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LevelSelectionUITKController : MonoBehaviour
{
    [SerializeField] UIDocument uiDoc;

    [Header("Scene Keys")]
    [SerializeField] string levelSelectionSceneKey   = "LevelSelection";
    [SerializeField] string charCarSelectionSceneKey = "SelectionScreen"; // matches your inspector

    SceneLoadService _loader;

    Button nextBtn, backBtn;
    Button[] levelButtons;
    int _selected = -1; // nothing selected at start

    void Awake()
    {
        if (!uiDoc) uiDoc = GetComponent<UIDocument>();
        _loader = FindObjectOfType<SceneLoadService>(true);

        var root  = uiDoc.rootVisualElement;
        nextBtn   = root.Q<Button>("NextBtn");
        backBtn   = root.Q<Button>("BackBtn");

        levelButtons = new[] {
            root.Q<Button>("Level1"),
            root.Q<Button>("Level2"),
            root.Q<Button>("Level3"),
            root.Q<Button>("Level4"),
            root.Q<Button>("Level5"),
            root.Q<Button>("Level6"),
        };

        // disable Next until we click a level
        nextBtn?.SetEnabled(false);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            levelButtons[i]?.RegisterCallback<ClickEvent>(_ =>
            {
                _selected = idx;
                MenuBridge.PendingLevelIndex = idx;

                // optional visual feedback
                Highlight(idx);

                // now we can proceed
                nextBtn?.SetEnabled(true);
            });
        }

        nextBtn?.RegisterCallback<ClickEvent>(_ => GoToCharCarSelection());
        backBtn?.RegisterCallback<ClickEvent>(_ => BackToMain());
    }

    void Highlight(int idx)
    {
        // simple selection feedback (optional)
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            if (i == idx) levelButtons[i].AddToClassList("selected");
            else          levelButtons[i].RemoveFromClassList("selected");
        }
    }

    async void GoToCharCarSelection()
    {
        if (_selected < 0) return; // safety

        if (!_loader) { Debug.LogError("[LevelSelectionUITK] SceneLoadService not found"); return; }
        var si = await _loader.LoadSceneAdditive(charCarSelectionSceneKey, activate: true);
        await _loader.UnloadScene(levelSelectionSceneKey);
        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
    }

    async void BackToMain()
    {
        if (!_loader) { Debug.LogError("[LevelSelectionUITK] SceneLoadService not found"); return; }
        var si = await _loader.LoadSceneAdditive("MainMenu", activate: true);
        await _loader.UnloadScene(levelSelectionSceneKey);
        if (si.HasValue) SceneManager.SetActiveScene(si.Value.Scene);
    }
}
