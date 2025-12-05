// Assets/Rumble/RpgQuest/Bridges/SWS/SWSAutopilotController.cs
using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SWS; // Simple Waypoint System

public partial class SWSAutopilotController : MonoBehaviour
{

    // ...

         Coroutine ghostRoutine;
    [SerializeField] bool stopAtEnd = true;

    // Add this field near your other serialized fields:
    [SerializeField] bool debugKeepGhost = false;

    public enum Mode { SplineOnPlayer, NavGhostFollower }
    // Add near the other fields:
    [SerializeField] AutopilotPhysicsAdapter physicsAdapter;
    [Header("Who to drive")]
    [SerializeField] Transform player;

    [Header("How to drive")]
    [SerializeField] Mode mode = Mode.SplineOnPlayer;
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] bool rotateToPath = true;
   

    [Header("Follower (ghost mode)")]
    [SerializeField] float followPosLerp = 12f;
    [SerializeField] float followRotLerp = 10f;

    // runtime
    splineMove splineDriver;          // SWS direct driver
    GameObject ghostAgent;            // ghost agent root (navmesh)
    navMove navDriver;                // SWS nav driver
                                      //  TransformFollower follower;       // smooth follow component
    RigidbodyFollowerSmooth follower;
    // Invector (optional)
    Component invectorController;     // vThirdPersonController
    Component invectorInput;          // vThirdPersonInput

    void Awake()
    {

        if (!player) player = transform;
        if (!physicsAdapter && player) physicsAdapter = player.GetComponent<AutopilotPhysicsAdapter>();
        // (rest of your Awake...)

        if (!player) player = transform;
        // cache Invector bits if present (keeps code compile-safe when missing)
        invectorController = player ? player.GetComponentInChildren(Type.GetType("Invector.vCharacterController.vThirdPersonController")) : null;
        invectorInput = player ? player.GetComponentInChildren(Type.GetType("Invector.vCharacterController.vThirdPersonInput")) : null;
    }

    /// Start autopilot along an SWS PathManager (assign your path container here)
    public void StartAutopilot(PathManager path)
    {
        if (!player || !path)
        {
            Debug.LogWarning("[SWS/AP] Missing player or PathManager.");
            return;
        }

        SetPlayerControlEnabled(false);

        if (mode == Mode.SplineOnPlayer)
        {
            // Drive the player directly with splineMove
            splineDriver = player.GetComponent<splineMove>();
            if (!splineDriver) splineDriver = player.gameObject.AddComponent<splineMove>();

            splineDriver.pathContainer = path;
            splineDriver.speed = moveSpeed;
            splineDriver.loopType = splineMove.LoopType.none;
            splineDriver.lookAhead = rotateToPath ? 0.02f : 0f;

            // Some SWS versions expose enum "timeValue" (time/speed). Set to "speed" if present.
            TrySetEnumByName(splineDriver, "timeValue", "speed");
            // Set moveToPath=false if available (start from current pos)
            TrySetBoolByName(splineDriver, "moveToPath", false);
            splineDriver.moveToPath = false;
            physicsAdapter?.EnterAutopilot();
            splineDriver.StartMove();

            if (stopAtEnd) StartCoroutine(WaitEndThenStop_Player());
        }
        else // NavGhostFollower
        {

            // (keep your other fields: player, moveSpeed, followPosLerp, followRotLerp, etc.)

   

            // --- inside StartAutopilot(PathManager path) -> NavGhostFollower branch ---
            if (ghostAgent) Destroy(ghostAgent);

            Debug.Log("[SWS/AP] NAV: creating ghost...");
            ghostAgent = new GameObject("AP_GhostAgent");
            ghostAgent.transform.SetPositionAndRotation(player.position, player.rotation);

            var agent = ghostAgent.AddComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 12f;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.1f;

            // Ensure the agent starts ON the NavMesh (warp if needed)
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(ghostAgent.transform.position, out var hit, 5f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                else if (path && path.waypoints != null && path.waypoints.Length > 0 &&
                        NavMesh.SamplePosition(path.waypoints[0].position, out hit, 10f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                else
                    Debug.LogWarning("[SWS/AP] NAV: Could not find NavMesh start position.");
            }

            Debug.Log($"[SWS/AP] NAV: path set → {(path ? path.name : "NULL")}, waypoints={path?.waypoints?.Length ?? 0}");

            // Smooth follower on the PLAYER (not the ghost)
            follower = player.GetComponent<RigidbodyFollowerSmooth>();
            if (!follower) follower = player.gameObject.AddComponent<RigidbodyFollowerSmooth>();
            follower.target = ghostAgent.transform;
            follower.posLerp = followPosLerp;
            follower.rotLerp = followRotLerp;
            Debug.Log("[SWS/AP] NAV: RigidbodyFollowerSmooth set.");

            // Drive the agent along SWS path WITHOUT navMove
            if (ghostRoutine != null) StopCoroutine(ghostRoutine);
            ghostRoutine = StartCoroutine(DriveAgentAlongPath(agent, path));













        }
    }
    IEnumerator DriveAgentAlongPath(NavMeshAgent agent, SWS.PathManager path)
    {
        if (!agent || !path || path.waypoints == null || path.waypoints.Length == 0)
        {
            Debug.LogWarning("[SWS/AP] NAV: Missing agent/path/waypoints.");
            yield break;
        }

        // small settle to avoid 1st-frame hiccups
        yield return null;

        for (int i = 0; i < path.waypoints.Length; i++)
        {
            var raw = path.waypoints[i].position;
            Vector3 dest = raw;

            // snap each target to the NavMesh so SetDestination never fails
            if (NavMesh.SamplePosition(raw, out var hit, 3f, NavMesh.AllAreas))
                dest = hit.position;

            agent.isStopped = false;
            agent.SetDestination(dest);

            // wait until we reach it
            while (agent.pathPending) yield return null;
            // tolerance: stoppingDistance plus a hair
            while (agent.remainingDistance > agent.stoppingDistance + 0.05f)
                yield return null;

            yield return null; // one extra frame for safety
        }

        if (stopAtEnd)
            StopAutopilot(true); // this will also clean the ghost & re-enable WASD
    }


    public void StopAutopilot(bool snapToEnd = false)
    {
        // Debug.Log($"[SWS/AP] Start (mode={mode}) path={(path ? path.name : "NULL")}");
        if (mode == Mode.SplineOnPlayer && splineDriver)
        {
            if (snapToEnd) TryCall(splineDriver, "SetPathPosition", 1f);
            TryCall(splineDriver, "Stop");
            splineDriver.enabled = false;
            splineDriver = null;
        }
        else
        {
            if (ghostRoutine != null) { StopCoroutine(ghostRoutine); ghostRoutine = null; }

            if (follower) { Destroy(follower); follower = null; }

            if (ghostAgent)
            {
                if (snapToEnd && player && ghostAgent) player.SetPositionAndRotation(ghostAgent.transform.position, ghostAgent.transform.rotation);
                Destroy(ghostAgent);
                ghostAgent = null;
            }


        }

        // Always restore physics + controls
        physicsAdapter?.ExitAutopilot();     // <-- puts rb.isKinematic back to your normal default
        SetPlayerControlEnabled(true);

        Debug.Log("[SWS/AP] Autopilot stopped.");
    }


    IEnumerator WaitEndThenStop_Player()
    {
        // give SWS a frame to actually start its tween
        yield return null;

        // Prefer pathPosition (0..1). If not present, fall back to distance to last point.
        float timeout = 120f; // safety (seconds)
        float t = 0f;

        // Try to read pathPosition
        var posProp = splineDriver.GetType().GetProperty(
            "pathPosition",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
        );

        // Try to read PathManager from the driver (for fallback distance check)
        var pathField = splineDriver.GetType().GetField("pathContainer",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var pm = pathField?.GetValue(splineDriver) as SWS.PathManager;

        Vector3 lastPoint = Vector3.zero;
        if (pm && pm.waypoints != null && pm.waypoints.Length > 0)
            lastPoint = pm.waypoints[pm.waypoints.Length - 1].position;

        while (splineDriver && t < timeout)
        {
            t += Time.deltaTime;

            bool finished = false;

            if (posProp != null)
            {
                var pos = (float)posProp.GetValue(splineDriver);
                finished = pos >= 0.99f;
            }
            else if (pm)
            {
                finished = (Vector3.Distance(transform.position, lastPoint) < 0.5f);
            }

            if (finished) break;
            yield return null;
        }

        StopAutopilot(true);
    }

    IEnumerator WaitEndThenStop_Ghost(NavMeshAgent agent)
    {
        while (agent && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance || agent.velocity.sqrMagnitude > 0.01f))
            yield return null;
        StopAutopilot(true);
    }



    // ---------- reflection helpers (keep this minimal & safe) ----------
    bool TrySetEnumByName(object obj, string name, string enumLiteral)
    {
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType.IsEnum)
        {
            try { var val = Enum.Parse(p.PropertyType, enumLiteral, true); p.SetValue(obj, val); return true; }
            catch { }
        }
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType.IsEnum)
        {
            try { var val = Enum.Parse(f.FieldType, enumLiteral, true); f.SetValue(obj, val); return true; }
            catch { }
        }
        return false;
    }

    bool TrySetBoolByName(object obj, string name, bool value)
    {
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool)) { p.SetValue(obj, value); return true; }
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) { f.SetValue(obj, value); return true; }
        return false;
    }

    bool GetBool(object obj, string name)
    {
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(obj);
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(obj);
        return false;
    }

    void TryCall(object obj, string methodName, params object[] args)
    {
        var mi = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.ConvertAll(args, a => a?.GetType() ?? typeof(object)), null);
        mi?.Invoke(obj, args);
    }

    // Add inside SWSAutopilotController (anywhere in the class)
    Component FindCompByTypeName(Transform root, string typeName)
    {
        if (!root) return null;
        var all = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in all)
        {
            var t = c ? c.GetType() : null;
            if (t == null) continue;
            if (t.Name == typeName || t.FullName.EndsWith("." + typeName))
                return c;
        }
        return null;
    }
    public void EnableControlsNow()
    {
        physicsAdapter?.ExitAutopilot();   // puts RB kinematic/gravity back to your defaults
        SetPlayerControlEnabled(true);     // re-enables vThirdPersonInput reliably
        Debug.Log("[SWS/AP] Controls FORCE-ENABLED.");
    }

    void SetPlayerControlEnabled(bool enabled)
    {
        // Find (or re-find) the components by name, robust to assembly differences.
        var invectorInput = FindCompByTypeName(player, "vThirdPersonInput") as Behaviour;
        var invectorCtrl = FindCompByTypeName(player, "vThirdPersonController");

        if (invectorInput) invectorInput.enabled = enabled;

        // unlock/lock movement if the property exists
        if (invectorCtrl != null)
        {
            var prop = invectorCtrl.GetType().GetProperty("lockMovement",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool))
                prop.SetValue(invectorCtrl, !enabled, null);
        }

        // If you use a CharacterController in normal mode, re-enable it here
        var cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = enabled;

        Debug.Log($"[SWS/AP] Controls {(enabled ? "ENABLED" : "DISABLED")}. input={(invectorInput ? invectorInput.enabled.ToString() : "null")}");
    }





}

//
