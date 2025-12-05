// Assets/Scripts/AI/Brains/AnimalAI.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalAI : MonoBehaviour
{
    public Transform player;
    public float detectionRadius = 18f;
    public float fleeDistance = 25f;
    public Vector2 idleWanderRadius = new Vector2(6f, 16f);
    public float idleWaitMin = 2f, idleWaitMax = 6f;

    enum State { Idle, Eat, Flee }
    State state;
    NavMeshAgent agent;
    float stateTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        state = State.Idle;
        stateTimer = Random.Range(idleWaitMin, idleWaitMax);
    }

    void Update()
    {
        if (!player) { var ps = FindAnyObjectByType<PlayerStatus>(); if (ps) player = ps.transform; }

        float dist = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        if (dist < detectionRadius) EnterFlee();

        switch (state)
        {
            case State.Idle:
            case State.Eat:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    // choose a local wander point
                    Vector3 rnd = Random.insideUnitCircle * Random.Range(idleWanderRadius.x, idleWanderRadius.y);
                    Vector3 dest = transform.position + new Vector3(rnd.x, 0, rnd.y);
                    if (NavMesh.SamplePosition(dest, out var hit, 5f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);

                    state = (state == State.Idle) ? State.Eat : State.Idle;
                    stateTimer = Random.Range(idleWaitMin, idleWaitMax);
                }
                break;

            case State.Flee:
                if (dist > detectionRadius * 2f && agent.remainingDistance < 0.5f)
                {
                    state = State.Idle;
                }
                break;
        }
    }

    void EnterFlee()
    {
        state = State.Flee;
        // Flee opposite of player, add some randomness
        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 goal = transform.position + dir * fleeDistance + Random.insideUnitSphere * 4f;
        if (NavMesh.SamplePosition(goal, out var hit, 8f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        agent.speed = Mathf.Max(agent.speed, 4.5f);
    }
}

