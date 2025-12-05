// Assets/Rumble/RpgQuest/Bridges/Waypoints/SWSAcceptHook.cs
using UnityEngine;

public class SWSAcceptHook : MonoBehaviour
{
    public Collider stageTrigger;           // the trigger that has SWSStageTrigger
    public MonoBehaviour routeController;   // optional: your QuestRouteController (for BeginStage(0))

    public void OnAccept()
    {
        if (stageTrigger) stageTrigger.enabled = true;

        // Optional: if you want to mark Stage 0 active immediately
        var mi = routeController ? routeController.GetType().GetMethod("BeginStage") : null;
        if (mi != null) mi.Invoke(routeController, new object[] { 0 });

        Debug.Log("[SWS/Accept] Trigger armed; stage 0 begun.");
    }
}

