// Assets/Rumble/RpgQuest/Bridges/SWS/SWSStraightStart.cs
using UnityEngine;
using SWS;

public class SWSStraightStart : MonoBehaviour
{
    [Header("Stage bits")]
    public GameObject routeRoot;
    public GameObject collectiblesRoot;
    public PathManager path;

    [Header("Driver")]
    public SWSAutopilotController autopilot; // the one you already added

    public void OnAccept()
    {
        if (routeRoot) routeRoot.SetActive(true);
        if (collectiblesRoot) collectiblesRoot.SetActive(true);

        if (autopilot && path)
        {
            autopilot.StartAutopilot(path);
            Debug.Log($"[SWS] Accepted → route '{path.name}' shown + autopilot started.");
        }
        else
        {
            Debug.LogWarning("[SWS] Missing autopilot or PathManager on SWSStraightStart.");
        }
    }
}
