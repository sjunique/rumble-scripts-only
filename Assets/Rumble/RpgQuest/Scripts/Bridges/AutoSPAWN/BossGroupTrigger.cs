using UnityEngine;
using UnityEngine.AI;
 

[RequireComponent(typeof(Collider))]
public class BossGroupTrigger : MonoBehaviour
{
    [Header("Boss Group Prefab")]
    [Tooltip("Prefab with BossGroupRoot + Boss + Minions as children.")]
    public GameObject bossGroupPrefab;

    [Tooltip("Optional explicit spawn transform. If null, use this trigger's transform.")]
    public Transform spawnPoint;

    [Header("Activation")]
    public string playerTag = "Player";
    public bool oneShot = true;

    [Header("Ground / NavMesh Snap (optional)")]
    public bool snapToGround = true;
    public float groundRayHeight = 10f;
    public float groundRayDistance = 30f;
    public float surfaceOffset = 0.02f;
    public LayerMask groundMask = ~0;

    public bool snapToNavMesh = true;
    public float navMeshSampleRadius = 2f;

    bool _spawned;
    Collider _col;

    void Reset()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (!_col.isTrigger)
        {
            Debug.LogWarning($"[BossGroupTrigger] Collider on {name} was not trigger, setting isTrigger = true.");
            _col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[BossGroupTrigger] OnTriggerEnter with {other.name} (tag={other.tag}) on {name}");

        if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[BossGroupTrigger] Ignored {other.name}, tag != {playerTag}");
            return;
        }

        if (oneShot && _spawned)
        {
            Debug.Log("[BossGroupTrigger] Already spawned, oneShot = true");
            return;
        }

        SpawnBossGroup();
    }

    void SpawnBossGroup()
    {
        if (!bossGroupPrefab)
        {
            Debug.LogError($"[BossGroupTrigger] No bossGroupPrefab assigned on {name}");
            return;
        }

        _spawned = true;

        // Base position & rotation
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        // 1) Snap to ground (optional)
        if (snapToGround)
        {
            Vector3 start = pos + Vector3.up * groundRayHeight;
            if (Physics.Raycast(start, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + Vector3.up * surfaceOffset;
                Debug.Log($"[BossGroupTrigger] Ground snap at {pos}");
            }
            else
            {
                Debug.LogWarning("[BossGroupTrigger] Ground raycast did not hit, using original pos");
            }
        }

        // 2) Snap to NavMesh (optional)
        if (snapToNavMesh)
        {
            if (NavMesh.SamplePosition(pos, out var navHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                pos = navHit.position + Vector3.up * surfaceOffset;
                Debug.Log($"[BossGroupTrigger] NavMesh snap at {pos}");
            }
            else
            {
                Debug.LogWarning("[BossGroupTrigger] NavMesh.SamplePosition failed near spawn point");
            }
        }

        // 3) Instantiate
        var group = Instantiate(bossGroupPrefab, pos, rot);
        group.name = bossGroupPrefab.name + "_Instance";

        Debug.Log($"[BossGroupTrigger] Spawned boss group {group.name} at {pos}");
    }
}

// [RequireComponent(typeof(Collider))]
// public class BossGroupTrigger : MonoBehaviour
// {
//     [Header("Boss Group Prefab")]
//     [Tooltip("Prefab with BossGroupRoot + Boss + Minions as children.")]
//     public GameObject bossGroupPrefab;

//     [Tooltip("Optional explicit spawn transform. If null, use this trigger's transform.")]
//     public Transform spawnPoint;

//     [Header("Activation")]
//     public string playerTag = "Player";
//     public bool oneShot = true;

//     [Header("Ground / NavMesh Snap (optional)")]
//     public bool snapToGround = true;
//     public float groundRayHeight = 10f;
//     public float groundRayDistance = 30f;
//     public float surfaceOffset = 0.02f;
//     public LayerMask groundMask = ~0;

//     public bool snapToNavMesh = true;
//     public float navMeshSampleRadius = 2f;

//     bool _spawned;
//     Collider _col;

//     void Reset()
//     {
//         _col = GetComponent<Collider>();
//         _col.isTrigger = true;
//     }

//     void Awake()
//     {
//         _col = GetComponent<Collider>();
//         if (!_col.isTrigger)
//         {
//             Debug.LogWarning($"[BossGroupTrigger] Collider on {name} was not trigger, setting isTrigger = true.");
//             _col.isTrigger = true;
//         }
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         Debug.Log($"[BossGroupTrigger] OnTriggerEnter with {other.name} (tag={other.tag}) on {name}");

//         if (!other.CompareTag(playerTag))
//         {
//             Debug.Log($"[BossGroupTrigger] Ignored {other.name}, tag != {playerTag}");
//             return;
//         }

//         if (oneShot && _spawned)
//         {
//             Debug.Log("[BossGroupTrigger] Already spawned, oneShot = true");
//             return;
//         }

//         SpawnBossGroup();
//     }

//     void SpawnBossGroup()
//     {
//         if (!bossGroupPrefab)
//         {
//             Debug.LogError($"[BossGroupTrigger] No bossGroupPrefab assigned on {name}");
//             return;
//         }

//         _spawned = true;

//         // Base position & rotation
//         Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
//         Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

//         // 1) Snap to ground (optional)
//         if (snapToGround)
//         {
//             Vector3 start = pos + Vector3.up * groundRayHeight;
//             if (Physics.Raycast(start, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
//             {
//                 pos = hit.point + Vector3.up * surfaceOffset;
//                 Debug.Log($"[BossGroupTrigger] Ground snap at {pos}");
//             }
//             else
//             {
//                 Debug.LogWarning("[BossGroupTrigger] Ground raycast did not hit, using original pos");
//             }
//         }

//         // 2) Snap to NavMesh (optional)
//         if (snapToNavMesh)
//         {
//             if (NavMesh.SamplePosition(pos, out var navHit, navMeshSampleRadius, NavMesh.AllAreas))
//             {
//                 pos = navHit.position + Vector3.up * surfaceOffset;
//                 Debug.Log($"[BossGroupTrigger] NavMesh snap at {pos}");
//             }
//             else
//             {
//                 Debug.LogWarning("[BossGroupTrigger] NavMesh.SamplePosition failed near spawn point");
//             }
//         }

//         // 3) Instantiate
//         var group = Instantiate(bossGroupPrefab, pos, rot);
//         group.name = bossGroupPrefab.name + "_Instance";

//         Debug.Log($"[BossGroupTrigger] Spawned boss group {group.name} at {pos}");
//     }
// }
