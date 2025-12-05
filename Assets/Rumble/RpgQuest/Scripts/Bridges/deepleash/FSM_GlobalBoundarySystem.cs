 
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class FSM_GlobalBoundarySystem : MonoBehaviour
{
    public static FSM_GlobalBoundarySystem Instance;
    
    [Header("Global Boundary Management")]
    public bool enableBoundaryManagement = true;
    
    private List<FSM_BoundaryCollider> boundaries = new List<FSM_BoundaryCollider>();
    private List<FSM_AIBoundaryManager> managedAI = new List<FSM_AIBoundaryManager>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterBoundary(FSM_BoundaryCollider boundary)
    {
        if (!boundaries.Contains(boundary))
        {
            boundaries.Add(boundary);
        }
    }

    public void RegisterAI(FSM_AIBoundaryManager aiManager)
    {
        if (!managedAI.Contains(aiManager))
        {
            managedAI.Add(aiManager);
        }
    }

    public bool IsPositionInBoundary(Vector3 position)
    {
        foreach (var boundary in boundaries)
        {
            if (boundary != null && boundary.isActive)
            {
                Collider collider = boundary.GetComponent<Collider>();
                if (collider != null && collider.bounds.Contains(position))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public Vector3 GetNearestValidPosition(Vector3 fromPosition)
    {
        Vector3 bestPosition = fromPosition;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < 12; i++)
        {
            Vector3 randomDir = Random.onUnitSphere * 5f;
            randomDir.y = 0;
            Vector3 testPos = fromPosition + randomDir;

            if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                if (!IsPositionInBoundary(hit.position))
                {
                    float dist = Vector3.Distance(fromPosition, hit.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestPosition = hit.position;
                    }
                }
            }
        }

        return bestPosition;
    }
}
