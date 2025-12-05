using System.Collections.Generic;
using UnityEngine;
using Invector;

public class BossLink : MonoBehaviour
{
    public GameObject boss;
    public List<GameObject> minions = new();

    void Start()
    {
        if (!boss) return;
        var b = boss.GetComponent<DeathBridge>();
        if (b) b.onDied += OnBossDead;
    }

    void OnBossDead()
    {
        foreach (var m in minions)
        {
            if (!m) continue;
            var hc = m.GetComponent<vHealthController>();
            if (hc) hc.ChangeHealth(-99999);      // instant kill
            else Destroy(m);
        }
    }
}

