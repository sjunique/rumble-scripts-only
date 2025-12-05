 

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Invector.vCharacterController.AI;
using Invector;
public class FSM_AIBoundaryManager : MonoBehaviour
{
    [Header("FSM AI Boundary Settings")]
    public LayerMask boundaryLayer = 1 << 8;
    public float boundaryCheckInterval = 0.3f;
    public float boundaryAvoidDistance = 3f;
    public float recoveryDelay = 2f;
    
    private NavMeshAgent navMeshAgent;
    private vControlAIMelee fsmController;
    private Coroutine boundaryCheckCoroutine;
    private bool isRecoveringFromBoundary = false;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        fsmController = GetComponent<vControlAIMelee>();
        
        if (navMeshAgent != null)
        {
            boundaryCheckCoroutine = StartCoroutine(BoundaryCheckRoutine());
        }
    }

    void OnDestroy()
    {
        if (boundaryCheckCoroutine != null)
            StopCoroutine(boundaryCheckCoroutine);
    }

    private IEnumerator BoundaryCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(boundaryCheckInterval);
            
            if (!isRecoveringFromBoundary && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                CheckForBoundaryIssues();
            }
        }
    }

    private void CheckForBoundaryIssues()
    {
        // Check if path is blocked by boundary
        if (navMeshAgent.hasPath && navMeshAgent.remainingDistance < boundaryAvoidDistance)
        {
            CheckPathForBoundaries();
        }
        
        // Check if stuck near boundary
        if (IsStuckNearBoundary())
        {
            HandleBoundaryStuck();
        }
    }

    private void CheckPathForBoundaries()
    {
        if (navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial ||
            navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            // Path is blocked, try to find alternative
            StartCoroutine(HandleBlockedPath());
        }
    }

    private bool IsStuckNearBoundary()
    {
        // Check if AI is near boundary and not moving
        if (navMeshAgent.velocity.magnitude < 0.1f && navMeshAgent.hasPath)
        {
            return Physics.CheckSphere(transform.position, 2f, boundaryLayer);
        }
        return false;
    }

    private IEnumerator HandleBlockedPath()
    {
        if (isRecoveringFromBoundary) yield break;
        
        isRecoveringFromBoundary = true;
        
        // Store current destination
        Vector3 originalDestination = navMeshAgent.destination;
        
        // Temporarily disable agent to clear path
        navMeshAgent.isStopped = true;
        yield return new WaitForSeconds(0.1f);
        
        // Find alternative position
        Vector3 alternativePosition = FindAlternativePosition(originalDestination);
        
        if (alternativePosition != Vector3.zero)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(alternativePosition);
        }
        else
        {
            // If no alternative found, wait and retry original destination
            yield return new WaitForSeconds(recoveryDelay);
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(originalDestination);
            }
        }
        
        yield return new WaitForSeconds(1f);
        isRecoveringFromBoundary = false;
    }

    private void HandleBoundaryStuck()
    {
        StartCoroutine(RecoverFromStuck());
    }

    private IEnumerator RecoverFromStuck()
    {
        if (isRecoveringFromBoundary) yield break;
        
        isRecoveringFromBoundary = true;
        
        Vector3 escapePosition = FindEscapePosition();
        if (escapePosition != Vector3.zero && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(escapePosition);
        }
        
        yield return new WaitForSeconds(recoveryDelay);
        isRecoveringFromBoundary = false;
    }

    private Vector3 FindAlternativePosition(Vector3 targetDestination)
    {
        Vector3 directionToTarget = (targetDestination - transform.position).normalized;
        
        // Try positions around the boundary
        Vector3[] testDirections = {
            Quaternion.Euler(0, 45, 0) * directionToTarget,
            Quaternion.Euler(0, -45, 0) * directionToTarget,
            Quaternion.Euler(0, 90, 0) * directionToTarget,
            Quaternion.Euler(0, -90, 0) * directionToTarget,
        };

        foreach (Vector3 direction in testDirections)
        {
            Vector3 testPosition = transform.position + direction * boundaryAvoidDistance;
            if (NavMesh.SamplePosition(testPosition, out NavMeshHit hit, boundaryAvoidDistance, NavMesh.AllAreas))
            {
                if (!Physics.CheckSphere(hit.position, 1f, boundaryLayer))
                {
                    return hit.position;
                }
            }
        }

        return Vector3.zero;
    }

    private Vector3 FindEscapePosition()
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 testPosition = transform.position + direction * 5f;

            if (NavMesh.SamplePosition(testPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                if (!Physics.CheckSphere(hit.position, 1f, boundaryLayer))
                {
                    return hit.position;
                }
            }
        }
        return Vector3.zero;
    }
}
