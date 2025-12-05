using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using RpgQuest.Utilities; // Add this line
// Kid-mode assist: smoothly bias back to path and add a gentle slowdown off-path
public class KidModeAssist : MonoBehaviour
{

    [Header("References")]
    [Mandatory]
    public WaypointPathVisualizer visualizer;   // assign your visualizer (on the SWS path object)
    [Mandatory]
    public PathFollowerNudge nudge;             // assign the nudge on the player
    [Mandatory]
    public Transform playerRoot;                // usually this.transform

    [Header("Distance bands (m)")]
    public float nearDist = 1.5f;               // inside this, almost zero help
    public float farDist = 6f;                 // beyond this, maximum help

    [Header("Nudge strength")]
    [Range(0, 1)] public float minNudge = 0.25f; // gentle when near
    [Range(0, 1)] public float maxNudge = 0.80f; // firm when far

    [Header("Speed multiplier")]
    public float onPathSpeedMult = 1.00f;       // normal speed on path
    public float offPathSpeedMult = 0.70f;      // slight slowdown off path

    // expose current multiplier so you can apply it in your controller
    public float CurrentSpeedMult { get; private set; } = 1f;

    void Reset() { playerRoot = transform; }

    void Update()
    {
        if (!visualizer || !nudge || !playerRoot || visualizer.PathPoints == null || visualizer.PathPoints.Count < 2) return;

        int seg; float t;
        Vector3 closest = visualizer.ClosestPointOnPath(playerRoot.position, out seg, out t);
        float d = Vector3.Distance(playerRoot.position, closest);

        // k=0 near path, k=1 far away
        float k = Mathf.InverseLerp(nearDist, farDist, d);

        // ramp nudge + speed
        nudge.nudgeStrength = Mathf.Lerp(minNudge, maxNudge, k);
        CurrentSpeedMult = Mathf.Lerp(onPathSpeedMult, offPathSpeedMult, k);
    }
}

