// AutoPilotNavigator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AutoPilotNavigator : MonoBehaviour
{

[Header("Start Behaviour")]
[Tooltip("Before takeoff, fly directly to the very first waypoint (XZ) and start the run there.")]
public bool approachFirstWaypoint = true;

[Tooltip("Teleport to the first waypoint instantly (useful for debug).")]
public bool snapToFirstWaypoint = false;

[Tooltip("Approach speed scaling when homing to the first waypoint (1 = cruise).")]
[Range(0.2f, 1.5f)] public float firstApproachSpeedScale = 1.0f;

[Tooltip("How close (m) to the very first waypoint to consider it 'reached' before starting Takeoff/Cruise.")]
public float firstWpArriveRadius = 3f;
















    [Header("Auto Path Resolve")]
    [Tooltip("Auto-assign paths when the car is spawned or the refs are missing.")]
    public bool autoResolvePaths = true;

    [Tooltip("Re-check periodically if paths become null at runtime (e.g., scene reload).")]
    public bool keepTrackingPaths = true;

    [Tooltip("Seconds between retries when paths are missing.")]
    [Range(0.1f, 5f)] public float resolveRetryInterval = 0.5f;

    [Header("Discovery Filters (optional)")]
    [Tooltip("If set, search only paths with this tag.")]
    public string pathTag = "";             // e.g., "WaypointPath"

    [Tooltip("If set, search only return paths with this tag.")]
    public string returnPathTag = "";       // e.g., "WaypointPath_Return"

    [Tooltip("Optional name contains (case-insensitive). Leave empty to ignore.")]
    public string pathNameContains = "";    // e.g., "Outbound" / "Path_A"

    [Tooltip("Optional name contains for return path. Leave empty to ignore.")]
    public string returnNameContains = "";  // e.g., "Inbound" / "Path_A_Return"

    [Tooltip("Max radius around the car to consider paths. 0 = no limit.")]
    public float searchRadius = 0f;

    Coroutine _resolveCo;






    [Header("Altitude Profile")]
[Tooltip("Follow the path's Y (ups/downs) instead of a fixed cruise height.")]
public bool followWaypointAltitude = true;

[Tooltip("How quickly we blend toward the path altitude (0 = instant).")]
[Range(0f, 1f)] public float altitudeSmooth = 0.15f;

[Tooltip("Optional: limit vertical target change rate (m/s). 0 = unlimited.")]
public float maxAltitudeChangeRate = 8f;

// runtime smoothing state
private float _altTargetSmooth = float.NaN;





















    [Header("Path Smoothing")]
    [Tooltip("How far ahead (meters) to aim along the path at low speeds")]
    public float lookAheadMin = 6f;

    [Tooltip("How far ahead (meters) to aim along the path at high speeds")]
    public float lookAheadMax = 14f;

    [Tooltip("Blend speed -> lookahead. 0 = fixed, 1 = fully scales with speed")]
    [Range(0, 1)] public float lookAheadSpeedFactor = 0.7f;

    [Tooltip("Reduce speed in sharp corners within this preview distance")]
    public float cornerPreview = 10f;

    [Tooltip("Minimum speed scale at a 90° corner (0.5 = half speed)")]
    [Range(0.2f, 1f)] public float cornerMinSpeedScale = 0.6f;





    [Header("References")]
    [Tooltip("Path container with waypoint children")]
    public SimpleWaypointPath path;

    [Tooltip("Optional other path to use for the return trip. If empty, the path will be reversed.")]
    public SimpleWaypointPath returnPath;

    [Tooltip("Your existing hover controller (optional, used to read hover height)")]
    public RpgHoverController hoverController; // integrates hover height & ground snap

    [Header("Flight")]
    [Tooltip("Horizontal cruise speed (m/s)")]
    public float cruiseSpeed = 20f;

    [Tooltip("Autopilot cruise height above origin if the first in-air leg needs it (m). If 0, uses current Y or waypoint Y.")]
    public float cruiseHeight = 15f;

    [Tooltip("How close to a waypoint before advancing (m)")]
    public float waypointArriveRadius = 4f;

    [Tooltip("How tightly we steer to face the target (deg/sec)")]
    public float yawRate = 90f;

    [Tooltip("Bank/roll when turning (deg) - purely visual torque")]
    public float bankDegrees = 15f;

    [Header("Vertical PID")]
    public float altP = 2.0f;
    public float altI = 0.0f;
    public float altD = 1.0f;
    public float maxVerticalSpeed = 10f;

    [Header("Landing")]
    [Tooltip("Slowdown radius near destination (m)")]
    public float slowRadius = 15f;

    [Tooltip("Stop here above destination before ground snap (m)")]
    public float flareHeight = 2.0f;

    [Tooltip("When true, keep hovering at destination; otherwise kill velocity")]
    public bool hoverAtDestination = true;

    [Header("Debug")]
    public bool drawDebug = true;

    private Rigidbody rb;
    private List<Transform> workingPoints = new List<Transform>();
    private int wpIndex = 0;

    private enum Phase { Idle, Takeoff, Cruise, Landing, Arrived }
    private Phase phase = Phase.Idle;

    // PID state
    private float altIntegral;
    private float lastAltError;
    public bool IsAutopilotActive => phase != Phase.Idle;
    public string PhaseLabel => phase.ToString();
    // Cached hover height
    private float hoverHeight => hoverController ? hoverController.HoverHeight : 2.5f;
  void OnEnable()
    {
        if (autoResolvePaths && _resolveCo == null)
            _resolveCo = StartCoroutine(Co_ResolvePathsLoop());
    }

    void OnDisable()
    {
        if (_resolveCo != null) StopCoroutine(_resolveCo);
        _resolveCo = null;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (hoverController == null) hoverController = GetComponent<RpgHoverController>();
        if (autoResolvePaths && (path == null || (returnPath == null && !HasReturnFallback())))
            TryResolvePathsOnce();
    }

    // Public hook if your spawner wants to force a refresh right after instantiation
    public void RefreshPathsFromSpawn()
    {
        TryResolvePathsOnce();
    }
  

    void EnsurePathsResolvedForStart(bool isReturn)
    {
        if (autoResolvePaths && (path == null || (returnPath == null && isReturn && !HasReturnFallback())))
            TryResolvePathsOnce();
    }

    bool HasReturnFallback() => path != null; // your existing reverse fallback works if 'path' exists

    System.Collections.IEnumerator Co_ResolvePathsLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.1f, resolveRetryInterval));
        while (true)
        {
            if (path == null || (returnPath == null && !HasReturnFallback()))
                TryResolvePathsOnce();

            if (!keepTrackingPaths) break;
            yield return wait;
        }
        _resolveCo = null;
    }

