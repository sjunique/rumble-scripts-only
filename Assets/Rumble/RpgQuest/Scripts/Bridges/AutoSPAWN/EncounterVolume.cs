using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EncounterVolume : MonoBehaviour
{
    public AISpawnManager manager;
    public GameObject[] wallsToEnable;
    public GameObject boss;
    bool started;

    void OnTriggerEnter(Collider other)
    {
        if (started) return;
        if (!other.CompareTag("Player")) return;
        started = true;

        foreach (var w in wallsToEnable) if (w) w.SetActive(true);

        if (boss)
        {
            var b = boss.GetComponent<DeathBridge>();
            if (b) b.onDied += () =>
            {
                foreach (var w in wallsToEnable) if (w) w.SetActive(false);
            };
        }
    }
}

