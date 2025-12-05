 
  using RpgQuest.Utilities; // you mentioned this utility exists
using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class MagnetPickup : MonoBehaviour
{
    [Header("Attract Settings")]
    [Tooltip("Start pulling when within this radius.")]
    public float attractRadius = 3f;

    [Tooltip("How fast the pickup flies to the player.")]
    public float pullSpeed = 6f;

    [Tooltip("Auto-collect when this close to the player.")]
    public float autoCollectDistance = 0.6f;

    [Header("Resolve Player")]
    [Tooltip("Tag used to find the player after spawn.")]
    public string playerTag = "Player";

    [Tooltip("Layers to scan for the player when searching nearby.")]
    public LayerMask playerLayers = ~0;

    [Tooltip("How often (seconds) to try re-finding the player if missing.")]
    public float reacquireInterval = 0.5f;

    [Header("Reward")]
    [SerializeField] private int pointsValue = 10;

    [Header("Optional")]
    [Tooltip("Lift target slightly to avoid intersecting feet.")]
    public float hoverOffset = 1f;

    [Tooltip("If true, forces this collider to be a trigger at runtime.")]
    public bool forceTrigger = true;

    // Runtime
    public Transform player;  // now optional; auto-filled
    float _nextScanTime;
    Collider _col;
    bool _collected;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (forceTrigger && _col) _col.isTrigger = true;

        // Try immediate resolve by tag (works if player already exists)
        TryResolvePlayerImmediate();
    }

    void OnEnable()
    {
        // Listen for future spawns (covers respawn / scene changes)
        PlayerSpawnBroadcaster.OnPlayerSpawned += HandlePlayerSpawned;
        // If anyone already spawned, latch onto the last known
        if (PlayerSpawnBroadcaster.Last != null) player = PlayerSpawnBroadcaster.Last;
    }

    void OnDisable()
    {
        PlayerSpawnBroadcaster.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    void HandlePlayerSpawned(Transform t)
    {
        if (t) player = t;
    }

    void Update()
    {
        if (_collected) return;

        // Reacquire player if missing
        if (!player && Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + reacquireInterval;
            TryResolvePlayerImmediate();
            if (!player) TryResolvePlayerNearby();
        }

        if (!player) return;

        float d = Vector3.Distance(transform.position, player.position);
        if (d <= attractRadius)
        {
            var targetPos = player.position + Vector3.up * hoverOffset;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, pullSpeed * Time.deltaTime);

            if (d <= autoCollectDistance)
                Collect();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other || !other.transform) return;

        // Helpful extra: latch onto player if they bump the pickup
        if (!player && other.CompareTag(playerTag))
            player = other.transform;

        if (player && Vector3.Distance(transform.position, player.position) <= autoCollectDistance)
            Collect();
    }

    void Collect()
    {
        if (_collected) return;
        _collected = true;

        // Award points
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.AddPoints(pointsValue);
            Debug.Log($"[Pickup] +{pointsValue} points (total now {mgr.Points}).", this);
        }
        else
        {
            Debug.LogWarning($"[Pickup] UpgradeStateManager not found. +{pointsValue} points not applied.", this);
        }

        // TODO: play SFX/VFX here…

        // Prevent double-triggers, then remove
        if (_col) _col.enabled = false;
        Destroy(gameObject);
    }

    // --------- Player resolving helpers ---------

    void TryResolvePlayerImmediate()
    {
        var go = GameObject.FindWithTag(playerTag);
        if (go) player = go.transform;
    }

    void TryResolvePlayerNearby()
    {
        // Search a bit larger than attractRadius to catch nearby player
        float scanRadius = Mathf.Max(attractRadius * 1.5f, 5f);
        var hits = Physics.OverlapSphere(transform.position, scanRadius, playerLayers, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Transform bestT = null;

        foreach (var h in hits)
        {
            var t = h.transform;
            if (!t || !t.CompareTag(playerTag)) continue;
            float d = Vector3.Distance(transform.position, t.position);
            if (d < best) { best = d; bestT = t; }
        }

        if (bestT) player = bestT;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, autoCollectDistance);
    }
}







// [RequireComponent(typeof(Collider))]
// public class MagnetPickup : MonoBehaviour
// {
//     public float attractRadius = 3f;   
//     public float pullSpeed = 6f;       
//     public float autoCollectDistance = 0.6f;

//     [Mandatory] public Transform player; // assign at runtime or find by tag "Player"
//     [SerializeField] private int pointsValue = 10; // how many points this pickup is worth

//     void Update()
//     {
//         if (!player) return;
//         float d = Vector3.Distance(transform.position, player.position);
//         if (d <= attractRadius)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 player.position + Vector3.up * 1f,
//                 pullSpeed * Time.deltaTime);

//             if (d <= autoCollectDistance) Collect();
//         }
//     }

//     void Collect()
//     {
//         // award points
//         if (UpgradeStateManager.Instance != null)

//         {
//             UpgradeStateManager.Instance.AddPoints(pointsValue);
//                    Debug.LogWarning($". {pointsValue} Points  added.");
//         }
            

//         if(UpgradeStateManager.Instance == null)
//            Debug.LogWarning($"UpgradeStateManager instance not found. {pointsValue} Points not added.");

//         // TODO: play sound, VFX
//         Destroy(gameObject);
//     }
// }
