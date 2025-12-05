using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
 

public class WaypointCarTeleporter : MonoBehaviour
{
    public enum SnapMode
    {
        NearestOnPath,
        ExplicitSingle,
        ExplicitListByIndex
    }

    [Header("Path (used for Nearest & tangent)")]
    public SimpleWaypointPath path; // must expose List<Transform> Points

    [Header("Snap Source")]
    public SnapMode snapMode = SnapMode.NearestOnPath;
    public Transform explicitWaypoint;               // for ExplicitSingle
    public List<Transform> explicitWaypoints = new(); // for ExplicitListByIndex
    public int explicitIndex = 0;                    // index into explicitWaypoints

    [Header("Car")]
    public GameObject car;            // your car instance
    public bool autoFindCar = true;   // find by tag/name if null
    public string carTag = "Vehicle"; // or "Car"
    public string[] fallbackNames = new[] { "HoverCar", "Car", "Vehicle", "PlayerCar" };

    [Header("Placement")]
    public bool alignRotationToPathTangent = true;
    public bool useWaypointRotationIfNoTangent = true;
    [Tooltip("Move a tiny step forward after snapping (meters), to avoid being exactly on the marker.")]
    [Range(0f, 3f)] public float postSnapForwardNudge = 0.5f;

    [Header("Autopilot Handoff")]
    public bool startAutopilotAfterTeleport = true;
    public bool startReturn = false; // call Return path instead of Forward

    [Header("Hotkeys (optional)")]
    public bool enableHotkeys = true;
    public KeyCode snapKey = KeyCode.F8;    // main snap
    public KeyCode snapExplicitIndexKey = KeyCode.F7; // snap to explicitIndex (when using ExplicitListByIndex)

    void Update()
    {
        if (!enableHotkeys) return;

        if (Input.GetKeyDown(snapKey))
        {
            TeleportByMode();
        }
        if (snapMode == SnapMode.ExplicitListByIndex && Input.GetKeyDown(snapExplicitIndexKey))
        {
            TeleportToExplicitIndex(explicitIndex);
        }
    }

    // ---------- Public API ----------
    public bool TeleportByMode()
    {
        if (!EnsureCar()) return false;

        Transform wp = ResolveWaypointByMode();
        if (wp == null)
        {
            Debug.LogWarning("[WaypointCarTeleporter] No waypoint resolved.");
            return false;
        }
        return TeleportCarToWaypoint(wp);
    }

    public bool TeleportToExplicit(Transform target)
    {
        if (target == null) return false;
        snapMode = SnapMode.ExplicitSingle;
        explicitWaypoint = target;
        return TeleportCarToWaypoint(target);
    }

    public bool TeleportToExplicitIndex(int index)
    {
        if (explicitWaypoints == null || explicitWaypoints.Count == 0) return false;
        index = Mathf.Clamp(index, 0, explicitWaypoints.Count - 1);
        explicitIndex = index;
        return TeleportCarToWaypoint(explicitWaypoints[index]);
    }

