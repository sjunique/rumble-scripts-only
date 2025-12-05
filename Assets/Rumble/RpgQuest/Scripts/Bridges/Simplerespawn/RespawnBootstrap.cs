using System.Collections;
using UnityEngine;
using Invector;



using Invector.vCharacterController;
using UnityEngine;

public class RespawnBootstrap : MonoBehaviour
{
    void OnEnable()
    {
        PlayerCarSpawner.OnPlayerSpawned += HandleSpawned;
    }

    void OnDisable()
    {
        PlayerCarSpawner.OnPlayerSpawned -= HandleSpawned;
    }

    void HandleSpawned(GameObject player)
    {
        var health = player.GetComponent<vHealthController>();
        var respawn = player.GetComponent<SimpleRespawn>();
        if (!health || !respawn)
        {
          //  Debug.LogError("[RespawnBootstrap] Missing health or SimpleRespawn on player.");
            return;
        }

        // Ensure we’re not double-subscribing
        health.onDead.RemoveListener(respawn.OnPlayerDead);
        health.onDead.AddListener(respawn.OnPlayerDead);

       // Debug.Log($"[RespawnBootstrap] Bound vHealth.onDead -> SimpleRespawn.OnPlayerDead on {player.name}");
    }
}




