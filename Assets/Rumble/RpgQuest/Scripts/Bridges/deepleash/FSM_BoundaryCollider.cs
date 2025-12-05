 

using UnityEngine;
using UnityEngine.AI;

public class FSM_BoundaryCollider : MonoBehaviour
{
    [Header("FSM Boundary Settings")]
    public bool isActive = true;
    public bool carveNavMesh = true;
    
    private NavMeshObstacle navMeshObstacle;

    void Start()
    {
        // Setup NavMesh Obstacle for automatic pathfinding avoidance
        if (carveNavMesh)
        {
            navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
            ConfigureObstacle();
        }
        
        // Set boundary layer
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        
        // Ensure collider is trigger for detection
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false; // Keep as solid collider
        }
    }

    void ConfigureObstacle()
    {
        Collider collider = GetComponent<Collider>();
        
        if (collider is BoxCollider boxCollider)
        {
            navMeshObstacle.shape = NavMeshObstacleShape.Box;
            navMeshObstacle.size = boxCollider.size;
            navMeshObstacle.center = boxCollider.center;
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
            navMeshObstacle.radius = capsuleCollider.radius;
            navMeshObstacle.height = capsuleCollider.height;
            navMeshObstacle.center = capsuleCollider.center;
        }
        
        navMeshObstacle.carving = true;
        navMeshObstacle.carveOnlyStationary = true;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (navMeshObstacle != null)
        {
            navMeshObstacle.carving = active;
        }
    }
}
