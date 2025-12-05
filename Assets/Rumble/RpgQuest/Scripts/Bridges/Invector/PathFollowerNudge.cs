// ==============================
// PathFollowerNudge.cs
// Softly biases the player toward the visual path, or runs autopilot along it
// ==============================
using UnityEngine;
using RpgQuest.Utilities; // Add this line
[AddComponentMenu("Path/Path Follower Nudge")]
public class PathFollowerNudge : MonoBehaviour
{



    [Header("References")]
      [Mandatory] 
    public WaypointPathVisualizer path;
      [Mandatory] 
    public Transform playerRoot;
    [Tooltip("Usually the main camera. If null, falls back to playerRoot or Camera.main.")]
  [Mandatory]     public Transform referenceFrame;

    // --- Autopilot start mode ---
    public enum APStartMode { FromPathStart, FromClosestParam }
    [Header("Autopilot Start")]
    public APStartMode apStartMode = APStartMode.FromPathStart;

    // --- Waypoint-by-waypoint AP (robust) ---
    [Header("Autopilot (Waypoint Mode)")]
    public bool autopilotUseWaypoints = true;   // <— turn this ON
    public float waypointReachRadius = 1.3f;   // how close to a node counts as “reached”
    public float apCenterGain = 0.20f;  // small pull back to segment center

    // --- End handling ---
    [Header("Autopilot End")]
    public bool stopAtEnd = true;
    public float endStopRadius = 1.4f;
    public System.Action OnAutopilotFinished;

    // progress state for waypoint mode
    int _apIndex = 0;   // target node index
    bool _apInit = false;

