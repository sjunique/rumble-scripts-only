using UnityEngine;
using Invector;
public class SimpleRespawnBinder : MonoBehaviour
{
    void Start()
    {
        var health = FindObjectOfType<vHealthController>(true);
        var respawn = FindObjectOfType<SimpleRespawn>(true);

        if (!health) { Debug.LogError("[RespawnBinder] No vHealthController found."); return; }
        if (!respawn){ Debug.LogError("[RespawnBinder] No SimpleRespawn found."); return; }

        // Subscribe (avoid dupes)
        health.onDead.RemoveListener(respawn.OnPlayerDead);
        health.onDead.AddListener(respawn.OnPlayerDead);

        Debug.Log("[RespawnBinder] Bound vHealth.onDead -> SimpleRespawn.OnPlayerDead.");
    }
}

