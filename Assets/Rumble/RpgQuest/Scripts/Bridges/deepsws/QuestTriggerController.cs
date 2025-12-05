using UnityEngine;
using Invector.vCharacterController;

public class QuestTriggerController : MonoBehaviour
{
    [Header("Quest Configuration")]
    public string questId = "MainQuest";
    public bool questAccepted = false;
    
    [Header("References")]
    public GameObject waypointPath; // Reference to your SWS path object
    public GameObject collectiblesContainer; // Parent object of all collectibles
    public GameObject triggerVisual; // The visible gizmo representation
    
    [Header("Auto-Pilot Settings")]
    public float followSpeed = 3f;
    public float rotationSpeed = 5f;
    public float reachDistance = 1.5f;
    
    private vThirdPersonController playerController;
    private bool isFollowingPath = false;
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    
    void Start()
    {
        // Initially disable the path and collectibles
        if (waypointPath != null) waypointPath.SetActive(false);
        if (collectiblesContainer != null) collectiblesContainer.SetActive(false);
        if (triggerVisual != null) triggerVisual.SetActive(true);
        
        // Find player controller
        playerController = FindObjectOfType<vThirdPersonController>();
        
        // If quest is already accepted, activate the trigger
        if (questAccepted)
        {
            ActivateQuestTrigger();
        }
    }
    
    void Update()
    {
        if (isFollowingPath && playerController != null)
        {
            FollowWaypoints();
        }
    }
    
    // Call this method when the quest is accepted
    public void OnQuestAccepted(string acceptedQuestId)
    {
        if (acceptedQuestId == questId)
        {
            questAccepted = true;
            ActivateQuestTrigger();
        }
    }
    
    void ActivateQuestTrigger()
    {
        // Enable the trigger visual
        if (triggerVisual != null) triggerVisual.SetActive(true);
        
        // Enable the collider
        GetComponent<Collider>().enabled = true;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && questAccepted)
        {
            // Activate the waypoint path and collectibles
            if (waypointPath != null) 
            {
                waypointPath.SetActive(true);
                InitializeWaypoints();
            }
            
            if (collectiblesContainer != null) collectiblesContainer.SetActive(true);
            
            // Disable the trigger visual and collider
            if (triggerVisual != null) triggerVisual.SetActive(false);
            GetComponent<Collider>().enabled = false;
            
            // Start following the path
            isFollowingPath = true;
            
            // Disable player input for auto-pilot
            if (playerController != null)
            {
                playerController.lockMovement = true;
                playerController.lockRotation = true;
            }
        }
    }
    
    void InitializeWaypoints()
    {
        // Get waypoints from SWS path
        WaypointPathVisualizer pathVisualizer = waypointPath.GetComponent<WaypointPathVisualizer>();
        if (pathVisualizer != null)
        {
            // Get the waypoints from the visualizer
            // This might need adjustment based on your SWS setup
            Transform pointsRoot = pathVisualizer.pointsRoot;
            waypoints = new Transform[pointsRoot.childCount];
            for (int i = 0; i < pointsRoot.childCount; i++)
            {
                waypoints[i] = pointsRoot.GetChild(i);
            }
        }
        
        currentWaypointIndex = 0;
    }
    
    void FollowWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0 || currentWaypointIndex >= waypoints.Length)
        {
            // Path completed
            isFollowingPath = false;
            
            // Restore player control
            if (playerController != null)
            {
                playerController.lockMovement = false;
                playerController.lockRotation = false;
            }
            return;
        }
        
        // Get current waypoint
        Transform currentWaypoint = waypoints[currentWaypointIndex];
        
        // Calculate direction to waypoint
        Vector3 direction = (currentWaypoint.position - playerController.transform.position).normalized;
        direction.y = 0; // Keep movement horizontal
        
        // Move towards waypoint
        playerController.transform.position += direction * followSpeed * Time.deltaTime;
        
        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            playerController.transform.rotation = Quaternion.Slerp(
                playerController.transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
        
        // Check if reached waypoint
        float distanceToWaypoint = Vector3.Distance(playerController.transform.position, currentWaypoint.position);
        if (distanceToWaypoint < reachDistance)
        {
            currentWaypointIndex++;
        }
    }
    
    // Visualize the trigger in the editor
    void OnDrawGizmos()
    {
        if (!questAccepted)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawIcon(transform.position + Vector3.up, "quest_trigger.png", true);
        }
    }
}
