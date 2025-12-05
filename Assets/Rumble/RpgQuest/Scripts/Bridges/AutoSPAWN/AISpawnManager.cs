using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
 
 

public class AISpawnManager : MonoBehaviour
{


 

// layerMask for AI bodies – adjust to your character layer
[SerializeField] LayerMask characterLayer = ~0;   // or set in inspector
[SerializeField] LayerMask groundMask = ~0;
[SerializeField] float surfaceOffset = 0.1f;

    public AIPoolManager poolManager;

    public List<AISpawnPoint> points = new();
    public bool autoDiscoverPoints = true;
    public bool spawnOnStart = true;

    // bookkeeping
    readonly Dictionary<AISpawnPoint, int> _alive = new();
    readonly Dictionary<GameObject, AISpawnPoint> _owner = new();
    readonly Dictionary<AISpawnPoint, float> _nextSpawnAt = new();

    void Awake()
    {

        if (poolManager)
        {
            foreach (var pt in points)
            {
                if (!pt || !pt.prefab) continue;
                poolManager.RegisterPrefab(pt.PoolId(), pt.prefab);
            }
        }
        if (autoDiscoverPoints)
        {
            points.Clear();
            points.AddRange(FindObjectsOfType<AISpawnPoint>(true));
        }

        foreach (var p in points)
        {
            if (!p) continue;
            _alive[p] = 0;
            _nextSpawnAt[p] = Time.time;
        }
    }

    void Start()
    {
        if (!spawnOnStart) return;
        foreach (var p in points)
        {
            if (!p || !p.prefab) continue;
            var toSpawn = Mathf.Clamp(p.initialCount, 0, Mathf.Max(0, p.maxAlive));
            for (int i = 0; i < toSpawn; i++) TrySpawn(p);
        }
    }

 void Update()
{
    var now = Time.time;

    foreach (var point in points)
    {
        if (!point) continue;

        // ❗ Points owned by Activators are not auto-spawned
        if (point.activatorControlled)
            continue;

        if (!point.prefab || !point.respawnEnabled)
            continue;

        if (_alive.TryGetValue(point, out var alive) && alive >= point.maxAlive)
            continue;

        if (now >= _nextSpawnAt[point])
        {
            TrySpawn(point);
        }
    }
}


    // -------------------------------------------------------
    public void TrySpawn(AISpawnPoint p)
    {
        if (!p || !p.prefab) return;

        if (!TryComputeSpawnPoseFromPoint(p, p.transform.position, out var pos, out var rot))
            return;

        GameObject go = null;
        var id = p.PoolId();

        if (poolManager)
            go = poolManager.Get(id, pos, rot);
        else
            go = Instantiate(p.prefab, pos, rot);

        if (!go) return;

        ConfigureMovementMode_RBAgent(go);
        PlaceOnGround(go, pos, rot);
        EnsureOnNavMesh(go);

        AttachDeathGuard(go);
        AttachMotorSilencer(go);
        ConfigureAgentDefaults(go);
        InvokePooledReset(go);

        _alive[p] = Mathf.Clamp(_alive[p] + 1, 0, int.MaxValue);
        _owner[go] = p;
        _nextSpawnAt[p] = Time.time + p.respawnDelay;
    }
    public void OnAIDied(GameObject ai)
    {
        if (!ai) return;
        if (_owner.TryGetValue(ai, out var p))
        {
            _owner.Remove(ai);
            _alive[p] = Mathf.Max(0, _alive[p] - 1);
            _nextSpawnAt[p] = Time.time + p.respawnDelay;
        }
    }

    // -------------------------------------------------------
    // placement / navmesh

    void AttachMotorSilencer(GameObject go)
    {
        if (!go.GetComponent<AIMotorDeathSilencer>())
            go.AddComponent<AIMotorDeathSilencer>();
    }


    void AttachDeathGuard(GameObject go)
    {
        if (!go.GetComponent<DeathVelocityGuard>())
            go.AddComponent<DeathVelocityGuard>();
    }

