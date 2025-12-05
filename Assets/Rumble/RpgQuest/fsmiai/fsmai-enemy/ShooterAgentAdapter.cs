using UnityEngine;

using UnityEngine;
using UnityEngine.AI;

/// Keeps NavMeshAgent updates alive even when the shooter AI switches to aim/strafe mode.
[RequireComponent(typeof(NavMeshAgent))]
public class ShooterAgentAdapter : MonoBehaviour
{
    NavMeshAgent agent;
    Animator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();
        if (anim) anim.applyRootMotion = false;
        if (agent)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
    }

    void LateUpdate()
    {
        // Some Invector shooter routines set these false every frame while aiming.
        // Force them back so the agent continues to move.
        if (!agent) return;
        if (!agent.updatePosition) agent.updatePosition = true;
        if (!agent.updateRotation) agent.updateRotation = true;
    }
}
