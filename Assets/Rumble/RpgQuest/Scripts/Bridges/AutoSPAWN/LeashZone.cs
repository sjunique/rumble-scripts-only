using UnityEngine;
using UnityEngine.AI;
using Invector.vCharacterController.AI;           // <- for vControlAI
using Invector;         // <- for vHealthController (if needed)
using System.Collections;
 
 
using Invector.vCharacterController;

[RequireComponent(typeof(Collider))]
public class LeashZone : MonoBehaviour
{
    public bool clearTargetOnReturn = true;
    public bool regenOnReturn = true;
    public float healFraction = 0.25f;
    public bool snapReturn = false;
    public float returnSpeedBoost = 1.2f;
    public float arriveThreshold = 1.0f;
    public float maxReturnSeconds = 5f;

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[LeashZone] OnTriggerExit called with: {other.name}");

        // 👇 NEW: always search on the root/parent in case a child TriggerProxy fired
        var agent = other.GetComponentInParent<NavMeshAgent>();
        if (!agent) { Debug.LogWarning("[LeashZone] No NavMeshAgent found on collider's parents"); return; }

        var ctrl = other.GetComponentInParent<vControlAI>();
        var hc   = other.GetComponentInParent<vHealthController>();
        var rb   = other.attachedRigidbody ? other.attachedRigidbody
                                           : other.GetComponentInParent<Rigidbody>();

        // Compute a point inside the zone
        var dst = ClosestPointInside(other.transform.position);

        if (clearTargetOnReturn && ctrl) ctrl.RemoveCurrentTarget();
        if (regenOnReturn && hc) { int heal = (int)(hc.maxHealth * healFraction); if (heal != 0) hc.ChangeHealth(heal); }

        if (snapReturn)
        {
            if (NavMesh.SamplePosition(dst, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position + Vector3.up * 0.05f);
            return;
        }

        StartCoroutine(ReturnWithAgent(agent, rb, dst, returnSpeedBoost, arriveThreshold, maxReturnSeconds));
    }

    IEnumerator ReturnWithAgent(NavMeshAgent agent, Rigidbody rb, Vector3 dst, float speedBoost, float arrive, float timeout)
    {
        bool hadRB = rb != null;
        bool origKinematic = hadRB ? rb.isKinematic : true;
        bool origUpdPos = agent.updatePosition, origUpdRot = agent.updateRotation, origStopped = agent.isStopped;
        float origSpeed = agent.speed; bool origAutoBraking = agent.autoBraking;

        if (hadRB) rb.isKinematic = true;          // freeze physics during return
        agent.updatePosition = true; agent.updateRotation = true; agent.isStopped = false; agent.autoBraking = true;

        if (!NavMesh.SamplePosition(dst, out var hit, 2f, NavMesh.AllAreas)) hit.position = dst;
        Vector3 deeper = Vector3.Lerp(hit.position, transform.position, 0.35f);
        float minArrive = Mathf.Max(arrive, agent.radius + agent.stoppingDistance + 0.35f);

        agent.speed = Mathf.Max(agent.speed, 3f) * speedBoost;
        agent.SetDestination(deeper);

        float t0 = Time.time;
        while (Time.time - t0 < timeout)
        {
            if (!agent.pathPending && agent.remainingDistance <= minArrive)
            { agent.Warp(deeper + Vector3.up * 0.05f); break; }
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);     // settle

        agent.speed = origSpeed; agent.isStopped = origStopped;
        agent.updatePosition = origUpdPos; agent.updateRotation = origUpdRot; agent.autoBraking = origAutoBraking;
        if (hadRB) rb.isKinematic = origKinematic;
    }

    Vector3 ClosestPointInside(Vector3 worldPos)
    {
        var col = GetComponent<Collider>();
        var p = col.ClosestPoint(worldPos);
        return p + (transform.position - p).normalized * 0.6f; // nudge well inside
    }
}
