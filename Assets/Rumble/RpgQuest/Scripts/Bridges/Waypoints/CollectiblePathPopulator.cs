using UnityEngine;

 
 
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
// CollectiblePathPopulator.cs  (fixed one-per-node logic)
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CollectiblePathPopulator : MonoBehaviour
{
    [Header("Inputs")]
    public WaypointPathVisualizer path;
    public Transform spawnParent;
    public GameObject[] collectiblePrefabs;

    [Header("Node Source")]
    [Tooltip("Assign your SWS Path object here (the parent with waypoint children). " +
             "If set and onePerNode=true, we will spawn exactly once per child waypoint in order.")]
    public Transform nodesRoot;
    public bool preferNodesRoot = true;

    [Header("Distribution")]
    public bool onePerNode = true;
    [Tooltip("Fallback when no nodesRoot assigned OR onePerNode=false. " +
             "Dense points will be coalesced at this spacing to avoid many spawns.")]
    public float nodeMinSpacing = 2.0f;
    public float everyMeters = 6f;     // used when onePerNode=false
    public float yOffset = 0.0f;
    public int randomSeed = 12345;

    [Header("Runtime Policy")]
    public bool editorOnly = true;
    public bool populateOnPlay = false;
    public bool clearBeforePopulate = true;
    public bool runOncePerSession = true;

    [Header("Debug")]
    public bool drawGizmos = true;

    bool _didRuntimeSpawn = false;

    void OnEnable()
    {
        if (!spawnParent) spawnParent = this.transform;
        if (!Application.isPlaying) return;
        if (editorOnly) return;
        if (runOncePerSession && _didRuntimeSpawn) return;
        if (populateOnPlay) { PopulateNow(); _didRuntimeSpawn = true; }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!spawnParent) spawnParent = this.transform;
        everyMeters   = Mathf.Max(0.25f, everyMeters);
        nodeMinSpacing= Mathf.Max(0.1f, nodeMinSpacing);
    }
#endif

    [ContextMenu("Populate (Editor)")]
    public void PopulateNow()
    {
        if (clearBeforePopulate) ClearSpawned();
        if (collectiblePrefabs == null || collectiblePrefabs.Length == 0)
        {
            Debug.LogWarning("[Populator] No collectiblePrefabs assigned.", this);
            return;
        }

        var rnd = new System.Random(randomSeed);
        int count = 0;

        if (onePerNode)
        {
            // --- Preferred: real waypoints from nodesRoot (SWS path parent) ---
            var nodePositions = GetWaypointNodePositions();
            if (nodePositions != null && nodePositions.Count > 0)
            {
                for (int i = 0; i < nodePositions.Count; i++)
                {
                    var pos = nodePositions[i] + Vector3.up * yOffset;
                    var rot = GuessForward(nodePositions, i);
                    SpawnOne(ChoosePrefab(rnd), pos, rot);
                    count++;
                }
            }
            else
            {
                // --- Fallback: coalesce the dense visualizer points by spacing ---
                var pts = GetCoalescedVisualizerPoints(nodeMinSpacing);
                for (int i = 0; i < pts.Count; i++)
                {
                    var pos = pts[i] + Vector3.up * yOffset;
                    var rot = GuessForward(pts, i);
                    SpawnOne(ChoosePrefab(rnd), pos, rot);
                    count++;
                }
            }
        }
        else
        {
            // March along by distance (still uses the visualizer curve)
            if (path == null || path.PathPoints == null || path.PathPoints.Count < 2)
            {
                Debug.LogWarning("[Populator] Path/points missing.", this);
                return;
            }

            float d = 0f;
            int seg; float t;
            path.ClosestPointOnPath(path.PathPoints[0], out seg, out t);
            Vector3 last = path.MarchForward(seg, t, 0f);

            while (true)
            {
                Vector3 p = path.MarchForward(seg, t, d);
                if ((p - last).sqrMagnitude < 1e-8f && d > 0f) break;

                var rot = Quaternion.LookRotation(((p - last).sqrMagnitude > 1e-8f ? (p - last) : Vector3.forward), Vector3.up);
                SpawnOne(ChoosePrefab(rnd), p + Vector3.up * yOffset, rot);
                count++;

                last = p;
                d += everyMeters;
                if (d > 100000f) break;
            }
        }

        Debug.Log($"[Populator] Spawned {count} collectible(s).", this);
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        if (!spawnParent) return;
        var spawned = new List<GameObject>();
        foreach (var tag in spawnParent.GetComponentsInChildren<SpawnedByPathPopulator>(true))
            if (tag) spawned.Add(tag.gameObject);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            foreach (var go in spawned)
                if (go) Undo.DestroyObjectImmediate(go);
        }
        else
