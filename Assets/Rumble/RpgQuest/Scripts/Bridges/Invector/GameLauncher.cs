using UnityEngine;
using Invector.vCharacterController;

[DefaultExecutionOrder(-50)]
public class GameLauncher : MonoBehaviour
{
    [Header("Spawnpoint Indexes (SpawnPointManager)")]
    public int playerSpawnIndex = 0;
    public int carSpawnIndex    = 1;

    [Header("Seat/Exit lookup (under the car prefab)")]
    public string[] seatNames = { "DriverSeat", "Seat_Driver", "Seat" };
    public string[] exitNames = { "ExitPoint", "Exit_Player", "Exit" };
    public string seatTag = "Seat";
    public string exitTag = "Exit";
 [Header("Loadout & Spawns")]
    public SelectedLoadout loadout;                 // assign or it will use SelectedLoadout.Instance
    public Transform playerSpawn;
    public Transform carSpawn;

    [Header("Options")]
    public bool spawnIfMissingOnly = true;          // prevents duplicates
    public bool activateCarOnStart = false;

    [Header("Scene Instances (debug)")]
    public GameObject playerInstance;
    public GameObject carInstance;



    
 
    void Start()
    {
        // Resolve loadout
        var lo = loadout ? loadout : SelectedLoadout.Instance;
        if (!lo)
        {
            Debug.LogError("[GameLauncher] No SelectedLoadout instance found.");
            return;
        }

        // -------- PLAYER --------
        if (spawnIfMissingOnly)
        {
            // already in scene?
            var existingPlayer = FindObjectOfType<vThirdPersonController>(true);
            if (existingPlayer)
            {
                playerInstance = existingPlayer.gameObject;
                // Optional: move existing to spawn point
                if (playerSpawn)
                    playerInstance.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            }
            else
            {
                playerInstance = lo.Spawn(SelectedLoadout.Slot.Character, playerSpawn);
            }
        }
        else
        {
            playerInstance = lo.Spawn(SelectedLoadout.Slot.Character, playerSpawn);
        }

        // -------- CAR / VEHICLE --------
        if (spawnIfMissingOnly)
        {
            // try find any RpgHoverController as “car already present”
            var existingCar = FindObjectOfType<RpgHoverController>(true);
            if (existingCar)
            {
                carInstance = existingCar.gameObject;
                if (carSpawn)
                    carInstance.transform.SetPositionAndRotation(carSpawn.position, carSpawn.rotation);
            }
            else
            {
                carInstance = lo.Spawn(SelectedLoadout.Slot.Vehicle, carSpawn);
            }
        }
        else
        {
            carInstance = lo.Spawn(SelectedLoadout.Slot.Vehicle, carSpawn);
        }

        if (carInstance)
            carInstance.SetActive(activateCarOnStart);

        Debug.Log($"[GameLauncher] Player={(playerInstance?playerInstance.name:"<null>")}  Car={(carInstance?carInstance.name:"<null>")}");
    }
 


    Transform FindChildByNamesOrTag(Transform root, string[] names, string tag)
    {
        if (!root) return null;

        if (!string.IsNullOrEmpty(tag))
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag(tag)) return t;
        }

        if (names != null)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string tn = t.name.ToLowerInvariant();
                for (int i = 0; i < names.Length; i++)
                    if (tn.Contains(names[i].ToLowerInvariant()))
                        return t;
            }
        }
        return null;
    }
}
/*


[DefaultExecutionOrder(-50)]
public class GameLauncher : MonoBehaviour
{
    [Header("Spawnpoint Indexes (SpawnPointManager)")]
    public int playerSpawnIndex = 0;
    public int carSpawnIndex    = 1;

    [Header("Seat/Exit lookup (under the car prefab)")]
    public string[] seatNames = { "DriverSeat", "Seat_Driver", "Seat" };
    public string[] exitNames = { "ExitPoint", "Exit_Player", "Exit" };
    public string seatTag = "Seat";
    public string exitTag = "Exit";

    void Start()
    {
        // --- SpawnPointManager ---
        var spm = SpawnPointManager.Instance;
        if (!spm) { Debug.LogError("[GameLauncher] No SpawnPointManager in scene."); return; }
        var pSpawn = spm.GetSpawnPoint(playerSpawnIndex);
        var cSpawn = spm.GetSpawnPoint(carSpawnIndex) ?? pSpawn;
        if (!pSpawn) { Debug.LogError("[GameLauncher] Invalid playerSpawnIndex."); return; }

        // --- SelectedLoadout singleton must exist (created in Launcher scene) ---
        if (SelectedLoadout.Instance == null)
        {
            Debug.LogError("[GameLauncher] No SelectedLoadout.Instance. Launch the Game scene from the Launcher so it can persist.");
            return;
        }

        // --- Spawn character & vehicle using your SelectedLoadout API ---
        var playerGO = SelectedLoadout.Instance.Spawn(SelectedLoadout.Slot.Character, pSpawn);
        var carGO    = SelectedLoadout.Instance.Spawn(SelectedLoadout.Slot.Vehicle,  cSpawn);

        if (!playerGO) { Debug.LogError("[GameLauncher] Character prefab not set in SelectedLoadout."); return; }
        if (!carGO)    { Debug.LogWarning("[GameLauncher] Vehicle prefab not set; continuing without a car."); }

        // Ensure types/components we need
        var player = playerGO.GetComponent<vThirdPersonController>();
        if (!player)
        {
            Debug.LogError("[GameLauncher] Spawned character has no vThirdPersonController component.");
            return;
        }

        // Car starts inactive (summon later)
        if (carGO) carGO.SetActive(false);

        // Find seat/exit under car
        Transform seat = carGO ? FindChildByNamesOrTag(carGO.transform, seatNames, seatTag) : null;
        Transform exit = carGO ? FindChildByNamesOrTag(carGO.transform, exitNames, exitTag) : null;
        if (carGO && !seat) Debug.LogWarning("[GameLauncher] DriverSeat not found under car.");
        if (carGO && !exit) Debug.LogWarning("[GameLauncher] ExitPoint not found under car.");

        // --- Initialize linker ---
        var linker = PlayerCarLinker.Instance;
        if (!linker) { Debug.LogError("[GameLauncher] No PlayerCarLinker in scene."); return; }

        var carCtrl = carGO ? carGO.GetComponent<RpgHoverController>() : null;
        var rig     = FindObjectOfType<CarCameraRig>(true);
        var tp      = FindObjectOfType<MountTeleporter>(true);

        linker.Initialize(player, carGO, seat, exit, carCtrl, rig, tp);

        // Push refs into systems (refresh even if their Awake already ran)
        linker.PushInto(FindObjectOfType<CarTeleportEnterExit>(true));
        linker.PushInto(FindObjectOfType<WaterCarSummon>(true));

        Debug.Log("[GameLauncher] Spawned & linked player/car from SelectedLoadout.");
    }

    Transform FindChildByNamesOrTag(Transform root, string[] names, string tag)
    {
        if (!root) return null;

        if (!string.IsNullOrEmpty(tag))
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag(tag)) return t;
        }

        if (names != null)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string tn = t.name.ToLowerInvariant();
                for (int i = 0; i < names.Length; i++)
                    if (tn.Contains(names[i].ToLowerInvariant()))
                        return t;
            }
        }
        return null;
    }
}

*/