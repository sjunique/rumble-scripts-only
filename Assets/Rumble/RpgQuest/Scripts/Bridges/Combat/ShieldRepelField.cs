using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SphereCollider))]
public class ShieldRepelField : MonoBehaviour
{


    // NEW settings (put near your other [Header]s)
    [Header("Launch / Air Toss")]
    public bool launchEnemies = true;          // turn on to yeet into the air
    public float launchOut = 8f;               // horizontal
    public float launchUp = 10f;              // vertical
    public float spinTorque = 6f;              // add some spin
    public float ragdollSeconds = 0.75f;       // physics window for agents
    public float stunSecondsAfter = 0.5f;      // extra AI pause after landing
    public string animTrigger = "Knockback";   // optional Animator trigger




    [Header("State")]
    public bool shieldActive = true;                  // flip this to enable/disable the shield

    [Header("Targets & Filtering")]
    public LayerMask targetLayers = ~0;               // layers you want to repel (Enemies/Animals)
    public string[] targetTags = { "Enemy", "Animal" }; // optional tag filter

    [Header("Repel Physics")]
    public float knockbackForce = 16f;                // impulse power for rigidbodies
    public float upBoost = 2f;                        // extra upward kick
    public ForceMode forceMode = ForceMode.VelocityChange;
    public float perTargetCooldown = 0.35f;           // prevents spam on same target

    [Header("NavMesh Agent Fallback")]
    public float agentStopSeconds = 0.35f;            // brief pause when repelled
    public float agentWarpDistance = 0.75f;           // small warp away from player
    [Header("Taming (optional)")]
    public bool tameOnRepel = true;

    [Header("FX (optional)")]
    public GameObject hitVfxPrefab;
    public float vfxLifetime = 1.0f;
    public float vfxScale = 1f;
    public AudioClip hitSfx;
    public float sfxVolume = 0.8f;

    // cache
    SphereCollider _col;
    Transform _player;
    readonly Dictionary<Collider, float> _lastHitTime = new();

    void Reset()
    {
        _col = GetComponent<SphereCollider>();
        _col.isTrigger = true;
        _col.radius = 2.0f;
    }

    void Awake()
    {
        _col = GetComponent<SphereCollider>();
    }

    void Start()
    {
        // follow the live player (clone-safe)
        var link = PlayerCarLinker.Instance;
        _player = link && link.player ? link.player.transform : transform.root;
        if (transform.parent == null && _player) transform.SetParent(_player, true);
    }

    void OnTriggerEnter(Collider other) => TryRepel(other);
    void OnTriggerStay(Collider other) => TryRepel(other);

    bool TryRepel(Collider other)
    {
        if (!shieldActive || !other) return false;

        // layer + tag gates
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return false;
        if (targetTags != null && targetTags.Length > 0)
        {
            bool ok = false;
            foreach (var t in targetTags) if (other.CompareTag(t)) { ok = true; break; }
            if (!ok) return false;
        }

        // cooldown per collider
        float now = Time.time;
        if (_lastHitTime.TryGetValue(other, out var last) && now - last < perTargetCooldown) return false;
        _lastHitTime[other] = now;

        // impact point & direction
        Vector3 origin = _player ? _player.position : transform.position;
        Vector3 hitPoint = other.ClosestPoint(origin);
        Vector3 dir = (other.transform.position - origin); dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = other.transform.forward;
        dir.Normalize();

        // FX
        if (hitVfxPrefab)
        {
            var v = Instantiate(hitVfxPrefab, hitPoint, Quaternion.LookRotation(dir));
            if (vfxScale != 1f) v.transform.localScale *= vfxScale;
            Destroy(v, vfxLifetime);
        }
        if (hitSfx) AudioSource.PlayClipAtPoint(hitSfx, hitPoint, sfxVolume);

        // knockback path 1: Rigidbody
        var rb = other.attachedRigidbody;
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * knockbackForce + Vector3.up * upBoost, forceMode);

            // launch into the air?
            if (launchEnemies)
            {
                // big arc + spin
                rb.AddForce(dir * launchOut + Vector3.up * launchUp, ForceMode.VelocityChange);
                rb.AddTorque(Random.onUnitSphere * spinTorque, ForceMode.VelocityChange);
            }
            else
            {
                // your original push
                rb.AddForce(dir * knockbackForce + Vector3.up * upBoost, forceMode);
            }

