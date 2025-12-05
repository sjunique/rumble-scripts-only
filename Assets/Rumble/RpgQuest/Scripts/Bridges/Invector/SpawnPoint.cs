using UnityEngine;

 

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Unique key used for teleporting (case-sensitive)")]
    public string locationName;

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            Diag.Warn("LOC", $"SpawnPoint '{name}' empty key.", this);
            return;
        }
        var mgr = FindObjectOfType<GameLocationManager>();
        if (!mgr) { Diag.Error("LOC", "GameLocationManager not found.", this); return; }
        mgr.RegisterSpawn(locationName, this);
    }
}
// This script is responsible for registering a spawn point in the game location manager.
// It uses a unique key to identify the spawn point and logs warnings or errors if necessary.