    // ---------- Core ----------
    bool TeleportCarToWaypoint(Transform wp)
    {
        if (!EnsureCar()) return false;

        var rb = car.GetComponent<Rigidbody>();
        var tr = car.transform;

        // Compute orientation
        Quaternion rot = wp.rotation;
        Vector3 pos = wp.position;

        // Try to align with path tangent if requested
        if (alignRotationToPathTangent && path != null && path.Points != null && path.Points.Count > 1)
        {
            int idx = IndexOfTransform(path.Points, wp);
            if (idx < 0) idx = FindNearestIndexXZ(path.Points, wp.position);
            Vector3 fwd = Vector3.forward;

            if (idx >= 0)
            {
                // Tangent = next - this (fallback: this - prev)
                Vector3 t;
                if (idx < path.Points.Count - 1 && path.Points[idx + 1] != null)
                    t = path.Points[idx + 1].position - path.Points[idx].position;
                else if (idx > 0 && path.Points[idx - 1] != null)
                    t = path.Points[idx].position - path.Points[idx - 1].position;
                else
                    t = wp.forward;

                t.y = 0f;
                if (t.sqrMagnitude > 0.0001f) fwd = t.normalized;

                rot = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else if (useWaypointRotationIfNoTangent)
            {
                rot = wp.rotation;
            }
        }
        else if (useWaypointRotationIfNoTangent)
        {
            rot = wp.rotation;
        }

        // Teleport (rigidbody-safe)
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
        }
        else
        {
            tr.SetPositionAndRotation(pos, rot);
        }

        // Tiny forward nudge along the chosen forward (prevents sitting exactly on the marker)
        if (postSnapForwardNudge > 0f)
        {
            Vector3 nudge = (rb ? rb.rotation * Vector3.forward : rot * Vector3.forward) * postSnapForwardNudge;
            if (rb) rb.position += nudge; else tr.position += nudge;
        }

        // Optional: kick off autopilot exactly from this waypoint
        // Optional: kick off autopilot exactly from this waypoint
    if (startAutopilotAfterTeleport)
{
    var nav = car.GetComponent<PrevAutoPilotNavigator>();
    if (nav != null)
    {
        nav.StartAutoPilotFromWaypoint(wp, startReturn);
    }
}
        return true;
    }

    Transform ResolveWaypointByMode()
    {
        switch (snapMode)
        {
            case SnapMode.ExplicitSingle:
                return explicitWaypoint;

            case SnapMode.ExplicitListByIndex:
                if (explicitWaypoints == null || explicitWaypoints.Count == 0) return null;
                explicitIndex = Mathf.Clamp(explicitIndex, 0, explicitWaypoints.Count - 1);
                return explicitWaypoints[explicitIndex];

            case SnapMode.NearestOnPath:
            default:
                if (path == null || path.Points == null || path.Points.Count == 0) return null;
                // Nearest *waypoint transform* (NOT the mid-segment)
                int nearest = FindNearestIndexXZ(path.Points, car ? car.transform.position : transform.position);
                if (nearest < 0) nearest = 0;
                return path.Points[nearest];
        }
    }


    int IndexOfWaypointInPath(Transform target)
    {
        if (path == null || path.Points == null || path.Points.Count == 0 || target == null) return 0;
        for (int i = 0; i < path.Points.Count; i++)
            if (path.Points[i] == target) return i;
        // fallback: nearest by XZ
        int best = 0; float bestD2 = float.PositiveInfinity;
        Vector2 p = new Vector2(target.position.x, target.position.z);
        for (int i = 0; i < path.Points.Count; i++)
        {
            if (!path.Points[i]) continue;
            var q = path.Points[i].position;
            float d2 = (new Vector2(q.x, q.z) - p).sqrMagnitude;
            if (d2 < bestD2) { best = i; bestD2 = d2; }
        }
        return best;
    }