            // optional: play a hit reaction
            // var anim = rb.GetComponentInParent<Animator>();
            // if (anim && !string.IsNullOrEmpty(animTrigger)) anim.SetTrigger(animTrigger);
            // if (tameOnRepel)
            // {
            //     // Try to find a Chomper on or above the collider we repelled
            //     var tame = other.GetComponentInParent<ChomperTamingController>();
            //     if (!tame)
            //     {
            //         // fall back: locate Emerald root and add the controller if needed
            //         var root = other.transform;
            //         while (root.parent) root = root.parent;
            //         var nav = root.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true);
            //         var attach = nav ? nav.transform : other.transform;
            //         tame = attach.GetComponent<ChomperTamingController>();
            //         if (!tame) tame = attach.gameObject.AddComponent<ChomperTamingController>();
            //     }
            //     if (tame) tame.Tame();
            // }



            return true;
        }

        // knockback path 2: NavMeshAgent
        var agent = other.GetComponentInParent<NavMeshAgent>();
        if (agent)
        {
            if (launchEnemies)
            {
                // Temporarily go physics, throw, then restore the agent
                StartCoroutine(LaunchAgent(agent, dir, other));
            }
            else
            {
                // your original small warp + brief stop
                Vector3 aways = dir * Mathf.Max(agentWarpDistance, 0.25f);
                agent.isStopped = true;
                agent.Warp(agent.transform.position + aways);
                StartCoroutine(ResumeAgent(agent, agentStopSeconds));
            }









            // Vector3 away = dir * Mathf.Max(agentWarpDistance, 0.25f);
            // agent.isStopped = true;
            // agent.Warp(agent.transform.position + away);
            // StartCoroutine(ResumeAgent(agent, agentStopSeconds));

            // if (tameOnRepel)
            // {
            //     // Try to find a Chomper on or above the collider we repelled
            //     var tame = other.GetComponentInParent<ChomperTamingController>();
            //     if (!tame)
            //     {
            //         // fall back: locate Emerald root and add the controller if needed
            //         var root = other.transform;
            //         while (root.parent) root = root.parent;
            //         var nav = root.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true);
            //         var attach = nav ? nav.transform : other.transform;
            //         tame = attach.GetComponent<ChomperTamingController>();
            //         if (!tame) tame = attach.gameObject.AddComponent<ChomperTamingController>();
            //     }
            //     if (tame) tame.Tame();
            // }

            return true;
        }

        return true;
    }

    IEnumerator ResumeAgent(NavMeshAgent agent, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (agent) agent.isStopped = false;
    }


    System.Collections.IEnumerator LaunchAgent(UnityEngine.AI.NavMeshAgent agent, Vector3 dir, Collider hitCol)
    {
        if (!agent) yield break;

        // Cache transform root (usually the animated character)
        var t = agent.transform;
        var anim = t.GetComponentInChildren<Animator>();
        if (anim && !string.IsNullOrEmpty(animTrigger)) anim.SetTrigger(animTrigger);

        // Disable agent to allow physics
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.enabled = false;

        // Ensure a collider & rigidbody exist to simulate an arc
        var rb = t.GetComponent<Rigidbody>();
        var col = t.GetComponent<Collider>();
        bool addedRB = false, addedCol = false;

        if (!col)
        {
            // try to reuse the collider we hit
            col = hitCol ? hitCol : t.gameObject.AddComponent<CapsuleCollider>();
            addedCol = !hitCol;
        }
        if (!rb)
        {
            rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = 50f;              // heavy-ish so the toss feels weighty
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            addedRB = true;
        }

        // zero out motion, then launch
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(dir * launchOut + Vector3.up * launchUp, ForceMode.VelocityChange);
        rb.AddTorque(Random.onUnitSphere * spinTorque, ForceMode.VelocityChange);

        // optional: small VFX/SFX already handled above

        // Let it fly
        yield return new WaitForSeconds(ragdollSeconds);

        // Clean up physics we added
        if (addedRB) Destroy(rb);
        if (addedCol) Destroy(col);

        // Re-enable agent and gently resume
        if (agent)
        {
            agent.enabled = true;
            agent.Warp(t.position); // snap agent onto current spot
            agent.isStopped = true;
            yield return new WaitForSeconds(stunSecondsAfter);
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }





#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<SphereCollider>();
        if (!col) return;
        Gizmos.color = shieldActive ? new Color(0.2f, 0.8f, 1f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(col.center, col.radius);
    }
#endif
}
