using System.Collections.Generic;
using UnityEngine;
using Invector;

 

public class BossGroupRoot : MonoBehaviour
{
    [Header("Prefabs (drag from Project)")]
    public GameObject bossPrefab;
    public List<GameObject> minionPrefabs = new List<GameObject>();

    [Header("Spawn Points (children of this root)")]
    [Tooltip("Optional: explicit boss spawn point. If null, use this root's transform.")]
    public Transform bossSpawnPoint;

    [Tooltip("Optional: explicit minion points. If an element is None or list is empty, we spawn around the boss.")]
    public List<Transform> minionSpawnPoints = new List<Transform>();

    [Header("Death Options")]
    public int overkillDamage = -9999;

    // runtime
    vHealthController _bossHealth;
    readonly List<vHealthController> _minionHealths = new List<vHealthController>();

    void Start()
    {
        SpawnGroup();
    }

    void OnDestroy()
    {
        if (_bossHealth != null)
            _bossHealth.onDead.RemoveListener(go =>OnBossDead());
    }

    void SpawnGroup()
    {
        Debug.Log($"[BossGroupRoot] Spawning group on {name}. Minion prefabs count = {minionPrefabs.Count}");

        if (!bossPrefab)
        {
            Debug.LogError($"[BossGroupRoot] No bossPrefab assigned on {name}");
            return;
        }

        // -------- Boss --------
        Transform bossPoint = bossSpawnPoint ? bossSpawnPoint : transform;
        Vector3 bossPos = bossPoint.position;
        Quaternion bossRot = bossPoint.rotation;

        GameObject bossGO = Instantiate(bossPrefab, bossPos, bossRot, transform);
        _bossHealth = bossGO.GetComponentInChildren<vHealthController>(true);
        if (_bossHealth == null)
        {
            Debug.LogError($"[BossGroupRoot] Spawned boss {bossGO.name} has no vHealthController.");
        }
        else
        {
            _bossHealth.onDead.AddListener(go=>OnBossDead());
        }

        // -------- Minions --------
        _minionHealths.Clear();

        for (int i = 0; i < minionPrefabs.Count; i++)
        {
            var prefab = minionPrefabs[i];
            if (!prefab)
            {
                Debug.LogWarning($"[BossGroupRoot] Minion prefab at index {i} is null on {name}");
                continue;
            }

            // compute spawn from boss + optional point
            Vector3 mPos;
            Quaternion mRot;
            GetMinionSpawnFromBoss(i, bossPos, bossRot, out mPos, out mRot);

            GameObject mGO = Instantiate(prefab, mPos, mRot, transform);
            var mh = mGO.GetComponentInChildren<vHealthController>(true);
            if (mh != null)
                _minionHealths.Add(mh);
            else
                Debug.LogWarning($"[BossGroupRoot] Spawned minion {mGO.name} has no vHealthController.");
        }

        Debug.Log($"[BossGroupRoot] Spawned boss + {_minionHealths.Count} minions under {name}");
    }

    /// <summary>
    /// Get minion spawn position/rotation.
    /// If there is a non-null MinionSpawnPoint for this index, use it.
    /// Otherwise, place the minion in a circle around the boss.
    /// </summary>
    void GetMinionSpawnFromBoss(int index, Vector3 bossPos, Quaternion bossRot,
                                out Vector3 pos, out Quaternion rot)
    {
        // 1) If we have explicit spawn points and this index has a valid transform, use it.
        if (minionSpawnPoints != null &&
            index < minionSpawnPoints.Count &&
            minionSpawnPoints[index] != null)
        {
            var t = minionSpawnPoints[index];
            pos = t.position;
            rot = t.rotation;
            return;
        }

        // 2) Fallback: circle around the boss
        int count = Mathf.Max(1, minionPrefabs.Count);
        float angle01 = index / (float)count;         // 0..1
        float angleRad = angle01 * Mathf.PI * 2f;     // 0..2pi
        float radius = 3f;                             // distance from boss

        Vector3 localOffset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * radius;
        // rotate offset with boss rotation so the ring follows his facing
        Vector3 worldOffset = bossRot * localOffset;

        pos = bossPos + worldOffset;
        // look roughly outward from the boss
        rot = Quaternion.LookRotation(worldOffset.normalized, Vector3.up);
    }

void OnBossDead()
{
    Debug.Log($"[BossGroupRoot] Boss died in {name}, killing {_minionHealths.Count} minions");

    // Kill MINIONS
    foreach (var mh in _minionHealths)
    {
        if (!mh || mh.currentHealth <= 0) continue;
        mh.ChangeHealth(overkillDamage);
    }

    // Force BOSS full death pipeline
    if (_bossHealth && !_bossHealth.isDead)
    {
        // This triggers the Animator death bool + AI shutdown + event broadcast
        _bossHealth.ChangeHealth(overkillDamage);

        // Sometimes Invector needs an explicit Kill() to fully shut down the AI
        var killMethod = _bossHealth.GetType().GetMethod("Kill");
        if (killMethod != null)
        {
            killMethod.Invoke(_bossHealth, null);
        }
    }
}



}














// public class BossGroupRoot : MonoBehaviour
// {
//     [Header("Boss & Minions")]
//     public vHealthController boss;
//     public List<vHealthController> minions = new List<vHealthController>();

//     [Header("Death Options")]
//     [Tooltip("Extra negative damage to guarantee minion death when boss dies.")]
//     public float overkillDamage = -9999f;

//     void Awake()
//     {
//         if (!boss)
//         {
//             Debug.LogWarning($"[BossGroupRoot] No boss assigned on {name}");
//             return;
//         }

//      //   boss.onDead.AddListener(OnBossDead);
//         boss.onDead.AddListener(go => OnBossDead());
//     }

//     void OnDestroy()
//     {
//         if (boss)
//            boss.onDead.AddListener(go => OnBossDead());
//     }

//     void OnBossDead()
//     {
//         Debug.Log($"[BossGroupRoot] Boss died, killing {minions.Count} minions in {name}");

//         foreach (var mh in minions)
//         {
//             if (!mh) continue;
//             if (mh.currentHealth <= 0f) continue;

//             var type = mh.GetType();
//             var change = type.GetMethod("ChangeHealth");
//             if (change != null)
//             {
//                 change.Invoke(mh, new object[] { overkillDamage });
//             }
//             else
//             {
//                 mh.SendMessage("ChangeHealth", overkillDamage, SendMessageOptions.DontRequireReceiver);
//             }
//         }
//     }
// }