#endif
        {
            foreach (var go in spawned)
                if (go) Destroy(go);
        }
        Debug.Log($"[Populator] Cleared {spawned.Count} spawned item(s).", this);
    }

    // ---------- helpers ----------

    // Prefer exact SWS waypoint children in order (one per node)
    List<Vector3> GetWaypointNodePositions()
    {
        if (!preferNodesRoot || !nodesRoot) return null;

        // use only direct children (SWS path layout) to preserve order
        var list = new List<Transform>();
        foreach (Transform child in nodesRoot) list.Add(child);
        list.Sort((a,b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

        // filter out inactive children
        var pos = new List<Vector3>(list.Count);
        foreach (var t in list)
            if (t && t.gameObject.activeInHierarchy) pos.Add(t.position);

        return pos;
    }

    // Coalesce dense curve points so you get ~one logical point per 'nodeMinSpacing' meters
    List<Vector3> GetCoalescedVisualizerPoints(float spacing)
    {
        var outPts = new List<Vector3>();
        if (path == null || path.PathPoints == null || path.PathPoints.Count == 0) return outPts;

        Vector3 last = Vector3.positiveInfinity;
        foreach (var p in path.PathPoints)
        {
            var w = p; w.y = 0f;
            if (last.x == float.PositiveInfinity || Vector3.Distance(new Vector3(last.x,0,last.z), w) >= spacing)
            {
                outPts.Add(p);
                last = p;
            }
        }
        return outPts;
    }

    // Forward from list of positions (Vector3)
    Quaternion GuessForward(IList<Vector3> pts, int i)
    {
        Vector3 fwd;
        if (i < pts.Count - 1) fwd = pts[i + 1] - pts[i];
        else                   fwd = pts[i] - pts[Mathf.Max(0, i - 1)];
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-8f) fwd = Vector3.forward;
        return Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }

    GameObject ChoosePrefab(System.Random r)
    {
        int idx = r.Next(0, collectiblePrefabs.Length);
        return collectiblePrefabs[idx];
    }

    void SpawnOne(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!prefab) return;
        var go = (GameObject)Instantiate(prefab, pos, rot, spawnParent);
        if (!go.CompareTag("Collectible")) go.tag = "Collectible";
        if (!go.TryGetComponent<SpawnedByPathPopulator>(out _))
            go.AddComponent<SpawnedByPathPopulator>();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // draw either nodesRoot waypoints or coalesced points so you see what will spawn
        var toDraw = (preferNodesRoot && nodesRoot) ? GetWaypointNodePositions()
                                                    : GetCoalescedVisualizerPoints(nodeMinSpacing);
        if (toDraw == null) return;

        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.6f);
        foreach (var p in toDraw)
            Gizmos.DrawSphere(p + Vector3.up * (yOffset + 0.1f), 0.15f);
    }
}

// This component populates a path with collectible items at specified intervals or at each waypoint.
// It can be configured to spawn items at runtime or only in the editor.
// The items are tagged with "Collectible" and can be cleared later.
// It supports both one item per waypoint or spaced out by a specified distance.
 