void TryResolvePathsOnce()
    {
        // If already assigned, keep them
        if (path == null)
            path = FindBestPath(isReturn:false);

        if (returnPath == null)
            returnPath = FindBestPath(isReturn:true);

#if UNITY_EDITOR
        if (drawDebug)
        {
            if (path != null) Debug.Log($"[AutoPilotNavigator] Resolved forward path: {path.name}", this);
            if (returnPath != null) Debug.Log($"[AutoPilotNavigator] Resolved return path: {returnPath.name}", this);
            if (path == null && returnPath == null) Debug.LogWarning("[AutoPilotNavigator] No paths found.", this);
        }
#endif
    }

    SimpleWaypointPath FindBestPath(bool isReturn)
    {
        // Gather all candidates in scene
        var all = GameObject.FindObjectsOfType<SimpleWaypointPath>(includeInactive: false);
        if (all == null || all.Length == 0) return null;

        string tagFilter = isReturn ? returnPathTag : pathTag;
        string nameFilter = isReturn ? returnNameContains : pathNameContains;

        var q = all.AsEnumerable();

        // Tag filter (if provided)
        if (!string.IsNullOrEmpty(tagFilter))
            q = q.Where(p => p != null && p.CompareTag(tagFilter));

        // Name contains (if provided)
        if (!string.IsNullOrEmpty(nameFilter))
            q = q.Where(p => p != null && p.name.IndexOf(nameFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);

        // Radius filter (if provided)
        if (searchRadius > 0f)
        {
            var pos = transform.position;
            q = q.Where(p => p != null && (p.transform.position - pos).sqrMagnitude <= searchRadius * searchRadius);
        }

        // If filtered set is empty, fall back to ALL paths (so we still pick something)
        var filtered = q.ToList();
        if (filtered.Count == 0) filtered = all.ToList();

        // Score & pick nearest useful path start to the car in XZ (helps on multi-route maps)
        Vector3 car = transform.position;
        SimpleWaypointPath best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var p in filtered)
        {
            if (p == null || p.Points == null || p.Points.Count == 0) continue;

            // choose a representative point (first; for return we prefer last so its orientation matches intent)
            Transform rep = (!isReturn) ? p.Points[0] : p.Points[p.Points.Count - 1];
            if (rep == null) continue;

            float d2 = (new Vector2(rep.position.x - car.x, rep.position.z - car.z)).sqrMagnitude;

            // Higher score = better candidate (closer = higher score)
            float score = -d2;

            // small bonus if name/tag matched (already filtered, but in fallback we may reach here)
            if (!string.IsNullOrEmpty(tagFilter) && p.CompareTag(tagFilter)) score += 10f;
            if (!string.IsNullOrEmpty(nameFilter) && p.name.IndexOf(nameFilter, System.StringComparison.OrdinalIgnoreCase) >= 0) score += 5f;

            if (score > bestScore) { bestScore = score; best = p; }
        }

        return best;
    }

    // ====== hook into your existing public API ======

    public void StartAutoPilotForward()
    {
        EnsurePathsResolvedForStart(isReturn:false);
        BuildWorkingPoints(path, reverse: false);
        BeginSequence();
    }

    public void StartAutoPilotReturn()
    {
        EnsurePathsResolvedForStart(isReturn:true);
        if (returnPath != null)
            BuildWorkingPoints(returnPath, reverse: false);
        else
            BuildWorkingPoints(path, reverse: true);
        BeginSequence();
    }





    static float DistXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
    
    
    