    void ConfigureAgentDefaults(GameObject go)
    {
        var a = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (!a) return;
        a.angularSpeed = Mathf.Clamp(a.angularSpeed, 180f, 999f);   // 180–240 melee, 120–180 shooter
        a.acceleration = Mathf.Clamp(a.acceleration, 8f, 999f);     // 8–12 melee, 6–9 shooter
        a.autoBraking = true;
    }






bool TryComputeSpawnPoseFromPoint(AISpawnPoint point, Vector3 fallbackPos, 
                                  out Vector3 pos, out Quaternion rot)
{
    pos = fallbackPos;
    rot = point.transform.rotation;

    // --- 1. pick a random horizontal offset inside spawnRadius ---
    Vector2 circle = Random.insideUnitCircle * point.spawnRadius;
    Vector3 candidate = point.transform.position + new Vector3(circle.x, 0f, circle.y);

    // start ray a bit above; this is same height you already use
    candidate.y += 10f;

    const int maxAttempts = 5;
    for (int i = 0; i < maxAttempts; i++)
    {
        // Raycast down to ground
        if (Physics.Raycast(candidate, Vector3.down, out var hit, 20f, groundMask))
        {
            Vector3 groundPos = hit.point;

            // --- 2. check NavMesh near that point ---
            if (NavMesh.SamplePosition(groundPos, out var navHit, 2f, NavMesh.AllAreas))
            {
                Vector3 final = navHit.position;

                // --- 3. check separation from other AIs ---
                float radius = point.minSeparation * 0.5f;
                bool overlaps = Physics.CheckSphere(final + Vector3.up * 0.5f,
                                                    radius,
                                                    characterLayer,
                                                    QueryTriggerInteraction.Ignore);

                if (!overlaps)
                {
                    pos = final + Vector3.up * surfaceOffset; // your small lift from ground
                    rot = Quaternion.LookRotation(point.transform.forward, Vector3.up);
                    return true;
                }
            }
        }

        // try another random spot
        Vector2 nextCircle = Random.insideUnitCircle * point.spawnRadius;
        candidate = point.transform.position + new Vector3(nextCircle.x, 0f, nextCircle.y);
        candidate.y += 10f;
    }

    // fallback – use original point center (may stack if everything else failed)
    pos = point.transform.position + Vector3.up * surfaceOffset;
    rot = point.transform.rotation;
    return true;
}



