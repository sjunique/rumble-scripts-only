using UnityEngine;
using UnityEngine.AI;

/// Keeps the NavMeshAgent usable and glued to the mesh.
[DefaultExecutionOrder(10000)]
public class NavMeshAgentStabilizer : MonoBehaviour
{
    public float resampleRadius = 2f;   // how far to search for mesh under you
    public bool forceEnabled = true;    // if some script disables the agent, turn it back on
    public bool autoWarpIfOffMesh = true;

    NavMeshAgent agent;
    Rigidbody rb;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (!agent) return;

        // If someone disabled it, re-enable (common in controller scripts)
        if (forceEnabled && !agent.enabled)
            agent.enabled = true;

        // If we’re not on the mesh (physics push, step offset, root motion), snap back
        if (autoWarpIfOffMesh && agent.enabled && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, resampleRadius, NavMesh.AllAreas))
                agent.Warp(hit.position + Vector3.up * 0.01f);
        }
    }
}
