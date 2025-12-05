using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if false
public class MainMenuController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] GameObject menuPanel;          // frame container
    [SerializeField] GameObject mainMenuPanel;      // Play/Continue/Quit/About column
    [SerializeField] GameObject levelSelectPanel;   // grid with Level1..6
    [SerializeField] GameObject aboutPanel;         // About view

    [Header("Focus (first selected)")]
    [SerializeField] Button playBtn;
    [SerializeField] Button level1Btn;
    [SerializeField] Button aboutBackBtn;

    [Header("Buttons")]
    [SerializeField] Button continueBtn;
    [SerializeField] Button aboutBtn;
    [SerializeField] Button quitBtn;

    [Header("Level Select")]
    [SerializeField] Button[] levelButtons;    // assign in order (0..5)

    [SerializeField] CanvasGroup canvasGroup;

    GameFlow _flow;

    void Awake()
    {
        // EventSystem safety
        if (!FindObjectOfType<EventSystem>())
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
           DontDestroyOnLoad(es);
        }
        _flow = FindObjectOfType<GameFlow>(true);
        if (!_flow) Debug.LogError("[MainMenu] GameFlow not found. Ensure Bootstrap has GameFlow.");
    }

    void OnEnable() => ResetMenuState();





    void Start()
    {
        MenuBridge.PendingLevelIndex = 0;
      //  MenuBridge.SkipSelectionForContinue = false;

        ShowMain();

        // Hook main buttons
        playBtn.onClick.RemoveAllListeners();
        continueBtn.onClick.RemoveAllListeners();
        aboutBtn.onClick.RemoveAllListeners();
        quitBtn.onClick.RemoveAllListeners();

        playBtn.onClick.AddListener(ShowLevelSelect);
        continueBtn.onClick.AddListener(OnClickContinue);
        aboutBtn.onClick.AddListener(ShowAbout);
        quitBtn.onClick.AddListener(Application.Quit);

        // Hook level buttons
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => OnPickLevel(idx));
            levelButtons[i].interactable = true; // gate with your own progression if needed
        }
    }

    public void ResetMenuState()
    {
        if (canvasGroup)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
        var es = EventSystem.current;
        if (es) es.enabled = true;

        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Panels
    public void ShowMain()
    {
        menuPanel.SetActive(true);
        mainMenuPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        aboutPanel.SetActive(false);
        UISelectUtil.Focus(playBtn);
    }

    public void ShowLevelSelect()
    {
        menuPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        aboutPanel.SetActive(false);
        UISelectUtil.Focus(level1Btn);
    }

    public void ShowAbout()
    {
        menuPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        aboutPanel.SetActive(true);
        UISelectUtil.Focus(aboutBackBtn);
    }

    // Actions
    void OnPickLevel(int levelIndex)
    {
        MenuBridge.PendingLevelIndex = levelIndex;
        if (_flow) _flow.GoToSelection(); // loads Selection (and unloads Menu)
    }

    void OnClickContinue()
    {
     //   if (_flow) _flow.ContinueLast();
    }
}
#endif