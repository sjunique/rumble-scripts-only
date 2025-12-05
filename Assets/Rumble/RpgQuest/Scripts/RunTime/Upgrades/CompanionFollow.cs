using UnityEngine;
using UnityEngine.AI;
using System.Collections;
[RequireComponent(typeof(NavMeshAgent))]
public class CompanionFollow : MonoBehaviour
{
    [Header("Target Player")]
    public Transform player;
    [SerializeField] string playerTag = "Player";   // your player must use this tag

    [Header("Behavior")]
    public float followDistance = 2.5f;
    public float repathInterval = 0.2f;

    NavMeshAgent agent;
    Coroutine _trackCo;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        // Start (re)binding to the player when we become active
        if (_trackCo != null) StopCoroutine(_trackCo);
        _trackCo = StartCoroutine(TrackPlayerLoop());
    }

    void OnDisable()
    {
        // Stop the tracking coroutine
        if (_trackCo != null) 
        { 
            StopCoroutine(_trackCo); 
            _trackCo = null; 
        }
        
        // Safely reset path only if agent is valid and on NavMesh
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    IEnumerator TrackPlayerLoop()
    {
        // Lazy-resolve player; retry if they spawn later / respawn
        while (player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
            if (player == null) yield return new WaitForSeconds(0.25f);
        }

        // Follow loop
        var wait = new WaitForSeconds(repathInterval);
        while (enabled)
        {
            // Additional safety checks
            if (player != null && agent != null && agent.isOnNavMesh)
            {
                float d = Vector3.Distance(transform.position, player.position);
                
                // Only set destination if outside follow distance
                if (d > followDistance) 
                {
                    agent.SetDestination(player.position);
                }
                else 
                {
                    // Only reset path if we're actually moving
                    if (agent.hasPath && agent.isActiveAndEnabled)
                    {
                        agent.ResetPath();
                    }
                }
            }
            yield return wait;
        }
    }
}
 