    // ---------- Utils ----------
    bool EnsureCarold()
    {
        if (car != null) return true;

        if (!autoFindCar) return false;

        if (!string.IsNullOrEmpty(carTag))
        {
            var tagged = GameObject.FindGameObjectsWithTag(carTag);
            if (tagged != null && tagged.Length > 0) { car = tagged[0]; return true; }
        }

        foreach (var name in fallbackNames)
        {
            var go = GameObject.Find(name);
            if (go) { car = go; return true; }
        }
        return car != null;
    }
    
    
    bool EnsureCar()
{
    if (car != null && car.activeInHierarchy) return true;

    if (!autoFindCar) return false;

    // 1) Try by tag — pick the NEAREST active one (in case multiple exist)
    if (!string.IsNullOrEmpty(carTag))
    {
        var tagged = GameObject.FindGameObjectsWithTag(carTag);
        if (tagged != null && tagged.Length > 0)
        {
            GameObject best = null;
            float bestD2 = float.PositiveInfinity;
            Vector3 p = transform.position;
            foreach (var go in tagged)
            {
                if (!go || !go.activeInHierarchy) continue;
                float d2 = (go.transform.position - p).sqrMagnitude;
                if (d2 < bestD2) { bestD2 = d2; best = go; }
            }
            if (best)
            {
                car = ResolveCarRoot(best);
                return car != null;
            }
        }
    }

    // 2) Fallback by likely names (includes "Car(Clone)")
    foreach (var name in fallbackNames)
    {
        var go = GameObject.Find(name);
        if (go && go.activeInHierarchy)
        {
            car = ResolveCarRoot(go);
            if (car) return true;
        }
    }

    return false;
}

// If your Navigator is on a child, promote that child as the "car" we will move.
GameObject ResolveCarRoot(GameObject candidate)
{
    if (!candidate) return null;

    var nav = candidate.GetComponentInChildren<PrevAutoPilotNavigator>(true);
    if (nav) return nav.gameObject;

    return candidate;
}

    
    
    
    
    
    
      int IndexOfTransform(IReadOnlyList<Transform> list, Transform t)
{
    if (list == null || t == null) return -1;
    for (int i = 0; i < list.Count; i++)
        if (list[i] == t) return i;
    return -1;
}

int FindNearestIndexXZ(IReadOnlyList<Transform> list, Vector3 pos)
{
    if (list == null || list.Count == 0) return -1;
    int best = 0; float bestD2 = float.PositiveInfinity;
    Vector2 p = new Vector2(pos.x, pos.z);
    for (int i = 0; i < list.Count; i++)
    {
        var ti = list[i];
        if (!ti) continue;
        var q = ti.position;
        float d2 = (new Vector2(q.x, q.z) - p).sqrMagnitude;
        if (d2 < bestD2) { bestD2 = d2; best = i; }
    }
    return best;
}


    void OnDrawGizmosSelected()
    {
        if (path == null || path.Points == null || path.Points.Count == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Points.Count; i++)
        {
            var t = path.Points[i];
            if (!t) continue;
            Gizmos.DrawWireSphere(t.position, 0.5f);
            if (i < path.Points.Count - 1 && path.Points[i + 1])
                Gizmos.DrawLine(t.position, path.Points[i + 1].position);
        }
    }

 


}

// public class WaypointCarTeleporter : MonoBehaviour
// {




//     [Header("Autopilot Handoff")]
// public bool startAutopilotAfterTeleport = true;
//     [Header("Path")]
//     public SimpleWaypointPath path;
//     public bool searchSpawnPoints = true; // prefer WaypointSpawnPoint markers if present

//     [Header("Find Existing Car in Scene")]
//     public GameObject car;              // drag your car instance here (preferred)
//     public bool autoFindCar = true;     // if car is null, try to find it
//     public string carTag = "Vehicle";   // or "Car" — must be set on the car
//     public string[] fallbackNames = new[] { "HoverCar", "Car", "Vehicle", "PlayerCar" };

//     [Header("Placement")]
//     public bool alignToPathForward = true;
//     public float hoverHeight = 3f;        // meters above ground
//     public float bumpUp = 0.25f;          // small safety lift
//     public LayerMask groundMask = ~0;     // EXCLUDE the car's own layer
//     public float rayStartUp = 5f;         // start ray above target
//     public float rayDownDistance = 100f;  // ray length downward

//     [Header("Hotkeys (optional)")]
//     public bool enableHotkeys = true;
//     public KeyCode teleportFirstKey   = KeyCode.F7;
//     public KeyCode teleportNearestKey = KeyCode.F8;
//     public KeyCode teleportIndexKey   = KeyCode.F9;
//     [Min(0)] public int teleportIndex = 0;

//     void Awake()
//     {
//         if (!car && autoFindCar)
//             car = FindCarInScene();
//     }

