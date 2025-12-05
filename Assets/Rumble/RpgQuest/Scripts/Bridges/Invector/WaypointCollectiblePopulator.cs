using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Path/Waypoint Collectible Populator")]
[ExecuteAlways]
public class WaypointCollectiblePopulator : MonoBehaviour
{
    [Header("Waypoint Source")]
    [Tooltip("Root that holds SWS waypoint nodes. Leave empty to use this transform.")]
    public Transform pointsRoot;
    [Tooltip("If true, traverse all descendants; otherwise only direct children.")]
    public bool includeDescendants = true;
    [Tooltip("Name filter for nodes (SWS usually 'Waypoint'). Empty = no filter.")]
    public string nameFilterContains = "Waypoint";
    [Tooltip("Close the loop when computing tangents (for facing).")]
    public bool closeLoop = false;

    [Header("Collectible")]
    public GameObject collectiblePrefab;
    [Tooltip("Parent collectibles under the waypoint node instead of a generated folder.")]
    public bool parentUnderWaypoint = false;

    [Header("Placement")]
    [Tooltip("Place on every Nth waypoint.")]
    public int placeEvery = 1;
    public bool skipFirst = true;
    public bool skipLast = false;
    [Tooltip("Optional index range filter (inclusive). -1 = no limit.")]
    public int startIndex = 0;
    public int endIndex = -1;

    [Header("Offsets")]
    [Tooltip("Meters forward from the node, using local forward/tangent.")]
    public float forwardOffset = 0f;
    [Tooltip("Meters right from the node, using local right/tangent.")]
    public float lateralOffset = 0f;
    [Tooltip("Add vertical lift after ground projection.")]
    public float verticalOffset = 0.2f;

    [Header("Orientation")]
    public bool faceAlongPath = true;
    public bool randomYawJitter = false;
    public float yawJitterDegrees = 10f;

    [Header("Grounding & Clearance")]
    public bool alignToGround = true;
    public LayerMask groundLayers = ~0;
    public float rayUp = 5f, rayDown = 30f;
    public bool preventOverlap = true;
    public float clearanceRadius = 0.5f;
    public float clearanceHeight = 1.0f;
    public int clearanceTries = 6;
    public float jitterRadius = 0.75f;

    [Header("Lifecycle")]
    public bool autoRebuildInEditor = false;
    public string generatedParentName = "__GeneratedCollectibles__";

    Transform _generatedRoot;
    readonly List<Transform> _wps = new();

