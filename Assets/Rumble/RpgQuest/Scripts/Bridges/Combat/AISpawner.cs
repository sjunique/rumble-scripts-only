// Assets/Scripts/AI/Spawn/AISpawner.cs
 
using UnityEngine;
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;      // for NavMesh, NavMeshAgent, NavMesh.SamplePosition
using Unity.AI.Navigation; // for NavMeshSurface, NavMeshLink, NavMeshModifier, etc.

public class AISpawner : MonoBehaviour
{


   [Header("Auto Spawn")]
    public bool spawnOnStart = true;
    public float spawnDelay = 0.5f;              // small delay to let scene settle
    public bool rebuildNavMeshFirst = false;     // call BuildNavMesh() before spawning
    public float navmeshWaitTimeout = 10f;       // seconds to wait for navmesh




    [System.Serializable] public class Entry { public GameObject prefab; public int count = 5; }

    public Transform center;          // if null, uses this.transform
    public float radius = 120f;       // spawn within this radius
    public List<Entry> entries = new();
    public LayerMask groundMask = ~0;

    [Header("NavMesh")]
    public float sampleRadius = 6f;   // how far to search for navmesh point

    [ContextMenu("Spawn Now")]
    



    void Start()
    {
        if (spawnOnStart && Application.isPlaying)
            StartCoroutine(SpawnWhenNavmeshReady());
    }

    IEnumerator SpawnWhenNavmeshReady()
    {
        if (rebuildNavMeshFirst)
        {
            foreach (var s in FindObjectsOfType<NavMeshSurface>(true))
                s.BuildNavMesh();
        }

        // brief settle delay
        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);

        // wait until a NavMesh is actually present
        float t = 0f;
        while (t < navmeshWaitTimeout && !NavMeshIsReady())
        {
            t += Time.deltaTime;
            yield return null;
        }

        // even if not "ready", go ahead (SpawnNow has retries)
        SpawnNow();
    }

 static bool NavMeshIsReady()
    {
        // robust readiness check: triangulation exists OR we can sample anywhere near origin
        var tri = NavMesh.CalculateTriangulation();
        if (tri.vertices != null && tri.vertices.Length > 0) return true;

        // fallback probe near world origin
        return NavMesh.SamplePosition(Vector3.zero, out _, 500f, NavMesh.AllAreas);
    }















    public void SpawnNow()
    {
        Vector3 c = center ? center.position : transform.position;
        int totalRequested = 0, totalSpawned = 0;

        foreach (var e in entries)
        {
            if (!e.prefab || e.count <= 0) continue;

            int spawned = 0;
            totalRequested += e.count;

            for (int i = 0; i < e.count; i++)
            {
                const int MAX_ATTEMPTS = 20;  // retry a few times to find navmesh
                bool ok = false;

                for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
                {
                    Vector3 pos = c + Random.insideUnitSphere * radius;
                    pos.y += 50f; // sample downward into terrain/navmesh

                    if (NavMesh.SamplePosition(pos, out var hit, sampleRadius, NavMesh.AllAreas))
                    {
                        var go = Instantiate(e.prefab, hit.position, Quaternion.Euler(0, Random.Range(0f, 360f), 0), transform);
                        EnsureAgent(go);
                        spawned++;
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    Debug.LogWarning($"[AISpawner] Could not find NavMesh for {e.prefab.name} after {MAX_ATTEMPTS} attempts.");
                }
            }

            totalSpawned += spawned;
            Debug.Log($"[AISpawner] {e.prefab.name}: requested {e.count}, spawned {spawned}.");
        }

        Debug.Log($"[AISpawner] Done. Requested {totalRequested}, spawned {totalSpawned} within radius {radius}.");
    }

    void EnsureAgent(GameObject go)
    {
        var agent = go.GetComponent<NavMeshAgent>();
        if (!agent) agent = go.AddComponent<NavMeshAgent>();
        agent.stoppingDistance = 0.5f;
        agent.speed = 3.5f;       // animals can be lower, enemies higher; override in prefab if needed
        agent.angularSpeed = 360f;
        agent.acceleration = 16f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = true;
    }
}