//     void Update()
//     {
//         if (!enableHotkeys) return;

//         if (Input.GetKeyDown(teleportFirstKey))   TeleportToFirst();
//         if (Input.GetKeyDown(teleportNearestKey)) TeleportToNearest(Camera.main ? Camera.main.transform.position : transform.position);
//         if (Input.GetKeyDown(teleportIndexKey))   TeleportToIndex(teleportIndex);
//     }

//     // ---------- PUBLIC API ----------

//     public bool TeleportToFirst(bool preferMarkedSpawn = true)
//     {
//         if (!EnsurePath() || !EnsureCar()) return false;

//         Transform t = null;
//         if (searchSpawnPoints && preferMarkedSpawn)
//             t = FindFirstSpawnPoint();
//         if (!t) t = path.Points[0];
// // Optional: immediately start autopilot from this waypoint
// if (startAutopilotAfterTeleport)
// {
//     var nav = car.GetComponent<PrevAutoPilotNavigator>();
//     if (nav != null)
//     {
//         // Start from the nearest segment (which will be this waypoint we just snapped to)
//         nav.StartAutoPilotForward();
//     }
// }




//         return TeleportCarTo(t);
//     }

//     public bool TeleportToIndex(int index)
//     {
//         if (!EnsurePath() || !EnsureCar()) return false;

//         index = Mathf.Clamp(index, 0, path.Points.Count - 1);

//         Transform t = null;
//         if (searchSpawnPoints)
//         {
//             var markers = GetSpawnPointsOrdered();
//             if (markers.Length > 0)
//             {
//                 t = markers
//                     .OrderBy(m => Mathf.Abs(IndexOfWaypoint(m.transform) - index))
//                     .First().transform;
//             }
//         }
//         if (!t) t = path.Points[index];

//         return TeleportCarTo(t);
//     }

//     public bool TeleportToNearest(Vector3 referencePosition, bool preferMarkedSpawn = true)
//     {
//         if (!EnsurePath() || !EnsureCar()) return false;

//         Transform best = null;
//         float bestD2 = float.PositiveInfinity;

//         if (searchSpawnPoints && preferMarkedSpawn)
//         {
//             foreach (var sp in GetSpawnPointsOrdered())
//             {
//                 float d2 = (XZ(sp.transform.position) - XZ(referencePosition)).sqrMagnitude;
//                 if (d2 < bestD2) { bestD2 = d2; best = sp.transform; }
//             }
//         }

//         if (!best)
//         {
//             for (int i = 0; i < path.Points.Count; i++)
//             {
//                 var wp = path.Points[i];
//                 if (!wp) continue;
//                 float d2 = (XZ(wp.position) - XZ(referencePosition)).sqrMagnitude;
//                 if (d2 < bestD2) { bestD2 = d2; best = wp; }
//             }
//         }

//         return TeleportCarTo(best);
//     }

//     public void SetCar(GameObject sceneCar) => car = sceneCar; // manual assignment if you spawn it earlier

//     // ---------- INTERNALS ----------

//     GameObject FindCarInScene()
//     {
//         GameObject c = null;
//         if (!string.IsNullOrEmpty(carTag))
//             c = GameObject.FindGameObjectWithTag(carTag);
//         if (!c)
//         {
//             foreach (var name in fallbackNames)
//             {
//                 var cand = GameObject.Find(name);
//                 if (cand) { c = cand; break; }
//             }
//         }
//         if (!c) Debug.LogWarning("WaypointCarTeleporter: Could not find car in scene. Assign it in the inspector.", this);
//         return c;
//     }

//     bool TeleportCarTo(Transform target)
//     {
//         if (!target || !car)
//         {
//             Debug.LogWarning("WaypointCarTeleporter: Missing target or car.", this);
//             return false;
//         }

//         // Compute safe Y
//         Vector3 pos = target.position;
//         float groundY = pos.y;
//         bool haveGround = false;

