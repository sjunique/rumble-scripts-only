 

// LinkerRefresher.cs (attach to one bootstrap object)
using UnityEngine;
public class LinkerRefresher : MonoBehaviour
{
    void OnEnable()
    {
        PlayerCarSpawner.OnPlayerSpawned += OnSpawn;
        // if you have an OnCarSummoned event, hook it too (the WaterCarSummon calls OnCarSummoned)
        // WaterCarSummon.OnCarSummoned += OnCarSummoned; // make static if needed
    }
    void OnDisable()
    {
        PlayerCarSpawner.OnPlayerSpawned -= OnSpawn;
    }
    void OnSpawn(GameObject player)
    {
       // if (PlayerCarLinker.Instance != null) PlayerCarLinker.Instance.PushIntoAll();
    }
}
