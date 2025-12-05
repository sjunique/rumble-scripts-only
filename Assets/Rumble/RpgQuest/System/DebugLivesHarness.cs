using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Diagnostic tool to monitor LivesService and GameFlow interaction.
/// Keeps a real-time log of lives, detects 0-life triggers,
/// and can manually simulate life loss/reset during play.
/// </summary>
public class DebugLivesHarness : MonoBehaviour
{
    private LivesService _lives;
    private GameFlow _gameFlow;

    private int _lastLives = -1;
    private bool _hooked = false;
    private string _currentScene = "";

    [SerializeField] bool harnessTriggersQuit = false; // default OFF
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
//        Debug.Log("[LivesHarness] Initialized (will auto-hook LivesService/GameFlow).");
    }

    void Start()
    {
        // Retry hook every second until found
        InvokeRepeating(nameof(FindAndHook), 0.5f, 1f);
    }

    void FindAndHook()
    {
        if (_hooked) return;

        _lives = FindObjectOfType<LivesService>(true);
        _gameFlow = FindObjectOfType<GameFlow>(true);

        if (_lives != null && _gameFlow != null)
        {
            _lives.OnOutOfLives -= HandleOutOfLives;
            _lives.OnOutOfLives += HandleOutOfLives;

            _lastLives = _lives.CurrentLives;
            _hooked = true;
          //  Debug.Log($"[LivesHarness] ✅ Hooked LivesService ({_lastLives} lives) and GameFlow.");
        }
    }

    void Update()
    {
        if (!_hooked || _lives == null) return;

        // Scene tracking
        var scene = SceneManager.GetActiveScene().name;
        if (scene != _currentScene)
        {
            _currentScene = scene;
            Debug.Log($"[LivesHarness] ▶ Scene changed → {_currentScene}");
        }

        // Lives tracking
        if (_lives.CurrentLives != _lastLives)
        {
         //   Debug.Log($"[LivesHarness] ❤️ Lives changed: {_lastLives} → {_lives.CurrentLives} (Scene: {_currentScene})");
            _lastLives = _lives.CurrentLives;
        }

        // Manual test hotkeys
        // if (Input.GetKeyDown(KeyCode.L))
        // {
        //     _lives.LoseLife();
        //     Debug.Log($"[LivesHarness] Manual LoseLife() called.");
        // }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_lives.CurrentLives <= 0)
            {
                Debug.Log("[LivesHarness] Lives are 0; auto-resetting before LoseLife().");
                _lives.ResetLives();
            }

            _lives.LoseLife();
            Debug.Log("[LivesHarness] Manual LoseLife() called.");
        }



        if (Input.GetKeyDown(KeyCode.R))
        {
            _lives.ResetLives();
            Debug.Log($"[LivesHarness] Lives reset to max: {_lives.CurrentLives}");
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log($"[LivesHarness] Manual QuitToMenu() invoked.");
            _gameFlow?.QuitToMenu();
        }
    }
    // Add this flag at top

    private void HandleOutOfLives()
    {
        Debug.LogWarning("[LivesHarness] ⚠ Out of lives detected!");
        if (harnessTriggersQuit)
        {
            Debug.LogWarning("[LivesHarness] Triggering QuitToMenu() (test mode).");
            _gameFlow?.QuitToMenu();
        }
    }

}
