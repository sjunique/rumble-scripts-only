using UnityEngine;
using UnityEngine.AI;

public class ShooterPhysicsMode : MonoBehaviour
{
    NavMeshAgent agent;
    Rigidbody rb;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();
        if (agent) { agent.updatePosition = false; agent.updateRotation = false; }
        if (rb) rb.isKinematic = false;
    }
    void LateUpdate()
    {
        if (agent && (agent.updatePosition || agent.updateRotation))
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }
}
