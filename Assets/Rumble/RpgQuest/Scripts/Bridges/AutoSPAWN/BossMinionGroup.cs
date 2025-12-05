using System.Collections.Generic;
using UnityEngine;
using Invector;

public class BossMinionGroup : MonoBehaviour
{
    [Header("Boss & Minions")]
    [Tooltip("The boss's Health component.")]
    public vHealthController bossHealth;

    [Tooltip("All minions that should die when the boss dies.")]
    public List<vHealthController> minionHealths = new List<vHealthController>();

    [Header("Options")]
    [Tooltip("Extra damage to guarantee death (negative).")]
    public float overkillDamage = -9999f;

    [Tooltip("If true, minions will immediately be 'killed' when the boss dies.")]
    public bool killMinionsOnBossDeath = true;

    [Tooltip("If true, disable respawn on all linked spawn points when boss dies.")]
    public bool disableSpawnOnBossDeath = true;

    [Tooltip("Optional: spawn points that correspond to these minions/boss.")]
    public List<AISpawnPoint> spawnPointsToDisable = new List<AISpawnPoint>();

    void Awake()
    {
        if (bossHealth != null)
        {
            // Register boss death callback


       


  
            bossHealth.onDead.AddListener(go => OnBossDead());
 


            
        }
        else
        {
            Debug.LogWarning($"[BossMinionGroup] No bossHealth assigned on {name}");
        }
    }

    void OnDestroy()
    {
        if (bossHealth != null)
        {
    


            bossHealth.onDead.AddListener(go => OnBossDead());
        }
    }

    void OnBossDead()
    {
        Debug.Log($"[BossMinionGroup] Boss died in group {name}");

        if (killMinionsOnBossDeath)
            KillAllMinions();

        if (disableSpawnOnBossDeath)
            DisableSpawnPoints();
    }

    void KillAllMinions()
    {
        foreach (var mh in minionHealths)
        {
            if (!mh) continue;

            // If already dead, skip
            if (mh.currentHealth <= 0f)
                continue;

            // Use a public API to trigger death instead of touching currentHealth directly
            var type = mh.GetType();
            var change = type.GetMethod("ChangeHealth");
            if (change != null)
            {
                // Apply huge negative so it certainly kills them
                change.Invoke(mh, new object[] { overkillDamage });
            }
            else
            {
                // Fallback: SendMessage for custom setups
                mh.SendMessage("ChangeHealth", overkillDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void DisableSpawnPoints()
    {
        foreach (var sp in spawnPointsToDisable)
        {
            if (!sp) continue;

            sp.respawnEnabled = false;
            // optionally, set maxAlive=0 if you use it as an extra guard
            sp.maxAlive = 0;
        }
    }
}
