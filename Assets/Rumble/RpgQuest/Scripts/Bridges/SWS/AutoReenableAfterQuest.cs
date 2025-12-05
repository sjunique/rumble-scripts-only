// Assets/Rumble/RpgQuest/Bridges/SWS/AutoReenableAfterQuest.cs
using System.Collections;
using UnityEngine;

public class AutoReenableAfterQuest : MonoBehaviour
{
    [Header("Hook these in the Inspector")]
    public SWSAutopilotController autopilot;
    public Quest questToWatch;

    [Tooltip("Extra delay after quest completes (to let teleport finish).")]
    public float afterQuestDelay = 0.4f;

    [Tooltip("Safety: even if quest never toggles, re-enable after this many seconds.")]
    public float hardTimeout = 8f;

    public void OnAccept()
    {
        StopAllCoroutines();
        StartCoroutine(RestoreRoutine());
    }

    IEnumerator RestoreRoutine()
    {
        float t = 0f;

        // Wait until quest completes or until we time out
        while (t < hardTimeout)
        {
            t += Time.deltaTime;
            if (!questToWatch || questToWatch.isCompleted) break;
            yield return null;
        }

        // Small grace period to let QuestCompletionTeleport finish repositioning
        yield return new WaitForSeconds(afterQuestDelay);

        // Idempotent: safe whether AP is still active or already stopped
        if (autopilot)
        {
            autopilot.StopAutopilot(false);  // calls ExitAutopilot + SetPlayerControlEnabled(true)
            // As an extra safety net:
            autopilot.EnableControlsNow();
        }
    }
}
