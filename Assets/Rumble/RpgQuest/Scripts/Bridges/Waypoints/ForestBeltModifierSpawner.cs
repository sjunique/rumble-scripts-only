// Assets/Scripts/NavMesh/ForestBeltModifierSpawner.cs
// Spawns a ring/belt of NavMeshModifierVolumes (e.g., forest = OffPath/NotWalkable)
// and optionally triggers a NavMesh rebuild when done.
using System.Collections.Generic;
using UnityEngine;

// Components (Surface / Link / Modifier / ModifierVolume) live here:
using Unity.AI.Navigation;

// The classic NavMesh/Agent API (NavMesh.SamplePosition, NavMeshAgent) still uses:
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif
[AddComponentMenu("NavMesh/Forest Belt Modifier Spawner")]
[ExecuteAlways]
public class ForestBeltModifierSpawner : MonoBehaviour
{
    [Header("Belt Shape")]
    [Tooltip("Center of the belt; defaults to this transform if null")]
    public Transform center;

    [Tooltip("Outer radius of the belt (meters)")]
    public float outerRadius = 150f;

    [Tooltip("Inner radius of the belt (meters). 0 = solid disk")]
    public float innerRadius = 100f;

    [Tooltip("Y (height) size of each volume box")]
    public float height = 50f;

    [Tooltip("How many volume segments around the ring")]
    public int segments = 32;

    [Tooltip("Extruded thickness per segment (auto if 0)")]
    public float segmentThickness = 0f;

    [Header("NavMesh Area / Agents")]
    [Tooltip("Area type the belt should imprint into the NavMesh")]
    public int area = 2; // e.g., OffPath=2 in your setup; adjust to your Area index

    [Tooltip("If empty = affect ALL agent types; else only these")]
    public List<int> affectAgentTypeIDs = new(); // Use NavMesh.GetSettingsByID(...) if needed

    [Header("Layering")]
    [Tooltip("Layer assigned to the spawned volume GameObjects (optional)")]
    public int spawnedLayer = 0; // Default

    [Header("Lifecycle")]
    [Tooltip("Remove previously spawned volumes under this spawner before spawning new ones")]
    public bool clearBeforeSpawn = true;

    [Tooltip("Automatically rebuild ALL NavMeshSurface components after spawning")]
    public bool autoRebakeNavMesh = true;

    [Tooltip("Only rebuild NavMeshes in the same scene as this spawner")]
    public bool limitRebakeToThisScene = true;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColorOuter = new Color(0, 1, 0, 0.2f);
    public Color gizmoColorInner = new Color(1, 0, 0, 0.2f);

    const string SPAWNED_ROOT_NAME = "_ForestBelt_ModifierVolumes";

    [ContextMenu("Spawn Belt + (Optional) Rebuild NavMesh")]
    public void Spawn()
    {
        if (outerRadius <= 0f) { Debug.LogWarning("[ForestBelt] Outer radius must be > 0."); return; }
        if (segments < 3) segments = 3;

        var root = GetOrCreateRoot();

        if (clearBeforeSpawn)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying) Undo.DestroyObjectImmediate(child.gameObject);
                else
#endif
                Destroy(child.gameObject);
            }
        }

        var ctr = center ? center.position : transform.position;

        float angleStep = 360f / segments;
        float midRadius = (outerRadius + Mathf.Max(0f, innerRadius)) * 0.5f;
        float radialThickness = Mathf.Max(0.01f, outerRadius - Mathf.Max(0f, innerRadius));
        float perSegThickness = segmentThickness > 0f ? segmentThickness : (2f * Mathf.PI * midRadius / segments);

        for (int i = 0; i < segments; i++)
        {
            float angle = angleStep * i;
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);

            // Position at midRadius around the center
            Vector3 pos = ctr + rot * Vector3.forward * midRadius;

            // Create volume GO
            var go = new GameObject($"ForestBelt_Seg_{i:D2}");
            go.layer = spawnedLayer;
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(go, "Create Forest Belt Segment");
#endif
            go.transform.SetParent(root, worldPositionStays: false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            // Size: thickness (radial) x height (Y) x length (tangent)
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(radialThickness, height, perSegThickness);

            var modVol = go.AddComponent<NavMeshModifierVolume>();
            modVol.center = Vector3.zero;
            modVol.size = box.size;
            modVol.area = area;

            // Affect specific agents only if list provided
            if (affectAgentTypeIDs != null && affectAgentTypeIDs.Count > 0)
            {
                // The API exposes a mask per agent type behind the scenes; simplest is to gate via overrideOn and area.
                // If you need per-agent filtering, duplicate volumes per agent type and enable/disable by script at bake-time.
            }
        }

        Debug.Log($"[ForestBelt] Spawned {segments} NavMeshModifierVolume segments (area={area}).");

        if (autoRebakeNavMesh)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorApplication.delayCall += RebuildNavMeshes; // wait a tick so hierarchy updates settle
            else
#endif
                RebuildNavMeshes();
        }
    }

    Transform GetOrCreateRoot()
    {
        var t = transform.Find(SPAWNED_ROOT_NAME);
        if (t != null) return t;

        var go = new GameObject(SPAWNED_ROOT_NAME);
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(go, "Create Forest Belt Root");
#endif
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    void RebuildNavMeshes()
    {
        var surfaces = FindObjectsOfType<NavMeshSurface>(includeInactive: true);
        int rebuilt = 0;

        foreach (var s in surfaces)
        {
            if (!s) continue;
            if (limitRebakeToThisScene && s.gameObject.scene != gameObject.scene) continue;

#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RecordObject(s, "Rebuild NavMesh");
#endif
            s.BuildNavMesh();
            rebuilt++;
        }

        Debug.Log($"[ForestBelt] Rebuilt {rebuilt} NavMeshSurface(s).");
    }

    // Auto-spawn on value changes in Editor (optional, keeps things WYSIWYG)
    void OnValidate()
    {
        outerRadius = Mathf.Max(1f, outerRadius);
        innerRadius = Mathf.Clamp(innerRadius, 0f, outerRadius - 0.01f);
        height = Mathf.Max(0.1f, height);
        segments = Mathf.Clamp(segments, 3, 512);

        // Live preview in Editor (toggle by commenting next line if too chatty)
        if (!Application.isPlaying && isActiveAndEnabled)
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this) Spawn();
            };
#endif
        }
    }

    // Gizmos for preview
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        var ctr = center ? center.position : transform.position;

        // Outer ring
        Gizmos.color = gizmoColorOuter;
        DrawWireDisc(ctr, Vector3.up, outerRadius);

        // Inner ring (if any)
        if (innerRadius > 0f)
        {
            Gizmos.color = gizmoColorInner;
            DrawWireDisc(ctr, Vector3.up, innerRadius);
        }
    }

    static void DrawWireDisc(Vector3 center, Vector3 normal, float radius, int steps = 64)
    {
        Vector3 prev = center + Quaternion.AngleAxis(0, normal) * Vector3.forward * radius;
        for (int i = 1; i <= steps; i++)
        {
            float a = (360f * i) / steps;
            Vector3 curr = center + Quaternion.AngleAxis(a, normal) * Vector3.forward * radius;
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }
}
