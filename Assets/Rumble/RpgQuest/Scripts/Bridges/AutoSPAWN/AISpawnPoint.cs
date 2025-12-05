using UnityEngine;

 

public class AISpawnPoint : MonoBehaviour
{
    public GameObject prefab;

    [Header("Counts")]
    public int initialCount = 1;
    public int maxAlive = 5;
    public bool respawnEnabled = true;
    public float respawnDelay = 10f;

    [Header("Grounding / Alignment")]
    public LayerMask groundMask = ~0;
    public float raycastHeight = 40f;
    public float surfaceOffset = 0.02f;
    public bool alignToNormal = false;   // flat scenes default
    public float maxSlopeAngle = 55f;

    [Header("NavMesh Nudge")]
    public bool preferNavMesh = true;
    public float navmeshSearchRadius = 2f;

    [Tooltip("Random radius around this point to scatter spawned AIs.")]
    public float spawnRadius = 3f;

    [Tooltip("Try to keep this minimum distance between spawned AIs.")]
    public float minSeparation = 1.5f;





  
    [Tooltip("Pool ID to use in AIPoolManager. Leave blank to use prefab.name.")]
    public string poolIdOrName = "";
    [Tooltip("If true, this point is NOT auto-spawned by AISpawnManager.Update; it is only used by AISpawnActivatorFull.")]
    public bool activatorControlled = false;
    public enum MovementMode
    {
        AgentWithRigidbody,   // <-- your new default: RB non-kinematic + Agent drives
        AgentKinematic,       // previous style (RB kinematic)
        PhysicsMotor          // no agent, RB non-kinematic
    }
    [Header("Movement Mode")]
    public MovementMode movementMode = MovementMode.AgentWithRigidbody;

       public string PoolId()
    {
        if (!string.IsNullOrEmpty(poolIdOrName)) return poolIdOrName;
        return prefab ? prefab.name : "default";
    }





     
}
