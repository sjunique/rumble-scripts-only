 // ==============================
// WaypointPathVisualizer.cs
// Draws a smooth visual path along your waypoint chain
// + projects to terrain/navmesh
// + exposes sampled points for followers/nudging
// ==============================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Path/Waypoint Path Visualizer")] 
[ExecuteAlways]
public class WaypointPathVisualizer : MonoBehaviour
{

    [Header("State")]
[System.NonSerialized] public int PathBuildVersion = 0;
public event System.Action PathRebuilt;

    [Header("Waypoints")]
    public Transform pointsRoot;                // Parent containing child waypoints in order
    public bool readChildrenEveryRebuild = true; // Auto read children on Rebuild
    public bool closeLoop = false;               // Optional loop

    [Tooltip("If true, also include grandchildren/descendants (useful when waypoints are nested under another child, e.g., 'tttt/Waypoint 0..N').")] 
    public bool includeDescendants = true;
    [Tooltip("Filter descendant names (leave empty to include all). For SWS, 'Waypoint' works well.")] 
    public string nameFilterContains = "Waypoint";

    [Header("Spline")]
    public bool useCatmullRom = true;            // Smooth curve; off = straight lines
    [Range(2, 64)] public int samplesPerSegment = 12; // Curvature resolution

    [Header("Projection & Height")] 
    public bool projectToNavMesh = true;         // Prefer snapping to navmesh
    public float navMeshSampleMaxDistance = 2f;  // How far to search when projecting
    public LayerMask groundMask = ~0;            // Fallback Physics raycast layers
    public float groundOffset = 0.12f;           // Keep line floating slightly above ground

    [Header("Renderer")] 
    public LineRenderer line;                    // Assign a LineRenderer (Unlit/Transparent is nice)
    public float simplifyMinSpacing = 0.25f;     // Remove tiny zigzags after sampling

    // Public read-only result points (world space)
    public IReadOnlyList<Vector3> PathPoints => _pathPoints;

    private readonly List<Transform> _waypoints = new();
    private readonly List<Vector3> _sampled = new();
    private readonly List<Vector3> _pathPoints = new();

    [ContextMenu("Rebuild Path Now")]
    public void Rebuild()
    {
        GatherWaypoints();
        SampleSpline();
        ProjectSamples();
        Simplify();
        PushToRenderer();
        PathBuildVersion++;
PathRebuilt?.Invoke();
    }

    void OnEnable() { Rebuild(); }
    void OnValidate() { if (!Application.isPlaying) Rebuild(); }

