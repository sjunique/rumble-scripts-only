using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Invector.vCharacterController;
 using Invector;

[DefaultExecutionOrder(10000)] // run late
public class SceneSpawnRouter : MonoBehaviour
{

    [Header("Drag your SpawnPoint components here (safer than strings)")]
    public SpawnPoint playerSpawn;   // <- drag in Inspector
    public SpawnPoint carSpawn;

   public GameLocationManager loc;
    // Put these at the top of the class (inside SceneSpawnRouter)
    [SerializeField] string playerSpawnKey = "PlayerSpawnPoint";
    [SerializeField] string carSpawnKey = "CarSpawnPoint";


    [Header("Spawn keys registered by SpawnPoint.locationName")]

    [Header("References (auto-find by tag if empty)")]
    public Transform player;   // tag "Player"
    public Transform car;      // tag "Vehicle"

    [Header("Enforcement")]
    [Tooltip("How many end-of-frame passes to re-apply marker teleports.")]
    public int enforceFrames = 3;

    [Tooltip("Snap to ground after teleport (uses raycast, then Terrain.SampleHeight).")]
    public bool snapToGround = true;

    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundYOffset = 0.2f;

   

    IEnumerator Start()
    {
        // Wait until GameLocationManager exists
        while (!loc)
        {
            loc = FindObjectOfType<GameLocationManager>();
            if (loc) break;
            yield return null;
        }

        // Wait until the real player clone exists
        while (!player)
        {
            TryResolvePlayer();
            if (player) break;
            yield return null;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        // Make sure the scene file exists (uses current player position as fallback)
        SaveService.EnsureSceneFile(sceneName, player.transform.position, player.transform.rotation);

        // Try to load checkpoint
        SceneCheckpointFile cp;
        if (SaveService.TryLoadSceneCheckpoint(sceneName, out cp) && cp != null && cp.has)
        {
            // ✅ Apply the checkpoint position to the player
            player.SetPositionAndRotation(cp.pos, cp.rot);

            if (snapToGround)
                SnapToGround(player.transform, groundMask, groundYOffset);

            // Then move car or other markers if needed, but ignore player here
            TeleportToMarkers(loc, playerSpawnKey, carSpawnKey, ignorePlayer: true);

            Debug.Log($"[SpawnRouter] Restored checkpoint at {cp.pos} for scene '{sceneName}'.");
        }
        else
        {
            // No checkpoint → use normal spawn markers
            TeleportToMarkers(loc, playerSpawnKey, carSpawnKey);
            Debug.Log($"[SpawnRouter] No checkpoint; using default spawn for scene '{sceneName}'.");
        }
    }










    void TryResolvePlayer()
    {
        if (player) return;

        // Prefer your spawned player via PlayerSpawnBroadcaster (if you use it)
        if (PlayerSpawnBroadcaster.Last != null)
        {
            var tpc = PlayerSpawnBroadcaster.Last.GetComponent<vThirdPersonController>();
            if (tpc) { player = tpc.transform; return; }
        }

        // Fallback – first vThirdPersonController in scene
        var found = FindObjectOfType<vThirdPersonController>();
        if (found) player = found.transform;
    }
     

    void TeleportToMarkers(GameLocationManager loc, string pKey, string cKey, bool ignorePlayer = false)
    {
        if (!loc) return;

        if (!ignorePlayer && player)
        {
            bool ok = loc.TeleportPlayer(pKey, player);
            if (ok && snapToGround) SnapToGround(player, groundMask, groundYOffset);
        }
        if (car)
        {
            bool ok = loc.TeleportTransform(cKey, car);
            if (ok && snapToGround) SnapToGround(car, groundMask, groundYOffset);
        }
    }


    static void SnapToGround(Transform t, LayerMask mask, float yOffset)
    {
        var origin = t.position + Vector3.up * 200f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 1000f, mask, QueryTriggerInteraction.Ignore))
            t.position = hit.point + Vector3.up * yOffset;
        else if (Terrain.activeTerrain)
        {
            float h = Terrain.activeTerrain.SampleHeight(t.position) + Terrain.activeTerrain.transform.position.y;
            t.position = new Vector3(t.position.x, h + yOffset, t.position.z);
        }
    }


     IEnumerator ForceMarkersForFewFrames(GameLocationManager loc, string pKey, string cKey)
    {
        var scene = SceneManager.GetActiveScene().name;
        for (int i = 0; i < Mathf.Max(1, enforceFrames); i++)
        {
            yield return new WaitForEndOfFrame();
            if (player) {
                bool ok = loc.TeleportPlayer(pKey, player);
                if (ok && snapToGround) SnapToGround(player, groundMask, groundYOffset);
                Debug.Log($"[SpawnRouter] ({scene}) Player => '{pKey}' frame#{i+1} ok={ok} pos={player.position}");
            }
            if (car) {
                bool ok = loc.TeleportTransform(cKey, car);
                if (ok && snapToGround) SnapToGround(car, groundMask, groundYOffset);
                Debug.Log($"[SpawnRouter] ({scene}) Car    => '{cKey}' frame#{i+1} ok={ok} pos={car.position}");
            }
        }
    }



    // ---- Legacy single-file save (optional) ----
    void TryClearLegacyCheckpoint()
    {
        // If your old system still writes { hasCheckpoint: true } in a single save.json,
        // this makes it harmless. Adjust the path if your file name differs.
        var legacyPath = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
        if (!System.IO.File.Exists(legacyPath)) return;

        try
        {
            var json = System.IO.File.ReadAllText(legacyPath);
            if (json.Contains("\"hasCheckpoint\":true"))
            {
                json = json.Replace("\"hasCheckpoint\":true", "\"hasCheckpoint\":false");
                System.IO.File.WriteAllText(legacyPath, json);
                Debug.Log("[SpawnRouter] Cleared legacy hasCheckpoint in save.json for Level-Select launch.");
            }
        }
        catch { /* ignore */ }
    }
}


