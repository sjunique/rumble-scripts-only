using UnityEngine;
using UnityEngine.AI;

public class AIModeBinder : MonoBehaviour
{
    public Rigidbody rb;
    public NavMeshAgent agent;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        SetAliveMode();
    }

    public void SetAliveMode()
    {
        if (agent) { agent.enabled = true; agent.isStopped = false; }
        if (rb) rb.isKinematic = true;  // Agent drives motion
    }

    public void SetDeadMode()
    {
        if (agent) { agent.isStopped = true; agent.enabled = false; }
        if (rb) rb.isKinematic = false; // physics/ragdoll now allowed
    }
}

