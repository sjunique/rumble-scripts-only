 
using UnityEngine;
using Invector.vCharacterController.AI;

public class RBAgentDeathFix : MonoBehaviour
{
    vControlAICombat ai;
    Rigidbody rb;
    UnityEngine.AI.NavMeshAgent agent;

    void Awake()
    {
        ai = GetComponent<vControlAICombat>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    // Called via Animation Event or hooked into OnDead if you expose it
    public void OnAIDeath()
    {
        // Wait one frame, then restore our desired state
        StartCoroutine(RestorePhysics());
    }

    System.Collections.IEnumerator RestorePhysics()
    {
        yield return null;
        if (rb) rb.isKinematic = false;
        if (agent)
        {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }
}