/*
 void Start()
    {
        if (!player) { var p = GameObject.FindGameObjectWithTag("Player");  if (p) player = p.transform; }
    //    if (!car)    { var v = GameObject.FindGameObjectWithTag("Vehicle"); if (v) car = v.transform; }
     if (!car)    { var v = GameObject.FindGameObjectWithTag("CarRoot"); if (v) car = v.transform; }
        var sceneName = SceneManager.GetActiveScene().name;
        var loc = FindObjectOfType<GameLocationManager>();
        if (!loc) { Debug.LogWarning("[SpawnRouter] GameLocationManager not found."); return; }

        // Resolve keys + marker transforms from dragged SpawnPoints (if assigned)
        var pKey = playerSpawn ? playerSpawn.locationName : playerSpawnKey;
        var cKey = carSpawn    ? carSpawn.locationName    : carSpawnKey;
        var pT   = playerSpawn ? playerSpawn.transform    : GameObject.Find(pKey)?.transform;
        var cT   = carSpawn    ? carSpawn.transform       : GameObject.Find(cKey)?.transform;

        // Ensure per-scene file exists with marker transform as default (has=false)
        if (pT) SaveService.EnsureSceneFile(sceneName, pT.position, pT.rotation);

        if (SelectedLoadout.LaunchedFromLevelSelect)
        {
            // Force markers for a few frames, then consume the flag
            StartCoroutine(ForceMarkersForFewFrames(loc, pKey, cKey));
            SelectedLoadout.ClearLevelSelectLaunch();
            return;
        }

        // Not from Level Select: try checkpoint; if none, use markers
        if (SaveService.TryLoadSceneCheckpoint(sceneName, out var cp) && cp != null && cp.has && player)
        {
            player.SetPositionAndRotation(cp.pos, cp.rot);
            if (snapToGround) SnapToGround(player, groundMask, groundYOffset);
            // car to its marker
            TeleportToMarkers(loc, pKey, cKey, ignorePlayer:true);
        }
        else
        {
            TeleportToMarkers(loc, pKey, cKey);
        }
    }

*/
