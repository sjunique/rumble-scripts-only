// Assets/Rumble/RpgQuest/Bridges/SWS/SWSStageTrigger.cs
using UnityEngine;
using SWS;

public class SWSStageTrigger : MonoBehaviour
{
    [Header("Stage objects")]
    public GameObject routeRoot;
    public PathManager swsPath;           // <-- PathManager here
    public GameObject collectiblesRoot;

    [Header("Driver")]
    public SWSAutopilotController autopilot;
    public string playerTag = "Player";
    public bool consumeOnEnter = true;
    bool consumed;

    public void ArmTrigger(bool armed)
    {
        var col = GetComponent<Collider>();
        if (col) col.enabled = armed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed && consumeOnEnter) return;
        if (!other.CompareTag(playerTag)) return;

        if (routeRoot) routeRoot.SetActive(true);
        if (collectiblesRoot) collectiblesRoot.SetActive(true);

        if (autopilot && swsPath) autopilot.StartAutopilot(swsPath);
        else Debug.LogWarning("[SWS/Trigger] Missing autopilot or PathManager.");

        consumed = true;
    }
}

