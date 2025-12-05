using UnityEngine;

// Assets/_Launcher/Scripts/GameSelectionSpawner.cs
using UnityEngine;
using UnityEngine;

public class GameSelectionSpawner : MonoBehaviour
{
    public Transform characterSpawn;
    public Transform vehicleSpawn;

    void Start()
    {
        var lo = SelectedLoadout.Instance;
        if (!lo) { Debug.LogWarning("[GameSpawn] No SelectedLoadout found. Load via Launcher."); return; }

        var playerPrefab  = lo.GetRuntimeCharacter();
        var vehiclePrefab = lo.GetRuntimeVehicle();

        if (playerPrefab && characterSpawn)  Instantiate(playerPrefab,  characterSpawn.position, characterSpawn.rotation);
        if (vehiclePrefab && vehicleSpawn)    Instantiate(vehiclePrefab, vehicleSpawn.position,   vehicleSpawn.rotation);
    }
}
