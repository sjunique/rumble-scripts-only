using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string id;            // e.g. "AI_Melee", "AI_Shooter"
        public GameObject prefab;    // the prefab to pool
        public int initialCount = 4; // prewarm count
    }

    [Header("Pools")]
    public List<Pool> pools = new();

    // runtime state
    readonly Dictionary<string, Queue<GameObject>> _queues = new();
    readonly Dictionary<string, GameObject> _prefabs = new();

void Awake()
{
    // Find ANY valid NavMesh position once and reuse it for prewarm
    Vector3 safePos = Vector3.zero;
    NavMeshHit hit;
    if (NavMesh.SamplePosition(Vector3.zero, out hit, 1000f, NavMesh.AllAreas))
        safePos = hit.position;
    else
        safePos = Vector3.zero;  // fallback if no NavMesh yet (scene without bake)

    foreach (var p in pools)
    {
        if (p == null || p.prefab == null || string.IsNullOrEmpty(p.id))
            continue;

        if (_queues.ContainsKey(p.id)) continue;

        _prefabs[p.id] = p.prefab;
        var q = new Queue<GameObject>();

        for (int i = 0; i < Mathf.Max(0, p.initialCount); i++)
        {
            // 👇 prewarm on NavMesh, not at (0,0,0)
            var go = Instantiate(p.prefab, safePos, Quaternion.identity, transform);
            go.name = p.prefab.name;
            go.SetActive(false);
            q.Enqueue(go);
        }

        _queues.Add(p.id, q);
    }
}

    GameObject PrefabFor(string id)
    {
        if (_prefabs.TryGetValue(id, out var pf) && pf != null) return pf;
        Debug.LogWarning($"[AIPool] No prefab registered for id '{id}'");
        return null;
    }

    /// <summary>
    /// Fetch an instance positioned/rotated and ready.
    /// </summary>
    public GameObject Get(string id, Vector3 pos, Quaternion rot)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[AIPool] Get called with empty id");
            return null;
        }

        if (!_queues.TryGetValue(id, out var q))
        {
            // if unknown, try to create a queue on the fly (allows runtime ids)
            var pf = PrefabFor(id);
            if (pf == null) { Debug.LogWarning($"[AIPool] Unknown pool id '{id}'"); return null; }
            q = new Queue<GameObject>();
            _queues[id] = q;
        }

        GameObject go = null;

        if (q.Count > 0)
        {
            go = q.Dequeue();
            if (go == null) // in case something destroyed it
            {
                var pf = PrefabFor(id);
                if (pf == null) return null;
                go = Instantiate(pf, pos, rot, transform);
            }
        }
        else
        {
            var pf = PrefabFor(id);
            if (pf == null) return null;
            go = Instantiate(pf, pos, rot, transform);
        }

        // activate & place
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        // sanity: ensure RB/Agent are in a good state on checkout
        var rb = go.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var agent = go.GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.ResetPath();
        }

        // optional: call any reset hooks
        foreach (var reset in go.GetComponentsInChildren<IAIReset>(true))
            reset.OnPooledGet();

        return go;
    }


public void RegisterPrefab(string id, GameObject prefab)
{
    if (string.IsNullOrEmpty(id) || prefab == null) return;
    if (_prefabs.ContainsKey(id)) return;
    _prefabs[id] = prefab;                    // so PrefabFor(id) succeeds
    if (!_queues.ContainsKey(id)) _queues[id] = new Queue<GameObject>();
}

    /// <summary>
    /// Return an instance to the pool.
    /// </summary>
    public void Return(string id, GameObject go)
    {
        if (go == null) return;

        if (!_queues.TryGetValue(id, out var q))
        {
            // make a queue so we don't leak
            q = new Queue<GameObject>();
            _queues[id] = q;
        }

        // stop motion cleanly
        var agent = go.GetComponent<NavMeshAgent>();
        if (agent) { agent.isStopped = true; agent.ResetPath(); }

        var rb = go.GetComponent<Rigidbody>();
        if (rb && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // optional: notify listeners
        foreach (var reset in go.GetComponentsInChildren<IAIReset>(true))
            reset.OnPooledReturn();

        go.SetActive(false);
        go.transform.SetParent(transform);
        q.Enqueue(go);
    }
}