void FixedUpdate()
{
    // Already-idle or no path → do nothing
    if (phase == Phase.Idle || workingPoints.Count == 0) return;

    // NEW: don't let autopilot move the car unless it's actually driveable
    if (hoverController && !hoverController.canDrive)
        return;

    var target = workingPoints[Mathf.Clamp(wpIndex, 0, workingPoints.Count - 1)].position;

    switch (phase)
    {
        case Phase.Takeoff:
            {
                var (pursuit, _) = ComputePursuitTarget();
                DoYawSteer(pursuit);
                DoForwardThrust(pursuit, true);

                if (float.IsNaN(_altTargetSmooth)) _altTargetSmooth = transform.position.y;
                float takeoffAlt = TakeoffTargetAltitude();
                takeoffAlt = SmoothAltitudeTarget(takeoffAlt);
                DoAltitudeControl(takeoffAlt);

                DoAltitudeControl(TakeoffTargetAltitude());
                if (wpIndex >= 0) phase = Phase.Cruise;
                break;
            }

        case Phase.Cruise:
            {
                var (pursuit, cornerScale) = ComputePursuitTarget();
                DoYawSteer(pursuit);

                float saved = cruiseSpeed;
                cruiseSpeed *= cornerScale;
                DoForwardThrust(pursuit, false);
                cruiseSpeed = saved;

                float targetAlt = followWaypointAltitude ? pursuit.y : CruiseTargetAltitude(pursuit);
                targetAlt = SmoothAltitudeTarget(targetAlt);
                DoAltitudeControl(targetAlt);

                if (wpIndex >= workingPoints.Count - 2)
                    phase = Phase.Landing;
                break;
            }

        case Phase.Landing:
            {
                Vector3 dest = workingPoints[workingPoints.Count - 1].position;
                DoYawSteer(dest);
                DoForwardThrust(dest, false, landing: true);
                DoAltitudeControl(LandingTargetAltitude(dest));
                if (new Vector2(transform.position.x - dest.x, transform.position.z - dest.z).sqrMagnitude
                    <= waypointArriveRadius * waypointArriveRadius)
                {
                    phase = Phase.Arrived;
                    if (!hoverAtDestination) rb.linearVelocity = Vector3.zero;
                }
                break;
            }

        case Phase.Arrived:
            DoAltitudeControl(target.y + hoverHeight);
            break;
    }
}

    
    
    
    void FixedUpdateprev()
    {
        if (phase == Phase.Idle || workingPoints.Count == 0) return;

        var target = workingPoints[Mathf.Clamp(wpIndex, 0, workingPoints.Count - 1)].position;

        switch (phase)
        {
            case Phase.Takeoff:
                {
                    var (pursuit, _) = ComputePursuitTarget();
                    DoYawSteer(pursuit);
                    DoForwardThrust(pursuit, true);

                    if (float.IsNaN(_altTargetSmooth)) _altTargetSmooth = transform.position.y;
                    float takeoffAlt = TakeoffTargetAltitude();
                    takeoffAlt = SmoothAltitudeTarget(takeoffAlt);
                    DoAltitudeControl(takeoffAlt);

                    DoAltitudeControl(TakeoffTargetAltitude());
                    // When we’re on/near the first airborne segment, drop into Cruise:
                    if (wpIndex >= 0) phase = Phase.Cruise;
                    break;
                }
            case Phase.Cruise:
                {
                    var (pursuit, cornerScale) = ComputePursuitTarget();
                    DoYawSteer(pursuit);

                    float saved = cruiseSpeed;
                    cruiseSpeed *= cornerScale;
                    DoForwardThrust(pursuit, false);
                    cruiseSpeed = saved;

                    // >>> follow the path Y <<<
                    float targetAlt = followWaypointAltitude ? pursuit.y : CruiseTargetAltitude(pursuit);
                    targetAlt = SmoothAltitudeTarget(targetAlt);
                    DoAltitudeControl(targetAlt);

                    if (wpIndex >= workingPoints.Count - 2)
                        phase = Phase.Landing;
                    break;
                }


            case Phase.Landing:
                {
                    Vector3 dest = workingPoints[workingPoints.Count - 1].position;
                    DoYawSteer(dest);
                    DoForwardThrust(dest, false, landing: true);
                    DoAltitudeControl(LandingTargetAltitude(dest));
                    if (new Vector2(transform.position.x - dest.x, transform.position.z - dest.z).sqrMagnitude <= waypointArriveRadius * waypointArriveRadius)
                    {
                        phase = Phase.Arrived;
                        if (!hoverAtDestination) rb.linearVelocity = Vector3.zero;
                    }
                    break;
                }

            case Phase.Arrived:
                // optional gentle hover / hold
                DoAltitudeControl(target.y + hoverHeight);
                break;
        }
    }




    // Returns pursuit point and a corner speed scale (0..1)
    (Vector3 pursuit, float cornerScale) ComputePursuitTarget()
    {
        // Segment i -> i+1; clamp so we always have a valid segment
        int i = Mathf.Clamp(wpIndex, 0, workingPoints.Count - 2);

        Vector3 a = workingPoints[i].position;
        Vector3 b = workingPoints[i + 1].position;
        Vector3 p = transform.position;

        // --- Project progress in XZ only (Y differences shouldn't skew progress) ---
        Vector2 aXZ = new Vector2(a.x, a.z);
        Vector2 bXZ = new Vector2(b.x, b.z);
        Vector2 pXZ = new Vector2(p.x, p.z);

        Vector2 abXZ = bXZ - aXZ;
        float abLen2 = Mathf.Max(0.0001f, Vector2.Dot(abXZ, abXZ));

        // Raw t along segment in XZ
        float t = Vector2.Dot(pXZ - aXZ, abXZ) / abLen2;

        // Keep a tiny lead so we never aim "behind" and start circling
        const float minLead = 0.02f; // ~2% into segment
        t = Mathf.Clamp01(Mathf.Max(t, minLead));

        // Base pursuit on the 3D line using XZ t (keeps original vertical profile)
        Vector3 pursuit = Vector3.Lerp(a, b, t);

        // --- Dynamic look-ahead distance by speed ---
        float speed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        float look = Mathf.Lerp(lookAheadMin, lookAheadMax,
                      lookAheadSpeedFactor * Mathf.Clamp01(speed / (cruiseSpeed + 0.01f)));

        // March the pursuit forward by 'look' meters, spilling into following segments if needed
        float remain = look;
        int seg = i;
        float segLen = Vector3.Distance(a, b);
        float segPos = segLen * t;

        while (remain > 0f && seg < workingPoints.Count - 1)
        {
            Vector3 sA = workingPoints[seg].position;
            Vector3 sB = workingPoints[seg + 1].position;
            Vector3 sAB = sB - sA;
            float sLen = sAB.magnitude;

            float localStart = (seg == i) ? segPos : 0f;
            float step = Mathf.Min(remain, sLen - localStart);
            float localT = (localStart + step) / Mathf.Max(0.0001f, sLen);
            pursuit = Vector3.Lerp(sA, sB, localT);

            remain -= step;
            if (localT >= 1f) { seg++; segPos = 0f; } else break;
        }

        // --- Corner preview: scale speed down as the upcoming turn sharpens ---
        float cornerScale = 1f;
        if (seg < workingPoints.Count - 2)
        {
            Vector3 d1 = (b - a).normalized;                          // current seg dir
            Vector3 nA = workingPoints[seg].position;
            Vector3 nB = workingPoints[seg + 1].position;
            Vector3 d2 = (nB - nA).normalized;                         // next seg dir

            float distAhead = Vector3.Distance(p, pursuit);
            if (distAhead < cornerPreview)
            {
                float ang = Vector3.Angle(d1, d2);                   // 0..180
                float tAng = Mathf.Clamp01(ang / 90f);                // 0..1 (at 90°)
                cornerScale = Mathf.Lerp(1f, cornerMinSpeedScale, tAng);
            }
        }

        // --- Advance rule: near the next point in XZ, or we've overshot the segment ---
        float arrive = Mathf.Max(waypointArriveRadius, 2f);
        bool nearNextXZ = DistXZ(p, b) <= arrive * 1.2f;

        // Note: compare the raw (unclamped) t for overshoot, but with tolerance
        float tRaw = Vector2.Dot(pXZ - aXZ, abXZ) / abLen2;
        if (tRaw >= 1.001f || nearNextXZ)
        {
            wpIndex = Mathf.Min(wpIndex + 1, workingPoints.Count - 2);
        }

        return (pursuit, cornerScale);
    }







   

    public void StopAutoPilot()
    {
        phase = Phase.Idle;
        workingPoints.Clear();
        wpIndex = 0;
    }

    // ---------- Internals ----------
    void BeginSequence()
    {
        if (workingPoints.Count < 2) { phase = Phase.Idle; return; }
        wpIndex = 0;
        phase = Phase.Takeoff;
        ResetPID();
    }