    static float DistXZ(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
    [Header("Route Control")]
    public bool autoDisableOnFinish = true;




    int FindNearestIndex(Vector3 pos)
    {
        if (path == null || path.PathPoints == null || path.PathPoints.Count == 0) return 0;
        int best = 0; float bestD = float.MaxValue;
        for (int i = 0; i < path.PathPoints.Count; i++)
        {
            float d = DistXZ(pos, path.PathPoints[i]);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }
    public void ResetAutopilotProgress() { _apInit = false; }
    public void BindPath(WaypointPathVisualizer newPath) { path = newPath; _apInit = false; }
    public void SetRoute(
        WaypointPathVisualizer newPath,
        bool enableNudge = true,
        bool startAutopilot = false,
        bool fromStart = true)
    {
        // make sure the visualizer is active before we read points
        if (newPath && !newPath.gameObject.activeInHierarchy)
            newPath.gameObject.SetActive(true);

        path = newPath;
        nudgeEnabled = enableNudge;
        autopilot = startAutopilot;

        // Reset AP so Update() initializes deterministically for this route
        _apIndex = (fromStart || path == null)
            ? 0
            : FindNearestIndex(playerRoot ? playerRoot.position : Vector3.zero);
        _apInit = false;  // <-- changed from true to false

#if UNITY_EDITOR
    Debug.Log($"[PathFollowerNudge] SetRoute -> {(path ? path.name : "<null>")}, " +
              $"nudge={nudgeEnabled}, ap={autopilot}, start={(fromStart?"start":"nearest")}, index={_apIndex}");
#endif
    }

    public void ClearRoute(bool disableNudge = true)
    {
        autopilot = false;
        _apInit = false;
        _apIndex = 0;
        NudgeInput02 = Vector2.zero;
        AutoPilotInput02 = Vector2.zero;
        if (disableNudge) nudgeEnabled = false;
        path = null;
    }


    /// <summary>
    /// /
    /// </summary>









    [Header("Enable/Disable")]
    public bool nudgeEnabled = true;
    [Tooltip("If true, nudge is 0 when player isn't pressing input. Call SetUserInput02 from your input bridge.")]
    public bool nudgeOnlyWhenPlayerMoves = true;


    [Header("Guidance")]
    [Range(0f, 1f)] public float nudgeStrength = 0.3f;
    public float lookAhead = 3f;
    [Range(0f, 1f)] public float forwardBias = 0.7f;
    public float recenterGain = 0.6f;

    [Header("Autopilot")]
    public bool autopilot = false;
    public KeyCode toggleAutopilotKey = KeyCode.T;
    [Range(0f, 1f)] public float autopilotInputMagnitude = 0.9f;

    [Header("Smoothing / Debug")]
    public float inputSmoothing = 10f;
    public bool drawDebug = true;

    // Outputs (x = strafe, y = forward) — camera-relative
    public Vector2 NudgeInput02 { get; private set; }
    public Vector2 AutoPilotInput02 { get; private set; }

    Vector2 _smoothed;         // smoothed desired input (camera-relative)
    Transform _ref;            // cached reference frame
    Vector2 _lastUserInput02;  // optional: set from your input bridge

    // API 
    public void EnableNudge(bool on) { nudgeEnabled = on; }
    public void SetUserInput02(Vector2 userInput02) { _lastUserInput02 = userInput02; }

    void Awake()
    {
        _ref = referenceFrame ? referenceFrame : (Camera.main ? Camera.main.transform : playerRoot);
    }
    void Update()
    {
        if (Input.GetKeyDown(toggleAutopilotKey)) autopilot = !autopilot;

        NudgeInput02 = Vector2.zero;
        AutoPilotInput02 = Vector2.zero;

        if (!path || !playerRoot || path.PathPoints == null || path.PathPoints.Count < 2){
//            Debug.LogWarning("[PathFollowerNudge] No path or playerRoot set, or path is invalid.", this);
            return;      
        }
          

        if (!_ref) _ref = referenceFrame ? referenceFrame : (Camera.main ? Camera.main.transform : playerRoot);

        // --- End stop: if player is at the last node, stop AP/nudge ---
        // --- END STOP: if we're at the final node, stop AP & optionally disable nudge ---
        if (stopAtEnd && path && path.PathPoints != null && path.PathPoints.Count > 1)
        {
            Vector3 endPos = path.PathPoints[path.PathPoints.Count - 1];
            Vector3 a = playerRoot.position; a.y = 0; endPos.y = 0;
            if (Vector3.Distance(a, endPos) <= endStopRadius)
            {
                OnAutopilotFinished?.Invoke();
                if (autoDisableOnFinish) ClearRoute(true);
                else { autopilot = false; NudgeInput02 = Vector2.zero; AutoPilotInput02 = Vector2.zero; }
                return;
            }
        }

        Vector3 desiredWorld;

        if (autopilot && autopilotUseWaypoints)
        {
            // -------- Waypoint-by-waypoint autopilot --------
            if (!_apInit)
            {
                _apIndex = (apStartMode == APStartMode.FromPathStart) ? 0 : FindNearestIndex(playerRoot.position);
                _apInit = true;
            }

            // Clamp index safety
            _apIndex = Mathf.Clamp(_apIndex, 0, path.PathPoints.Count - 1);

            // If we already finished, stop
            if (_apIndex >= path.PathPoints.Count - 1 && stopAtEnd &&
                DistXZ(playerRoot.position, path.PathPoints[_apIndex]) <= endStopRadius)
            {
                autopilot = false;
                NudgeInput02 = Vector2.zero;
                AutoPilotInput02 = Vector2.zero;
                OnAutopilotFinished?.Invoke();
                return;
            }

            // Acquire current target node (advance when reached)
            Vector3 target = path.PathPoints[_apIndex];
            if (DistXZ(playerRoot.position, target) <= waypointReachRadius)
            {
                _apIndex++;
                if (_apIndex >= path.PathPoints.Count)
                {
                    autopilot = false;
                    NudgeInput02 = Vector2.zero;
                    AutoPilotInput02 = Vector2.zero;
                    OnAutopilotFinished?.Invoke();
                    return;
                }
                target = path.PathPoints[_apIndex];
            }

            // Direction to the node
            Vector3 toNode = target - playerRoot.position; toNode.y = 0f;

            // Small centerline pull toward the current segment (prev->target) to keep us on the line
            Vector3 prev = path.PathPoints[Mathf.Max(0, _apIndex - 1)];
            Vector3 seg = (target - prev); seg.y = 0f;
            Vector3 segDir = seg.sqrMagnitude > 1e-6f ? seg.normalized : Vector3.forward;
            // project player onto the segment for gentle recenter
            Vector3 pRel = playerRoot.position - prev; pRel.y = 0f;
            float t = Mathf.Clamp01(Vector3.Dot(pRel, segDir) / Mathf.Max(0.0001f, seg.magnitude));
            Vector3 onSeg = prev + segDir * (t * seg.magnitude);
            Vector3 toCenter = onSeg - playerRoot.position; toCenter.y = 0f;

            desiredWorld = toNode.normalized + toCenter * Mathf.Clamp01(apCenterGain);
            if (desiredWorld.sqrMagnitude < 1e-6f) desiredWorld = toNode; // fallback
            if (desiredWorld.sqrMagnitude < 1e-6f) return;
        }
        else
        {
            // -------- Nudge mode: tangent + recenter (keeps to centerline) --------
            int segId; float tt;
            Vector3 closest = path.ClosestPointOnPath(playerRoot.position, out segId, out tt);
            Vector3 ahead = path.MarchForward(segId, tt, 0.6f);

            Vector3 tangent = (ahead - closest); tangent.y = 0f;
            if (tangent.sqrMagnitude < 1e-6f) tangent = (ahead - playerRoot.position);

            Vector3 toCenter = closest - playerRoot.position; toCenter.y = 0f;

            desiredWorld = tangent.normalized + toCenter * Mathf.Clamp01(recenterGain);
            if (desiredWorld.sqrMagnitude < 1e-6f) return;
        }

        // --- Convert world dir to camera-relative input (x=strife, y=forward) ---
        Vector3 camFwd = _ref.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = _ref.right; camRight.y = 0f; camRight.Normalize();

        Vector2 desired02 = new Vector2(
            Vector3.Dot(camRight, desiredWorld.normalized),
            Vector3.Dot(camFwd, desiredWorld.normalized)
        );

        float k = 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime);
        _smoothed = Vector2.Lerp(_smoothed, desired02, k);

        if (autopilot)
        {
            AutoPilotInput02 = _smoothed.normalized * Mathf.Clamp01(autopilotInputMagnitude);
            NudgeInput02 = Vector2.zero;
        }
        else
        {
            NudgeInput02 = _smoothed * Mathf.Clamp01(nudgeStrength);
            AutoPilotInput02 = Vector2.zero;
        }

        if (drawDebug)
            Debug.DrawRay(playerRoot.position + Vector3.up * 0.05f, desiredWorld.normalized, Color.magenta);
    }


}
