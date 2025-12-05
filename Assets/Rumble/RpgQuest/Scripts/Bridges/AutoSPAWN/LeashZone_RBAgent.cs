 



using UnityEngine;
using UnityEngine.AI;
using Invector.vCharacterController;            // vHeadTrack
using Invector.vCharacterController.AI;         // vControlAICombat
using Invector.vCharacterController.vActions;   // (safe to include)
using Invector;   // vHealthController

[RequireComponent(typeof(Collider))]
public class LeashZone_RBAgent : MonoBehaviour
{
    [Header("Return")]
    public float stoppingDistance = 0.3f;
    public float inwardNudge = 0.2f;
    public float raycastHeight = 20f;
    public float surfaceOffset = 0.02f;
    public LayerMask groundMask = ~0;
    public float reattachSearchRadius = 3f;     // how far we search to re-warp onto NavMesh

    [Header("Death Handling")]
    public bool respectDeath = true;            // if true: do nothing when AI is dead (prevents "walk on place")
    public bool restorePhysicsAfterDeath = false; // if true: put RB/Agent back one frame after death

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        // Get AI & agent
        var ai    = other.GetComponentInParent<vControlAICombat>();
        var agent = other.GetComponentInParent<NavMeshAgent>();
        var hc    = other.GetComponentInParent<vHealthController>();
        if (!ai || !agent) return;

        // --- Death guard ---
        if (hc && hc.isDead)
        {
            if (respectDeath)
            {
                // Stop steering dead bodies; avoid walk-in-place
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                return;
            }
            else if (restorePhysicsAfterDeath)
            {
                // Optional: re-enable RB/Agent one frame after death (if some other script flipped them)
                ai.StartCoroutine(RestoreAfterDeath(agent, other.GetComponentInParent<Rigidbody>()));
                return;
            }
        }

        // Ensure agent is enabled & on a NavMesh triangle
        if (!agent.enabled) agent.enabled = true;
        if (!EnsureAgentOnNavMesh(agent, reattachSearchRadius, groundMask))
            return; // can't steer safely

        // Compute a point just inside the leash edge
        Vector3 dest = ClosestPointInside(other.transform.position, inwardNudge);

        // Snap that dest to ground and then onto NavMesh
        dest = SnapToGround(dest, raycastHeight, surfaceOffset, groundMask);
        dest = SampleOnNavMesh(dest, 2f, agent.areaMask, dest);

        // Clear head-look so it stops staring at the player
        ClearHeadTrack(ai);

        // Face the return direction (turn-back so no backward glide)
      
 


// ✅ Let NavMeshAgent own rotation entirely
if (agent.isOnNavMesh)
{
    agent.stoppingDistance = Mathf.Max(0.3f, stoppingDistance);   // slightly larger
    agent.updatePosition = true;
    agent.updateRotation = true;          // agent rotates toward path
    agent.isStopped = false;
    agent.SetDestination(dest);
}






    }



System.Collections.IEnumerator TurnToward(Transform t, Vector3 fwd, float maxDegPerSec, float maxTime)
{
    Quaternion target = Quaternion.LookRotation(fwd, Vector3.up);
    float elapsed = 0f;
    while (elapsed < maxTime)
    {
        t.rotation = Quaternion.RotateTowards(t.rotation, target, maxDegPerSec * Time.deltaTime);
        if (Quaternion.Angle(t.rotation, target) < 0.5f) break;
        elapsed += Time.deltaTime;
        yield return null;
    }
    t.rotation = target;
}
    // ------- helpers -------

    bool EnsureAgentOnNavMesh(NavMeshAgent agent, float searchRadius, LayerMask gMask)
    {
        if (agent.isOnNavMesh) return true;

        Vector3 p = agent.transform.position;

        // Try sampling near current position
        if (NavMesh.SamplePosition(p, out var hit, searchRadius, agent.areaMask))
        {
            agent.Warp(hit.position + Vector3.up * 0.001f);
            return agent.isOnNavMesh;
        }

        // Try ray to ground then sample there
        if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var gh, 5f, gMask, QueryTriggerInteraction.Ignore))
        {
            var g = gh.point + Vector3.up * 0.02f;
            if (NavMesh.SamplePosition(g, out hit, searchRadius, agent.areaMask))
            {
                agent.Warp(hit.position + Vector3.up * 0.001f);
                return agent.isOnNavMesh;
            }
        }
        return false;
    }

