using UnityEngine;

using System.Collections;
using System.Collections.Generic;
 
using UnityEngine.AI;
using Invector.vCharacterController.AI;
using Invector.vCharacterController;
 
 
 

[RequireComponent(typeof(Collider))]
public class LeashZone_SimpleSlideToCenter : MonoBehaviour
{
[Header("Guard Ring")]
[Tooltip("Radius around leash center where each AI will be parked after leash.")]
public float guardRingRadius = 2f;    

    [Header("Post-slide Behavior")]
[Tooltip("Time after reaching center during which AI stays frozen (motor/agent disabled).")]
public float stunDuration = 1.0f;   
    [Header("Slide Settings")]
    [Tooltip("How long it takes to slide the AI back to the center.")]
    public float slideDuration = 0.5f;

    [Tooltip("Vertical offset above ground for final position.")]
    public float surfaceOffset = 0.02f;

    [Tooltip("Layers considered as ground when raycasting.")]
    public LayerMask groundMask = ~0;

    readonly Dictionary<Transform, Coroutine> _activeSlides = new Dictionary<Transform, Coroutine>();

    Collider _collider;

    void Reset()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (!_collider.isTrigger)
            _collider.isTrigger = true;
    }

 
void OnTriggerExit(Collider other)
{
    var ai = other.GetComponentInParent<vControlAICombat>();
    if (!ai) return;

    var root = ai.transform;

    // 1) base point = leash center
    Vector3 center = GetLeashCenter();

    // 2) compute a unique offset for this AI (based on instance id)
    Vector3 offset = ComputeGuardOffset(root, guardRingRadius);

    // 3) target = center + offset, snapped to ground
    Vector3 rawTarget = center + offset;
    Vector3 target    = SnapToGround(rawTarget, surfaceOffset);

    if (_activeSlides.TryGetValue(root, out var co) && co != null)
        StopCoroutine(co);

    var newCo = StartCoroutine(SlideAIToCenter(ai, target));
    _activeSlides[root] = newCo;
}




Vector3 ComputeGuardOffset(Transform root, float radius)
{
    // Use instance id to get a stable pseudo-random angle per AI
    int hash = root.GetInstanceID();
    // map hash to [0,1)
    float u = Mathf.Abs(hash * 0.6180339887f);   // golden ratio scramble
    u = u - Mathf.Floor(u);

    float angle = u * Mathf.PI * 2f;
    Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

    return dir * radius;
}









    Vector3 GetLeashCenter()
    {
        // center of collider in world space
        if (_collider is BoxCollider box)
            return box.transform.TransformPoint(box.center);   // handles size/rotation
        else
            return _collider.bounds.center;                    // world-space already
    }

    Vector3 SnapToGround(Vector3 pos, float offset)
    {
        var start = pos + Vector3.up * 10f;
        if (Physics.Raycast(start, Vector3.down, out var hit, 30f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * offset;

        return pos;
    }





IEnumerator SlideAIToCenter(vControlAICombat ai, Vector3 targetPos)
{
    var root  = ai.transform;
    var rb    = root.GetComponent<Rigidbody>();
    var agent = root.GetComponent<NavMeshAgent>();
    var head  = root.GetComponent<vHeadTrack>();
    var motor = root.GetComponent<vAIMotor>();

    // we no longer toggle agent.enabled
    bool motorWasEnabled = motor != null && motor.enabled;

    // stop physics movement but DO NOT change isKinematic
    if (rb)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // temporarily disable motor so it doesn't try to move while we slide
    if (motorWasEnabled)
        motor.enabled = false;

    // optionally freeze head tracking
    if (head)
    {
        if (head.currentLookTarget)
            head.RemoveLookTarget(head.currentLookTarget.transform);

        head.freezeLookPoint = true;
    }

    Vector3 startPos = root.position;
    float elapsed = 0f;

    while (elapsed < slideDuration)
    {
        float t = elapsed / slideDuration;
        t = t * t * (3f - 2f * t); // smoothstep
        root.position = Vector3.Lerp(startPos, targetPos, t);

        elapsed += Time.deltaTime;
        yield return null;
    }

    root.position = targetPos;

    // optional stun
    if (stunDuration > 0f)
        yield return new WaitForSeconds(stunDuration);

    // restore motor & head
    if (motor)
        motor.enabled = motorWasEnabled;

    if (head)
        head.freezeLookPoint = false;

    _activeSlides.Remove(root);
}










IEnumerator SlideAIToCenters(vControlAICombat ai, Vector3 targetPos)
{
    var root  = ai.transform;
    var rb    = root.GetComponent<Rigidbody>();
    var agent = root.GetComponent<NavMeshAgent>();
    var head  = root.GetComponent<vHeadTrack>();
    var motor = root.GetComponent<vAIMotor>();

    // cache original enabled states
    bool hadAgent        = agent != null;
    bool agentWasEnabled = hadAgent && agent.enabled;
    bool motorWasEnabled = motor != null && motor.enabled;

    // stop physics movement but DO NOT change isKinematic
    if (rb)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // temporarily disable agent & motor so they don't fight the slide
    if (hadAgent && agentWasEnabled)
        agent.enabled = false;

    if (motorWasEnabled)
        motor.enabled = false;

    // optionally freeze head tracking so they don't keep staring at player
    if (head)
    {
        if (head.currentLookTarget)
            head.RemoveLookTarget(head.currentLookTarget.transform);

        head.freezeLookPoint = true;
    }

    Vector3 startPos = root.position;
    float elapsed = 0f;

    while (elapsed < slideDuration)
    {
        float t = elapsed / slideDuration;
        // smoothstep
        t = t * t * (3f - 2f * t);
        root.position = Vector3.Lerp(startPos, targetPos, t);

        elapsed += Time.deltaTime;
        yield return null;
    }

    root.position = targetPos;



     // Face toward leash center (optional)
Vector3 center = GetLeashCenter();
Vector3 dir = center - root.position;
dir.y = 0f;
if (dir.sqrMagnitude > 0.0001f)
    root.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

    // 🧊 STUN PHASE: keep them frozen for stunDuration
    if (stunDuration > 0f)
        yield return new WaitForSeconds(stunDuration);

    // restore components after stun
    if (hadAgent)
        agent.enabled = agentWasEnabled;

    if (motor)
        motor.enabled = motorWasEnabled;

    if (head)
        head.freezeLookPoint = false;

    _activeSlides.Remove(root);
}


}
