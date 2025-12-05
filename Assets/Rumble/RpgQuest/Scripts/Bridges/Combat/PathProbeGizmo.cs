// Assets/Scripts/AI/Debug/PathProbeGizmo.cs
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class PathProbeGizmo : MonoBehaviour
{
    public Transform agentOrigin;   // e.g., your player or a dummy
    public Transform target;        // a point across the belt
    public Color pathColor = Color.cyan;
    public float sampleRadius = 4f;
    public bool drawInGame = true;

    NavMeshPath path;
    float lastLen;

    void OnEnable() { path = new NavMeshPath(); }

    void Update()
    {
        if (!agentOrigin || !target) return;

        // Snap both positions to NavMesh
        if (!NavMesh.SamplePosition(agentOrigin.position, out var a, sampleRadius, NavMesh.AllAreas)) return;
        if (!NavMesh.SamplePosition(target.position,      out var b, sampleRadius, NavMesh.AllAreas)) return;

        if (NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path))
        {
            lastLen = PathLength(path);
        }
    }

    void OnDrawGizmos()
    {
        if (!agentOrigin || !target || path == null) return;
        Gizmos.color = pathColor;
        var c = path.corners;
        for (int i = 0; i < c.Length - 1; i++) Gizmos.DrawLine(c[i], c[i + 1]);

        // Labels (editor only)
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(target.position + Vector3.up * 2f, $"Path len: {lastLen:0.0} m");
        #endif
    }

    float PathLength(NavMeshPath p)
    {
        float d = 0f; var c = p.corners;
        for (int i = 0; i < c.Length - 1; i++) d += Vector3.Distance(c[i], c[i + 1]);
        return d;
    }
}
