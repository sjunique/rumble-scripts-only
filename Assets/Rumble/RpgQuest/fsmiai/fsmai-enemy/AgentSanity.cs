using UnityEngine;
using UnityEngine.AI;

public class AgentSanity : MonoBehaviour
{
    NavMeshAgent agent;
    Animator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (agent)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
        if (anim) anim.applyRootMotion = false; // Agent drives motion
    }

    void LateUpdate()
    {
        // Re-enforce in case some script toggles them mid-frame
        if (!agent) return;
        if (!agent.updatePosition) agent.updatePosition = true;
        if (!agent.updateRotation) agent.updateRotation = true;
    }
}