    [ContextMenu("Populate Collectibles Now")]
    public void Populate()
    {
        if (!collectiblePrefab)
        {
            Debug.LogWarning($"[{nameof(WaypointCollectiblePopulator)}] No collectiblePrefab assigned.", this);
            return;
        }

        GatherWaypoints();
        if (_wps.Count == 0)
        {
            Debug.LogWarning($"[{nameof(WaypointCollectiblePopulator)}] No waypoints found.", this);
            return;
        }

        if (!parentUnderWaypoint)
        {
            ClearGenerated();
            _generatedRoot = GetOrCreateGeneratedRoot();
        }

        int end = (endIndex >= 0 && endIndex < _wps.Count) ? endIndex : _wps.Count - 1;
        int start = Mathf.Clamp(startIndex, 0, end);

        for (int i = start; i <= end; i++)
        {
            if (skipFirst && i == 0) continue;
            if (skipLast && i == _wps.Count - 1) continue;
            if (placeEvery > 1 && ((i - start) % placeEvery != 0)) continue;

            var node = _wps[i];
            if (!node) continue;

            // Compute a tangent (prev->next) for forward/right basis
            Vector3 pos = node.position;
            Quaternion rot = node.rotation;

            if (faceAlongPath || forwardOffset != 0f || lateralOffset != 0f)
            {
                Vector3 fwd = TangentAt(i);
                if (fwd.sqrMagnitude < 1e-6f) fwd = node.forward;
                var basis = Quaternion.LookRotation(new Vector3(fwd.x, 0f, fwd.z).normalized, Vector3.up);
                pos += basis * new Vector3(lateralOffset, 0f, forwardOffset);
                rot = basis;
            }

            // Ground projection
            if (alignToGround)
                pos = ProjectToGround(pos) + Vector3.up * verticalOffset;
            else
                pos += Vector3.up * verticalOffset;

            // Clearance jitter (optional)
            if (preventOverlap && Physics.CheckBox(pos + Vector3.up * (clearanceHeight * 0.5f),
                                                   new Vector3(clearanceRadius, clearanceHeight * 0.5f, clearanceRadius),
                                                   Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                bool placed = false;
                for (int t = 0; t < clearanceTries; t++)
                {
                    Vector2 r = Random.insideUnitCircle * jitterRadius;
                    Vector3 cand = pos + new Vector3(r.x, 0f, r.y);
                    if (alignToGround) cand = ProjectToGround(cand) + Vector3.up * verticalOffset;
                    bool blocked = Physics.CheckBox(cand + Vector3.up * (clearanceHeight * 0.5f),
                                                    new Vector3(clearanceRadius, clearanceHeight * 0.5f, clearanceRadius),
                                                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                    if (!blocked) { pos = cand; placed = true; break; }
                }
                if (!placed) { /* fallback: keep original pos */ }
            }

            // Yaw jitter
            if (randomYawJitter)
            {
                float yaw = Random.Range(-yawJitterDegrees, yawJitterDegrees);
                rot = Quaternion.Euler(0f, rot.eulerAngles.y + yaw, 0f);
            }

            // Spawn
            Transform parent = parentUnderWaypoint ? node : _generatedRoot;
            var go = SafeInstantiate(collectiblePrefab, pos, rot, parent);
            go.name = parentUnderWaypoint ? $"{collectiblePrefab.name}" : $"{collectiblePrefab.name}_{i:00}";
        }
    }

    void GatherWaypoints()
    {
        _wps.Clear();
        var root = pointsRoot ? pointsRoot : transform;

        bool IsWaypoint(Transform t)
        {
            if (string.IsNullOrEmpty(nameFilterContains)) return true;
            return t.name.IndexOf(nameFilterContains, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (!includeDescendants)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.gameObject.activeInHierarchy && IsWaypoint(c)) _wps.Add(c);
            }
        }
        else
        {
            void AddDesc(Transform r)
            {
                for (int i = 0; i < r.childCount; i++)
                {
                    var c = r.GetChild(i);
                    if (c.gameObject.activeInHierarchy && IsWaypoint(c)) _wps.Add(c);
                    AddDesc(c);
                }
            }
            AddDesc(root);
        }
    }

    Vector3 TangentAt(int i)
    {
        int n = _wps.Count;
        if (n == 1) return Vector3.forward;
        int i0 = Mathf.Clamp(i - 1, 0, n - 1);
        int i1 = i;
        int i2 = Mathf.Clamp(i + 1, 0, n - 1);

        if (closeLoop)
        {
            i0 = (i - 1 + n) % n;
            i2 = (i + 1) % n;
        }

        Vector3 a = _wps[i0].position;
        Vector3 b = _wps[i1].position;
        Vector3 c = _wps[i2].position;

        Vector3 t = (c - a);
        t.y = 0f;
        return t;
    }

    Vector3 ProjectToGround(Vector3 pos)
    {
        Vector3 start = pos + Vector3.up * rayUp;
        if (Physics.Raycast(start, Vector3.down, out var hit, rayUp + rayDown, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;
        return pos;
    }

    GameObject SafeInstantiate(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            return go;
        }
#endif
        return Instantiate(prefab, pos, rot, parent);
    }

    Transform GetOrCreateGeneratedRoot()
    {
        var t = transform.Find(generatedParentName);
        if (!t)
        {
            var go = new GameObject(generatedParentName);
            t = go.transform;
            t.SetParent(transform);
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
        }
        return t;
    }

    void ClearGenerated()
    {
        var t = transform.Find(generatedParentName);
        if (!t) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(t.gameObject);
        else Destroy(t.gameObject);
#else
        Destroy(t.gameObject);
#endif
    }

    void OnValidate()
    {
        if (!autoRebuildInEditor) return;
        if (!collectiblePrefab) return;
        Populate();
    }
}

