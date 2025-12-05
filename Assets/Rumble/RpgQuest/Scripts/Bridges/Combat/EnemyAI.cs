// Assets/Scripts/AI/Brains/EnemyAI.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float detectRadius = 25f;
    public float keepDistance = 1.8f;   // distance to maintain when chasing
    public float fleeDistance = 30f;

    NavMeshAgent agent;
    PlayerStatus playerStatus;
    enum Mode { Idle, Chase, Flee }
    Mode mode;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mode = Mode.Idle;
    }

    void Update()
    {
        if (!player)
        {
            var ps = FindAnyObjectByType<PlayerStatus>();
            if (ps) { player = ps.transform; playerStatus = ps; }
            if (!player) return;
        }

        float d = Vector3.Distance(transform.position, player.position);

        bool hasBodyguard = playerStatus && playerStatus.hasBodyguard;

        if (d < detectRadius)
        {
            if (hasBodyguard) EnterFlee();
            else EnterChase();
        }

        if (mode == Mode.Chase)
        {
            if (d > detectRadius * 1.5f) mode = Mode.Idle;
            else
            {
                Vector3 targetPos = player.position - (player.forward * keepDistance * 0.3f);
                if (NavMesh.SamplePosition(targetPos, out var hit, 2f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }
        else if (mode == Mode.Flee)
        {
            if (d > detectRadius * 2f && agent.remainingDistance < 0.5f) mode = Mode.Idle;
        }
    }

    void EnterChase()
    {
        if (mode == Mode.Flee) return;
        mode = Mode.Chase;
        agent.speed = Mathf.Max(agent.speed, 4.5f);
    }

    void EnterFlee()
    {
        if (mode == Mode.Flee) return;
        mode = Mode.Flee;
        Vector3 away = (transform.position - player.position).normalized * fleeDistance;
        Vector3 dest = transform.position + away + Random.insideUnitSphere * 5f;
        if (NavMesh.SamplePosition(dest, out var hit, 8f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        agent.speed = Mathf.Max(agent.speed, 5.5f);
    }
}
