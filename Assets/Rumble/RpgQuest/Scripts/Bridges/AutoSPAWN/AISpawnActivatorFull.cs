using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Activates or deactivates spawned AIs based on player distance.
/// Use together with AISpawnManager & AISpawnPoint.
[RequireComponent(typeof(SphereCollider))]
public class AISpawnActivatorFull : MonoBehaviour
{


[Header("Portal Once Settings")]
public GameObject portalPrefab;
public Transform portalSpawnPoint;         // if null, will use activator position
public string portalUniqueKey = "PortalOnce_";   
public float portalLifetime = 5f;          // seconds before destroying portal




    [Header("Activation Settings")]
    public float triggerRadius = 30f;
    public bool despawnOnExit = true;
    public bool oneShot = false;                 // spawn only once ever
    public bool respawnOnReenter = true;         // allow reactivation later
    public string playerTag = "Player";
    public AISpawnManager manager;
    public AISpawnPoint point;


    public SpawnPortalOnce portalHelper;

    [Header("Despawn Mode")]
    public bool disableInsteadOfDestroy = true;  // keeps objects pooled
    public float despawnDelay = 2f;

    SphereCollider _trigger;
    bool _spawned;
    Transform _player;
    readonly List<GameObject> _spawnedObjects = new();

    void Reset()
    {
        _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
        _trigger.radius = triggerRadius;
    }


void PlayPortalOnce()
{
    // Already played?
    if (PlayerPrefs.GetInt(portalUniqueKey, 0) == 1)
        return;

    if (portalPrefab == null)
        return;

    // Decide where to place portal
    Transform t = portalSpawnPoint != null ? portalSpawnPoint : transform;

    GameObject portal = Instantiate(portalPrefab, t.position, t.rotation);
    Destroy(portal, portalLifetime);

    // Mark as played
    PlayerPrefs.SetInt(portalUniqueKey, 1);
    PlayerPrefs.Save();
}



    void Awake()
    {
        _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
        _trigger.radius = triggerRadius;
        if (!manager) manager = FindObjectOfType<AISpawnManager>();
        if (!point) point = GetComponent<AISpawnPoint>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        Debug.Log($"[Activator] Player ENTER {name}");
        _player = other.transform;

        if (_spawned && oneShot && !respawnOnReenter) return;

        Debug.Log("[AISpawnActivator] Player entered zone near " + point.name);
        SpawnIfNeeded();

    // Fire Portal VFX
    PlayPortalOnce();

    }

    [ContextMenu("Reset Portal Once Key")]
void ResetPortal()
{
    PlayerPrefs.DeleteKey(portalUniqueKey);
}


    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        Debug.Log($"[Activator] Player EXIT {name}");
        if (despawnOnExit) StartCoroutine(DespawnRoutine());
    }

    void SpawnIfNeeded()
    {
        if (manager == null || point == null) return;
        GameObject ai = manager.InstantiateForActivator(point);  // new helper method below
        if (ai)
        {
            _spawnedObjects.Add(ai);
            _spawned = true;
        }
    }

   IEnumerator DespawnRoutine()
{
    yield return new WaitForSeconds(despawnDelay);

    foreach (var obj in _spawnedObjects)
    {
        if (!obj) continue;

        if (disableInsteadOfDestroy)
        {
            var agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent) { agent.isStopped = true; agent.ResetPath(); }
            obj.SetActive(false);
        }
        else if (manager && manager.poolManager)
        {
            string id = "default";
            if (point)
                id = !string.IsNullOrEmpty(point.poolIdOrName)
                    ? point.poolIdOrName
                    : (point.prefab ? point.prefab.name : "default");

            manager.poolManager.Return(id, obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    _spawnedObjects.Clear();
    _spawned = false;
}


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
#endif
}