//         if (Terrain.activeTerrain != null)
//         {
//             groundY = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
//             haveGround = true;
//         }
//         else if (Physics.Raycast(pos + Vector3.up * rayStartUp,
//                                  Vector3.down,
//                                  out RaycastHit hit,
//                                  rayDownDistance + rayStartUp,
//                                  groundMask,
//                                  QueryTriggerInteraction.Ignore))
//         {
//             groundY = hit.point.y;
//             haveGround = true;
//         }

//         if (haveGround) pos.y = Mathf.Max(pos.y, groundY + hoverHeight + bumpUp);
//         else pos.y += bumpUp;

//         // Rotation
//         Quaternion rot = car.transform.rotation;
//         if (alignToPathForward)
//         {
//             Vector3 fwd = target.forward; fwd.y = 0f;
//             if (fwd.sqrMagnitude > 0.0001f) rot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
//         }

//         // Move the scene car
//         var rb = car.GetComponent<Rigidbody>();
//         if (rb)
//         {
//             ZeroVel(rb);
//             rb.position = pos;
//             rb.rotation = rot;
//         }
//         else
//         {
//             car.transform.SetPositionAndRotation(pos, rot);
//         }

//         return true;
//     }

//     bool EnsurePath()
//     {
//         if (path == null || path.Points == null || path.Points.Count == 0)
//         {
//             Debug.LogWarning("WaypointCarTeleporter: Path not set or has no points.", this);
//             return false;
//         }
//         return true;
//     }

//     bool EnsureCar()
//     {
//         if (!car && autoFindCar) car = FindCarInScene();
//         return car != null;
//     }

//     WaypointSpawnPoint[] GetSpawnPointsOrdered()
//     {
//         return path.GetComponentsInChildren<WaypointSpawnPoint>(true)
//                    .OrderBy(sp => IndexOfWaypoint(sp.transform))
//                    .ToArray();
//     }

//     int IndexOfWaypoint(Transform t)
//     {
//         for (int i = 0; i < path.Points.Count; i++)
//             if (path.Points[i] == t) return i;

//         // not directly in list (e.g., child marker) → take nearest
//         float best = float.PositiveInfinity; int bestIdx = 0;
//         for (int i = 0; i < path.Points.Count; i++)
//         {
//             float d2 = (path.Points[i].position - t.position).sqrMagnitude;
//             if (d2 < best) { best = d2; bestIdx = i; }
//         }
//         return bestIdx;
//     }

//     Transform FindFirstSpawnPoint()
//     {
//         var markers = GetSpawnPointsOrdered();
//         if (markers == null || markers.Length == 0) return null;

//         var start = markers.FirstOrDefault(m => m.role == WaypointSpawnPoint.Role.Start);
//         return start ? start.transform : markers[0].transform;
//     }

//     static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

//     // Zero velocities safely (supports normal Rigidbody; tries linearVelocity via reflection if present)
//     void ZeroVel(Rigidbody rb)
//     {
//         // standard PhysX
//         rb.velocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         // if your project uses a custom 'linearVelocity' on Rigidbody, set it via reflection
//         var prop = typeof(Rigidbody).GetProperty("linearVelocity", BindingFlags.Public | BindingFlags.Instance);
//         if (prop != null && prop.CanWrite)
//         {
//             try { prop.SetValue(rb, Vector3.zero, null); } catch { /* ignore */ }
//         }
//     }

//     // Gizmo: show first and last for quick sanity
//     void OnDrawGizmosSelected()
//     {
//         if (path == null || path.Points == null || path.Points.Count == 0) return;
//         Gizmos.color = Color.cyan;
//         if (path.Points[0]) Gizmos.DrawWireSphere(path.Points[0].position, 0.6f);
//         Gizmos.color = Color.magenta;
//         if (path.Points[path.Points.Count-1]) Gizmos.DrawWireSphere(path.Points[path.Points.Count-1].position, 0.6f);
//     }
// }