    bool TryComputeSpawnPoseFromPointsss(AISpawnPoint p, Vector3 origin, out Vector3 pos, out Quaternion rot)
    {
        pos = origin;
        rot = p.transform.rotation;

        // ray to ground
        if (Physics.Raycast(origin + Vector3.up * p.raycastHeight, Vector3.down,
                            out var hit, p.raycastHeight * 2f, p.groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            // slope gate
            if (Vector3.Angle(hit.normal, Vector3.up) > p.maxSlopeAngle)
                return false;

            pos = hit.point + Vector3.up * p.surfaceOffset;

            // optional navmesh nudge
            if (p.preferNavMesh && NavMesh.SamplePosition(pos, out var nHit, p.navmeshSearchRadius, NavMesh.AllAreas))
                pos = nHit.position + Vector3.up * p.surfaceOffset;

            // align to ground if desired
            if (p.alignToNormal)
            {
                var fwd = Vector3.ProjectOnPlane(p.transform.forward, hit.normal);
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(Vector3.forward, hit.normal);
                rot = Quaternion.LookRotation(fwd.normalized, hit.normal);
            }
            else rot = p.transform.rotation;

            return true;
        }

        // fallback: navmesh only
        if (p.preferNavMesh && NavMesh.SamplePosition(origin, out var only, p.navmeshSearchRadius, NavMesh.AllAreas))
        {
            pos = only.position + Vector3.up * p.surfaceOffset;
            rot = p.transform.rotation;
            return true;
        }

        // final fallback
        pos = origin;
        rot = p.transform.rotation;
        return true;
    }

    // -------------------------------------------------------
    // movement modes


    void ConfigureMovementMode(GameObject go)
    {
        var agent = go.GetComponent<NavMeshAgent>();
        var rb = go.GetComponent<Rigidbody>();
        var anim = go.GetComponent<Animator>();

        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (agent)
        {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
        if (anim) anim.applyRootMotion = false; // controller handles motion; Step Offset stays ON
    }




    void ConfigureMovementModes(GameObject go, AISpawnPoint.MovementMode mode)
    {
        var agent = go.GetComponent<NavMeshAgent>();
        var rb = go.GetComponent<Rigidbody>();
        var anim = go.GetComponent<Animator>();

        switch (mode)
        {
            case AISpawnPoint.MovementMode.AgentWithRigidbody:
                if (rb) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                if (agent)
                {
                    agent.enabled = true;
                    agent.updatePosition = true;
                    agent.updateRotation = true;
                    agent.isStopped = false;
                }
                if (anim) anim.applyRootMotion = false;
                break;

            case AISpawnPoint.MovementMode.AgentKinematic:
                if (rb) rb.isKinematic = true;
                if (agent)
                {
                    agent.enabled = true;
                    agent.updatePosition = true;
                    agent.updateRotation = true;
                    agent.isStopped = false;
                }
                if (anim) anim.applyRootMotion = false;
                break;

            case AISpawnPoint.MovementMode.PhysicsMotor:
                if (rb) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                if (agent) agent.enabled = false;
                // root motion depends on your motor; keeping off is usually fine
                if (anim) anim.applyRootMotion = false;
                break;
        }
    }

    void PlaceOnGround(GameObject go, Vector3 pos, Quaternion rot)
    {
        var agent = go.GetComponent<NavMeshAgent>();
        var rb = go.GetComponent<Rigidbody>();

        // set pose first
        go.transform.SetPositionAndRotation(pos, rot);

        // keep agent & RB in sync for the first frame
        if (agent && agent.enabled) agent.Warp(pos);
        if (rb && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // MovePosition next physics step avoids tiny penetrations
            rb.MovePosition(pos);
        }
    }

    // -------------------------------------------------------

    class _AI_DespawnTracker : MonoBehaviour
    {
        AISpawnManager _mgr;
        public void Init(AISpawnManager mgr) => _mgr = mgr;
        void OnDestroy() { if (_mgr) _mgr.OnAIDied(gameObject); }
    }



    public GameObject InstantiateForActivator(AISpawnPoint p)
    {
        if (!p || !p.prefab) return null;

        if (!TryComputeSpawnPoseFromPoint(p, p.transform.position, out var pos, out var rot))
            return null;

        var id = p.PoolId();
        var go = poolManager ? poolManager.Get(id, pos, rot) : Instantiate(p.prefab, pos, rot);
        if (!go) return null;

        ConfigureMovementMode_RBAgent(go);
        PlaceOnGround(go, pos, rot);
        EnsureOnNavMesh(go);

        AttachDeathGuard(go);
        AttachMotorSilencer(go);
        ConfigureAgentDefaults(go);
        InvokePooledReset(go);

        return go;
    }
    // helpers
    void ConfigureMovementMode_RBAgent(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var anim = go.GetComponent<Animator>();

        if (rb) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        if (agent) { agent.enabled = true; agent.updatePosition = true; agent.updateRotation = true; agent.isStopped = false; }
        if (anim) anim.applyRootMotion = false;
    }


    void InvokePooledReset(GameObject go)
    {
        if (!go) return;
        var resets = go.GetComponentsInChildren<IAIReset>(true);
        for (int i = 0; i < resets.Length; i++)
            resets[i].OnPooledGet();
    }

    void EnsureOnNavMesh(GameObject go)
    {
        var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (!agent) return;
        if (agent.isOnNavMesh) return;

        var pos = go.transform.position;
        if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 5f, agent.areaMask))
            agent.Warp(hit.position + Vector3.up * 0.001f);
    }

    // When you return an AI (despawn), use the same id:
    public void Despawn(GameObject ai)
    {
        if (!ai) return;
        if (_owner.TryGetValue(ai, out var p))
        {
            _owner.Remove(ai);
            _alive[p] = Mathf.Max(0, _alive[p] - 1);

            var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent) { agent.isStopped = true; agent.ResetPath(); }

            var id = p.PoolId();
            if (poolManager) poolManager.Return(id, ai);
            else Destroy(ai);

            _nextSpawnAt[p] = Time.time + p.respawnDelay;
        }
    }
}
