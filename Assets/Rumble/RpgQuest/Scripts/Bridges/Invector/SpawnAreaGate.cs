using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector.vCharacterController.AI;
using Invector;

[RequireComponent(typeof(Collider))]
public class SpawnAreaGate : MonoBehaviour
{
    public vAISpawn spawner;                // drag your EnemySpawner here (or leave null to auto-find)
    public bool spawnWhenPlayerEnters = true;
    public bool despawnOnExit = true;
    public float despawnDelay = 1.5f;
    int playersInside = 0;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        spawner = GetComponent<vAISpawn>();
    }

    bool IsPlayer(Collider other) => other.CompareTag("Player");

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        playersInside++;
        if (playersInside == 1 && spawnWhenPlayerEnters && spawner)
            spawner.StartSpawnAll();   // resumes all spawn groups
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        playersInside = Mathf.Max(0, playersInside - 1);
        if (playersInside == 0 && spawner)
        {
            spawner.PauseSpawnAll();   // pauses all spawn groups
            if (despawnOnExit) StartCoroutine(DespawnAll());
        }
    }

    IEnumerator DespawnAll()
    {
        // clean up current AI so the area goes quiet when the player leaves
        foreach (var props in spawner.spawnPropertiesList)
        {
            // copy to avoid list modified issues
            var list = new List<Invector.vCharacterController.AI.vAIMotor>(props.aiSpawnedList);
            foreach (var ai in list)
            {
                if (ai) Destroy(ai.gameObject, despawnDelay);
            }
        }
        yield return null;
    }
}