    void GatherWaypoints()
    {
        _waypoints.Clear();
        if (!pointsRoot) pointsRoot = transform;

        // Helper to decide if a transform is a waypoint by name
        bool IsWaypoint(Transform t)
        {
            if (string.IsNullOrEmpty(nameFilterContains)) return true;
            return t.name.IndexOf(nameFilterContains, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (readChildrenEveryRebuild)
        {
            if (!includeDescendants)
            {
                for (int i = 0; i < pointsRoot.childCount; i++)
                {
                    var c = pointsRoot.GetChild(i);
                    if (c.gameObject.activeInHierarchy && IsWaypoint(c))
                        _waypoints.Add(c);
                }
            }
            else
            {
                // Recursive pre-order traversal to keep hierarchy order
                void AddDesc(Transform r)
                {
                    for (int i = 0; i < r.childCount; i++)
                    {
                        var c = r.GetChild(i);
                        if (c.gameObject.activeInHierarchy && IsWaypoint(c))
                            _waypoints.Add(c);
                        AddDesc(c);
                    }
                }
                AddDesc(pointsRoot);

                // Edge case: if we only captured the container (no real points), fall back to direct children
                if (_waypoints.Count < 2)
                {
                    for (int i = 0; i < pointsRoot.childCount; i++)
                    {
                        var c = pointsRoot.GetChild(i);
                        if (c.gameObject.activeInHierarchy)
                            _waypoints.Add(c);
                    }
                }
    }
        }
       /* else
        {
            // manual list via children order retained
            for (int i = 0; i < pointsRoot.childCount; i++)
                _waypoints.Add(pointsRoot.GetChild(i));
        }*/
    }

    void SampleSpline()
    {
        _sampled.Clear();
        if (_waypoints.Count < 2) return;

        if (!useCatmullRom)
        {
            // Linear: sample each segment endpoints with uniform steps
            for (int i = 0; i < _waypoints.Count - 1 + (closeLoop ? 1 : 0); i++)
            {
                Vector3 a = _waypoints[i % _waypoints.Count].position;
                Vector3 b = _waypoints[(i + 1) % _waypoints.Count].position;
                for (int s = 0; s <= samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    _sampled.Add(Vector3.Lerp(a, b, t));
                }
            }
            return;
        }

        // Catmull-Rom
        for (int i = 0; i < _waypoints.Count - 1 + (closeLoop ? 1 : 0); i++)
        {
            // Indices with clamping or looping at ends
            Vector3 p0 = _waypoints[WrapIndex(i - 1)].position;
            Vector3 p1 = _waypoints[WrapIndex(i)].position;
            Vector3 p2 = _waypoints[WrapIndex(i + 1)].position;
            Vector3 p3 = _waypoints[WrapIndex(i + 2)].position;

            for (int s = 0; s <= samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                _sampled.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
    }

    int WrapIndex(int i)
    {
        int n = _waypoints.Count;
        if (closeLoop)
            return (i % n + n) % n;
        // clamp for non-loop end segments
        return Mathf.Clamp(i, 0, n - 1);
    }

    static Vector3 CatmullRom(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3, float t)
    {
        // Standard Catmull-Rom
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    void ProjectSamples()
    {
        _pathPoints.Clear();
        foreach (var p in _sampled)
        {
            Vector3 outP = p;
            bool placed = false;

            if (projectToNavMesh && NavMesh.SamplePosition(p, out var hit, navMeshSampleMaxDistance, NavMesh.AllAreas))
            {
                outP = hit.position + Vector3.up * groundOffset;
                placed = true;
            }

            if (!placed)
            {
                // Physics raycast down from above
                Vector3 rayOrigin = p + Vector3.up * 1000f;
                if (Physics.Raycast(rayOrigin, Vector3.down, out var rh, 2000f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    outP = rh.point + Vector3.up * groundOffset;
                    placed = true;
                }
            }

            if (!placed)
            {
                outP = p + Vector3.up * groundOffset; // fallback
            }

            _pathPoints.Add(outP);
        }
    }

    void Simplify()
    {
        if (_pathPoints.Count < 3 || simplifyMinSpacing <= 0f) return;
        var simplified = new List<Vector3>(_pathPoints.Count);
        Vector3 last = _pathPoints[0];
        simplified.Add(last);
        for (int i = 1; i < _pathPoints.Count; i++)
        {
            if (Vector3.Distance(last, _pathPoints[i]) >= simplifyMinSpacing)
            {
                last = _pathPoints[i];
                simplified.Add(last);
            }
        }
        _pathPoints.Clear();
        _pathPoints.AddRange(simplified);
    }

    void PushToRenderer()
    {
        if (!line) return;
        if (_pathPoints.Count < 2)
        {
            line.positionCount = 0;
            return;
        }
        line.positionCount = _pathPoints.Count;
        line.SetPositions(_pathPoints.ToArray());
    }

    // Utility: closest point on the polyline; returns world point and segment index
    public Vector3 ClosestPointOnPath(Vector3 worldPos, out int segIndex, out float segT)
    {
        segIndex = -1; segT = 0f;
        float bestSqr = float.MaxValue;
        Vector3 best = Vector3.zero;
        for (int i = 0; i < _pathPoints.Count - 1; i++)
        {
            Vector3 a = _pathPoints[i];
            Vector3 b = _pathPoints[i + 1];
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude + 1e-6f;
            float t = Mathf.Clamp01(Vector3.Dot(worldPos - a, ab) / len2);
            Vector3 p = a + ab * t;
            float d2 = (worldPos - p).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2; best = p; segIndex = i; segT = t;
            }
        }
        return best;
    }

    // Utility: march forward along the path by distance from a starting segment/t value
    public Vector3 MarchForward(int segIndex, float segT, float distance)
    {
        Vector3 current = Vector3.Lerp(_pathPoints[segIndex], _pathPoints[segIndex+1], segT);
        float remaining = distance;
        int i = segIndex;
        float t = segT;
        while (remaining > 0f && i < _pathPoints.Count - 1)
        {
            Vector3 a = Vector3.Lerp(_pathPoints[i], _pathPoints[i+1], t);
            Vector3 b = _pathPoints[i+1];
            float step = Vector3.Distance(a, b);
            if (step > remaining)
            {
                float f = remaining / step;
                return Vector3.Lerp(a, b, f);
            }
            remaining -= step;
            i++;
            t = 0f;
        }
        return _pathPoints.Last();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!pointsRoot) pointsRoot = transform;
        // Draw waypoint spheres
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        for (int i = 0; i < pointsRoot.childCount; i++)
        {
            Gizmos.DrawSphere(pointsRoot.GetChild(i).position + Vector3.up * 0.05f, 0.15f);
        }

        // Draw sampled polyline
        if (_pathPoints.Count > 1)
        {
            Gizmos.color = new Color(1f, 1f, 0.3f, 0.9f);
            for (int i = 0; i < _pathPoints.Count - 1; i++)
                Gizmos.DrawLine(_pathPoints[i], _pathPoints[i + 1]);
        }
    }
#endif
}

 