Vector3 ClosestPointInside(Vector3 worldPos, float nudge)
{
    var col = GetComponent<Collider>();
    Vector3 closest = col.ClosestPoint(worldPos);
    Vector3 center  = col.bounds.center;

    // Direction from boundary toward the center
    Vector3 dir = (center - closest);
    if (dir.sqrMagnitude < 0.0001f) dir = Vector3.zero;
    else dir.Normalize();

    // Move at least 'nudge' meters inward, but also a bit toward center
    float inward = Mathf.Max(0.5f, nudge);
    Vector3 target = closest + dir * inward;

    // Keep same height; we'll raycast down later
    target.y = closest.y;
    return target;
}


    Vector3 SnapToGround(Vector3 pos, float upHeight, float offset, LayerMask mask)
    {
        if (Physics.Raycast(pos + Vector3.up * upHeight, Vector3.down,
            out var hit, upHeight * 2f, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * offset;
        }
        return pos;
    }

    Vector3 SampleOnNavMesh(Vector3 pos, float radius, int areaMask, Vector3 fallback)
    {
        return NavMesh.SamplePosition(pos, out var hit, radius, areaMask) ? hit.position : fallback;
    }

    void ClearHeadTrack(vControlAICombat ai)
    {
        var ht = ai.GetComponent<vHeadTrack>();
        if (!ht) return;

        if (ht.currentLookTarget != null)
            ht.RemoveLookTarget(ht.currentLookTarget.transform);

        // Freeze for one frame so it doesn't instantly re-acquire
        ht.freezeLookPoint = true;
        ai.StartCoroutine(UnfreezeNextFrame(ht));
    }

    System.Collections.IEnumerator UnfreezeNextFrame(vHeadTrack ht)
    {
        yield return null;
        ht.freezeLookPoint = false;
    }

    System.Collections.IEnumerator RestoreAfterDeath(NavMeshAgent agent, Rigidbody rb)
    {
        yield return null; // wait one frame to let death toggles run
        if (rb) rb.isKinematic = false;
        if (agent)
        {
            agent.enabled = true;
            // Reattach safely if off-mesh
            EnsureAgentOnNavMesh(agent, reattachSearchRadius, groundMask);
        }
    }
}

























/*
[RequireComponent(typeof(Collider))]
public class LeashZone_RBAgent : MonoBehaviour
{
    [Header("Return")]
    public float stoppingDistance = 0.3f;
    public float inwardNudge = 0.2f;
    public float raycastHeight = 20f;
    public float surfaceOffset = 0.02f;
    public LayerMask groundMask = ~0;
    public float reattachSearchRadius = 3f;   // how far to look to re-warp onto NavMesh

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        var ai    = other.GetComponentInParent<vControlAICombat>();
        var agent = other.GetComponentInParent<NavMeshAgent>();
        if (!ai || !agent) return;

        // 0) Ensure agent is enabled and placed on NavMesh
        if (!agent.enabled) agent.enabled = true;
        if (!EnsureAgentOnNavMesh(agent, reattachSearchRadius))
        {
            // Can't control this AI right now; bail safely
            return;
        }

        // 1) Compute a point just inside the zone
        Vector3 dest = ClosestPointInside(other.transform.position, inwardNudge);

        // 2) Snap destination to ground and NavMesh
        dest = SnapToGround(dest, raycastHeight, surfaceOffset);
        dest = SampleOnNavMesh(dest, 2f, agent.areaMask, dest); // keep same if sampling fails

        // 3) Clear head look so it stops staring at the player
        ClearHeadTrack(ai);

        // 4) Steer with agent (only if still on NavMesh)
        if (agent.isOnNavMesh)
        {
            agent.stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;            // safe now
            agent.SetDestination(dest);   
            if (agent.isOnNavMesh)
{
    Vector3 dir = (dest - ai.transform.position).normalized;
    if (dir.sqrMagnitude > 0.01f)
        ai.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

    agent.SetDestination(dest);
}


        
                  // safe now
        }
    }

    bool EnsureAgentOnNavMesh(NavMeshAgent agent, float searchRadius)
    {
        // If agent is already properly placed, we're done
        if (agent.isOnNavMesh) return true;

        // Try to sample near the agent's current transform position
        var pos = agent.transform.position;
        if (NavMesh.SamplePosition(pos, out var hit, searchRadius, agent.areaMask))
        {
            // Warp places the agent “onto” the mesh instantly
            agent.Warp(hit.position + Vector3.up * 0.001f);
            return agent.isOnNavMesh;
        }

        // Last resort: slightly downward ray to find ground, then sample there
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var groundHit, 5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            var p = groundHit.point + Vector3.up * 0.02f;
            if (NavMesh.SamplePosition(p, out hit, searchRadius, agent.areaMask))
            {
                agent.Warp(hit.position + Vector3.up * 0.001f);
                return agent.isOnNavMesh;
            }
        }
        return false;
    }

    Vector3 ClosestPointInside(Vector3 worldPos, float nudge)
    {
        var col = GetComponent<Collider>();
        var p = col.ClosestPoint(worldPos);
        Vector3 center = col.bounds.center;
        Vector3 dir = (p - center);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        return p - dir.normalized * Mathf.Max(0f, nudge);
    }

    Vector3 SnapToGround(Vector3 pos, float upHeight, float offset)
    {
        if (Physics.Raycast(pos + Vector3.up * upHeight, Vector3.down,
            out var hit, upHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * offset;
        }
        return pos;
    }

    Vector3 SampleOnNavMesh(Vector3 pos, float radius, int areaMask, Vector3 fallback)
    {
        if (NavMesh.SamplePosition(pos, out var hit, radius, areaMask))
            return hit.position;
        return fallback;
    }

    void ClearHeadTrack(vControlAICombat ai)
    {
        var ht = ai.GetComponent<vHeadTrack>();
        if (!ht) return;

        if (ht.currentLookTarget != null)
            ht.RemoveLookTarget(ht.currentLookTarget.transform);

        ht.freezeLookPoint = true;
        ai.StartCoroutine(UnfreezeNextFrame(ht));
    }

    System.Collections.IEnumerator UnfreezeNextFrame(vHeadTrack ht)
    {
        yield return null;
        ht.freezeLookPoint = false;
    }
}
 */