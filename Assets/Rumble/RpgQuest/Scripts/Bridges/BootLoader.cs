using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] string mainMenuScene = "MainMenu";
    void Start()
    {
        // Reset statics in case domain reload is off
        MenuBridge.PendingLevelIndex = 0;
      //  MenuBridge.SkipSelectionForContinue = false;

        SceneManager.LoadScene(mainMenuScene);
    }
}