float SmoothAltitudeTarget(float rawTarget)
{
    // init on first use
    if (float.IsNaN(_altTargetSmooth)) _altTargetSmooth = rawTarget;

    // exponential blend toward target
    float blended = Mathf.Lerp(_altTargetSmooth, rawTarget, 1f - Mathf.Pow(1f - altitudeSmooth, Time.fixedDeltaTime * 60f));

    // optional rate limit (prevents sharp spikes on steep steps)
    if (maxAltitudeChangeRate > 0f)
    {
        float maxStep = maxAltitudeChangeRate * Time.fixedDeltaTime;
        blended = Mathf.MoveTowards(_altTargetSmooth, blended, maxStep);
    }

    _altTargetSmooth = blended;
// inside SmoothAltitudeTarget, before returning:
#if !DISABLE_GROUND_CLAMP
if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
{
    float minAlt = hit.point.y + hoverHeight; // uses your existing hover height
    _altTargetSmooth = Mathf.Max(_altTargetSmooth, minAlt);
}
#endif


    return blended;
}

    void BuildWorkingPoints(SimpleWaypointPath src, bool reverse)
    {
        workingPoints.Clear();
        if (!src) return;
        var pts = src.Points;
        if (reverse)
        {
            for (int i = pts.Count - 1; i >= 0; --i)
                if (pts[i]) workingPoints.Add(pts[i]);
        }
        else
        {
            for (int i = 0; i < pts.Count; ++i)
                if (pts[i]) workingPoints.Add(pts[i]);
        }
    }

    bool ReachedHorizontal(Vector3 target, float radius)
    {
        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(target.x, target.z);
        return Vector2.Distance(a, b) <= radius;
    }

    void AdvanceOrSwitch(Phase nextDefault)
    {
        wpIndex++;
        if (wpIndex >= workingPoints.Count)
        {
            phase = Phase.Arrived;
        }
        else
        {
            // If we just consumed the final en-route node, switch to Landing
            phase = (wpIndex == workingPoints.Count - 1) ? Phase.Landing : nextDefault;
        }
        ResetPID();
    }

    // Heading/yaw to face next waypoint
    void DoYawSteer(Vector3 target)
    {
        var flatTo = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
        if (flatTo.sqrMagnitude < 0.01f) return;

        var desired = Quaternion.LookRotation(flatTo.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, yawRate * Time.fixedDeltaTime);

        // Visual bank (optional)
        var bank = Mathf.Clamp(Vector3.SignedAngle(transform.forward, flatTo.normalized, Vector3.up) / 4f, -bankDegrees, bankDegrees);
        // Smoothly nudge local Z to bank value
        var e = transform.localEulerAngles;
        e.z = Mathf.MoveTowardsAngle(NormalizeAngle(e.z), bank, yawRate * 0.5f * Time.fixedDeltaTime);
        transform.localEulerAngles = e;
    }

    // Forward thrust with simple slowing near final point
    void DoForwardThrust(Vector3 target, bool aggressiveClimb, bool landing = false)
    {
        var to = target - transform.position;
        var flat = new Vector3(to.x, 0, to.z);
        float dist = flat.magnitude;

        float desiredSpeed = cruiseSpeed;

        if (landing && dist < slowRadius)
            desiredSpeed = Mathf.Lerp(4f, cruiseSpeed, Mathf.Clamp01(dist / slowRadius));

        if (aggressiveClimb) desiredSpeed = Mathf.Max(desiredSpeed, cruiseSpeed * 0.75f);

        Vector3 vel = rb.linearVelocity;
        Vector3 flatForward = transform.forward; flatForward.y = 0; flatForward.Normalize();

        Vector3 desiredVel = flatForward * desiredSpeed;
        Vector3 accel = (desiredVel - new Vector3(vel.x, 0, vel.z)) * 2.0f; // simple PD-ish
        rb.AddForce(new Vector3(accel.x, 0, accel.z), ForceMode.Acceleration);
    }

    // Altitude control (PID on vertical axis)
    void DoAltitudeControl(float targetAlt)
    {
        float currAlt = transform.position.y;
        float err = targetAlt - currAlt;

        altIntegral += err * Time.fixedDeltaTime;
        float deriv = (err - lastAltError) / Mathf.Max(Time.fixedDeltaTime, 1e-4f);
        lastAltError = err;

        float cmd = altP * err + altI * altIntegral + altD * deriv;

        // Clamp vertical velocity
        var v = rb.linearVelocity;
        float vy = Mathf.Clamp(v.y + cmd * Time.fixedDeltaTime, -maxVerticalSpeed, maxVerticalSpeed);
        rb.linearVelocity = new Vector3(v.x, vy, v.z);
    }

    float TakeoffTargetAltitude()
    {
        // Rise to either cruiseHeight, or first in-air waypoint's Y, whichever is higher than ground hover
        float baseAlt = transform.position.y;
        float target = (cruiseHeight > 0 ? workingPoints[0].position.y + cruiseHeight : Mathf.Max(baseAlt, workingPoints[Mathf.Min(1, workingPoints.Count - 1)].position.y));
        return Mathf.Max(target, workingPoints[0].position.y + hoverHeight + 1f);
    }

    float CruiseTargetAltitude(Vector3 wp)
    {
        // Track waypoint altitude if it’s in air, otherwise hold a sensible cruise height
        float target = wp.y;
        if (cruiseHeight > 0) target = Mathf.Max(target, workingPoints[0].position.y + cruiseHeight);
        return target;
    }

    float LandingTargetAltitude(Vector3 dest)
    {
        // Descend to a flare height above destination, then your hover system will snap
        return dest.y + Mathf.Max(flareHeight, hoverHeight);
    }

    float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }

    void ResetPID()
    {
        altIntegral = 0f;
        lastAltError = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug || workingPoints.Count == 0) return;
        Gizmos.color = Color.magenta;
        for (int i = 0; i < workingPoints.Count; i++)
        {
            if (i > 0) Gizmos.DrawLine(workingPoints[i - 1].position, workingPoints[i].position);
            Gizmos.DrawWireSphere(workingPoints[i].position, (i == 0 || i == workingPoints.Count - 1) ? 1.0f : 0.6f);
        }
    }
}

