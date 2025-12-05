using UnityEngine;

 

public class FishBoid : MonoBehaviour
{
    // Add this to your existing variables
    [Header("Obstacle Avoidance")]
    public float avoidanceForce = 5f;
    public float rayDistance = 3f;
    public LayerMask obstacleMask;
    public float[] rayAngles = { 0f, 45f, -45f, 90f, -90f }; // Multi-directional rays



    [Header("Movement")]
    public float speed = 2f;
    public float rotationSpeed = 5f;
    public float wanderStrength = 0.5f;
    private Vector3 _velocity;

    [Header("Behavior Weights")]
    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public float avoidanceWeight = 3f;
    public float targetWeight = 1f;

    [Header("Detection")]
    public float neighborRadius = 5f;
    public float avoidanceDistance = 3f;
     

    private FishSchoolManager _manager;
    private Vector3 _wanderPoint;
    private float _wanderAngle;

    public FishSchoolManager manager
    {
        get => _manager;
        set => _manager = value;
    }

    void Start()
    {
        _velocity = transform.forward * speed;
        _wanderPoint = Random.onUnitSphere * neighborRadius;
        obstacleMask = LayerMask.GetMask("Obstacles");
    }

    void Update()
    {
        if (_manager == null) return;
        AvoidObstacles(); // Call this in Update or FixedUpdate
        Vector3 acceleration = CalculateMovementForces();
        _velocity = Vector3.Lerp(_velocity, _velocity + acceleration, Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, speed);

        if (_velocity != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_velocity);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        transform.position += _velocity * Time.deltaTime;

        // Debug visualization
        Debug.DrawRay(transform.position, _velocity.normalized * 2f, Color.green);
        Debug.DrawRay(transform.position, transform.forward * avoidanceDistance, Color.red);
    }

    Vector3 CalculateMovementForces()
    {
        Vector3 forces = Vector3.zero;

        // Get nearby fish
        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborRadius);
        int neighborCount = 0;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject == gameObject || !neighbor.CompareTag("Fish"))
                continue;

            FishBoid otherFish = neighbor.GetComponent<FishBoid>();
            if (otherFish == null) continue;

            neighborCount++;

            // Cohesion: Move toward average position
            cohesion += neighbor.transform.position;

            // Separation: Avoid crowding
            Vector3 toNeighbor = transform.position - neighbor.transform.position;
            float distance = toNeighbor.magnitude;
            if (distance < avoidanceDistance)
            {
                separation += toNeighbor.normalized / distance;
            }

            // Alignment: Match velocity
            alignment += otherFish._velocity;
        }

        if (neighborCount > 0)
        {
            cohesion = (cohesion / neighborCount - transform.position).normalized * cohesionWeight;
            separation = (separation / neighborCount).normalized * separationWeight;
            alignment = (alignment / neighborCount).normalized * alignmentWeight;
        }

        forces += cohesion + separation + alignment;

        // Obstacle avoidance (highest priority)
        Vector3 avoidance = CalculateAvoidance();
        forces += avoidance * avoidanceWeight;

        // School pattern following
        if (_manager != null)
        {
            Vector3 targetDir = (_manager.GetSchoolPatternTarget(this) - transform.position).normalized;
            forces += targetDir * targetWeight;
        }

        // Random wandering
        _wanderAngle += Random.Range(-30f, 30f) * wanderStrength;
        _wanderPoint = Quaternion.Euler(0, _wanderAngle, 0) * Vector3.forward * neighborRadius;
        forces += _wanderPoint.normalized * wanderStrength;

        return forces;
    }

    Vector3 CalculateAvoidance()
    {
        Vector3 avoidanceForce = Vector3.zero;
        int avoidanceCount = 0;

        // Forward raycast
        if (Physics.Raycast(transform.position, transform.forward, avoidanceDistance, obstacleMask))
        {
            avoidanceForce += -transform.forward * 2f;
            avoidanceCount++;
        }

        // Downward raycast (for ground/plane avoidance)
        if (Physics.Raycast(transform.position, -transform.up, avoidanceDistance * 0.5f, obstacleMask))
        {
            avoidanceForce += Vector3.up * 2f;
            avoidanceCount++;
        }

        // Sphere cast for general obstacles
        Collider[] obstacles = Physics.OverlapSphere(transform.position, avoidanceDistance * 0.8f, obstacleMask);
        foreach (var obstacle in obstacles)
        {
            if (obstacle.gameObject != gameObject)
            {
                Vector3 dirToObstacle = transform.position - obstacle.transform.position;
                avoidanceForce += dirToObstacle.normalized * (avoidanceDistance - dirToObstacle.magnitude);
                avoidanceCount++;
            }
        }

        return avoidanceCount > 0 ? avoidanceForce / avoidanceCount : Vector3.zero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceDistance * 0.8f);
    }
    

    void AvoidObstacles()
    {
        Vector3 avoidanceDirection = Vector3.zero;
        int hits = 0;

        // Cast multiple rays in different directions
        foreach (float angle in rayAngles)
        {
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            RaycastHit hit;
            
            if (Physics.Raycast(transform.position, dir, out hit, rayDistance, obstacleMask))
            {
                Debug.DrawRay(transform.position, dir * hit.distance, Color.red, 0.1f);
                avoidanceDirection += (hit.point - transform.position).normalized * avoidanceForce;
                hits++;
            }
            else
            {
                Debug.DrawRay(transform.position, dir * rayDistance, Color.green, 0.1f);
            }
        }

        // Additional downward ray for ground/plane avoidance
        if (Physics.Raycast(transform.position, Vector3.down, rayDistance * 0.5f, obstacleMask))
        {
            avoidanceDirection += Vector3.up * avoidanceForce;
            hits++;
        }

        // Apply avoidance force if any hits
        if (hits > 0)
        {
            GetComponent<Rigidbody>().AddForce(-avoidanceDirection / hits, ForceMode.Acceleration);
        }
    }


}
/*
public class FishBoid : MonoBehaviour
{
    public FishSchoolManager manager;

    Vector3 velocity;
    float speed = 2f;
    float neighborRadius = 3f;
    float separationRadius = 1f;
    void Awake() { Debug.Log(name + " FishBoid Awake!"); }
    void Update()
    {


        if (manager == null)
        {
            Debug.LogWarning(name + " FishBoid missing manager!", this);
            return;
        }
        Vector3 separation = Vector3.zero, alignment = Vector3.zero, cohesion = Vector3.zero;
        int count = 0;

        // rest of your flocking code...
        foreach (var fish in manager.fishList)
        {
            if (fish == null || fish == this || !fish.gameObject.activeSelf) continue;

            float dist = Vector3.Distance(transform.position, fish.transform.position);

            // Separation
            if (dist < separationRadius)
                separation += (transform.position - fish.transform.position) / dist;

            // Cohesion + Alignment
            if (dist < neighborRadius)
            {
                cohesion += fish.transform.position;
                alignment += fish.velocity;
                count++;
            }
        }


        if (count > 0)
        {
            cohesion = (cohesion / count - transform.position).normalized;
            alignment = (alignment / count).normalized;
        }

        Vector3 dir = separation + cohesion + alignment + Random.insideUnitSphere * 0.1f;
        velocity = Vector3.Lerp(velocity, dir.normalized * speed, Time.deltaTime);

        transform.position += velocity * Time.deltaTime;
        if (velocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * 2f);

        // Keep fish within a certain radius of the manager (school center)
        Vector3 offset = transform.position - manager.transform.position;
        if (offset.magnitude > manager.spawnRadius)
            velocity -= offset.normalized * speed * 0.2f;
    }
}
*/
