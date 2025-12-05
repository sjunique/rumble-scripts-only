using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameResetUtility
{
    public static void ResetAllProgress()
    {
        Debug.Log("[Reset] Clearing PlayerPrefs + SaveService files + quest state.");

        // 1. Clear PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. Clear SaveService files
        SaveService.DeleteProfile();
        SaveService.DeleteAllSceneCheckpoints();

        // 3. Reset quest state in current session (optional but nice)
        var qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.ResetAllQuests();
        }
    }
}
