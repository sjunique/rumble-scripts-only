 

using UnityEngine;
using UnityEngine.AI;

public class SimpleWaypointGuide : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform[] waypoints; // Assign in inspector: questgiver, collectibles, etc.
    public float waypointReachDistance = 1.5f;
    
    [Header("Visual Guides")]
    public GameObject waypointMarkerPrefab; // Simple arrow or star
    public TrailRenderer pathTrail; // Visual path
    
    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private GameObject currentMarker;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetNextWaypoint(0);
    }

    void SetNextWaypoint(int index)
    {
        currentWaypointIndex = index;
        
        // Remove previous marker
        if (currentMarker != null) Destroy(currentMarker);
        
        // Set new destination
        agent.SetDestination(waypoints[currentWaypointIndex].position);
        
        // Create visual marker at waypoint (for kids)
        currentMarker = Instantiate(waypointMarkerPrefab, 
                                 waypoints[currentWaypointIndex].position + Vector3.up, 
                                 Quaternion.identity);
        
        // Update visual path
        if (pathTrail != null)
        {
            pathTrail.transform.position = transform.position;
            pathTrail.Clear();
        }
    }

    void Update()
    {
        // Check if reached current waypoint
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < waypointReachDistance)
        {
            if (currentWaypointIndex < waypoints.Length - 1)
            {
                SetNextWaypoint(currentWaypointIndex + 1);
            }
            else
            {
                // All waypoints complete
                if (currentMarker != null) Destroy(currentMarker);
            }
        }
    }
    
    // Call this when a collectible is picked up to skip to next target
    public void CollectedItem()
    {
        if (currentWaypointIndex < waypoints.Length - 1)
        {
            SetNextWaypoint(currentWaypointIndex + 1);
        }
    }
}
