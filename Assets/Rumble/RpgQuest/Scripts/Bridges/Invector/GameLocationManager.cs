using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;
using UnityEngine.SceneManagement;


public class GameLocationManager : MonoBehaviour
{
    private readonly Dictionary<string, SpawnPoint> spawns = new Dictionary<string, SpawnPoint>();
public bool HasSpawn(string key) => spawns.ContainsKey(key);
public string[] GetAllSpawnKeys()
{
    var arr = new string[spawns.Keys.Count];
    spawns.Keys.CopyTo(arr, 0);
    Debug.Log($"GetAllSpawnKeys: {arr.Length} keys found.");
    if (arr.Length == 0) Debug.LogWarning("No spawn keys registered!");
    else Debug.Log($"Spawn keys: {string.Join(", ", arr)}");
    // Debug.Log($"Spawn keys: {string.Join(", ", arr)}"); // Uncomment for debugging
    // This will log all keys in the console, useful for debugging purposes
    return arr;
}
    public SpawnPoint GetSpawn(string key)
    {
        if (spawns.TryGetValue(key, out var sp)) return sp;
        Diag.Error("LOC", $"No spawn point registered for key '{key}'");
        return null;
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // Rebuild next frame so all scene objects are fully active
        StartCoroutine(RebuildIndexNextFrame(s.name));
    }

    private IEnumerator RebuildIndexNextFrame(string sceneName)
    {
        yield return null; // wait one frame

        spawns.Clear();

        // find *all* SpawnPoints in the active scene (even disabled if you pass true)
        var points = Object.FindObjectsOfType<SpawnPoint>(true);
        foreach (var p in points)
        {
            if (string.IsNullOrWhiteSpace(p.locationName)) continue;
            spawns[p.locationName] = p;
        }

        Diag.Info("LOC", $"Rebuilt spawn index: {spawns.Count} points in scene '{sceneName}'");
    }


    // Teleport the player to a spawn point by its unique key
    // Returns true if successful, false if the key was not found or player is null.

    public bool TeleportPlayer(string locationKey, Transform player)
    {
        if (player == null) { Diag.Error("LOC", "TeleportPlayer: player is null"); return false; }
        if (!spawns.TryGetValue(locationKey, out var sp))
        {
            Diag.Error("LOC", $"TeleportPlayer: spawn '{locationKey}' not found");
            return false;
        }

        // If still attached to car/perch, detach
        if (player.parent != null) player.SetParent(null, true);

        // Invector / Rigidbody safety
        var rb = player.GetComponent<Rigidbody>();
        var tpc = player.GetComponent<vThirdPersonController>();

        bool prevKinematic = false;
        if (rb)
        {
            prevKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        // move
        player.SetPositionAndRotation(sp.transform.position, sp.transform.rotation);

        // settle physics
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = prevKinematic;
        }

        // rebind Invector animator state (optional but helpful)
        if (tpc && tpc.animator)
        {
            tpc.animator.Rebind();
            tpc.animator.Update(0f);
        }

        Diag.Info("LOC", $"Teleported to '{locationKey}' at {sp.transform.position}");
        return true;
    }


    public bool TeleportTransform(string locationKey, Transform t)
    {



        if (!spawns.TryGetValue(locationKey, out var sp)) { Diag.Error("LOC", $"No spawn '{locationKey}'"); return false; }
   // ⬇️ Block vehicle teleports for a short window after NearPlayer summon
    if (CarSummonGuard.IsBlocked && t.CompareTag("Vehicle"))
    {
        Diag.Info("LOC", $"Teleport suppressed by CarSummonGuard for <{t.name}>.");
        return false;
    }
    



        if (t.parent) t.SetParent(null, true);

        var rb = t.GetComponent<Rigidbody>();
        bool prevKinematic = false;




        if (rb)
        {
            prevKinematic = rb.isKinematic;
            // zero velocities while still in previous state
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // freeze during the move
        }

        t.SetPositionAndRotation(sp.transform.position, sp.transform.rotation);

        if (rb) rb.isKinematic = prevKinematic;

        Diag.Info("LOC", $"Teleported <{t.name}> to '{locationKey}' at {sp.transform.position}");
        return true;
    }


    // In GameLocationManager.RegisterSpawn(...)
    public void RegisterSpawn(string key, SpawnPoint point)
    {
        var scene = point.gameObject.scene.name;
        var path = point.transform.GetHierarchyPath(); // helper below

        if (spawns.TryGetValue(key, out var existing) && existing != null)
        {
            var oldScene = existing.gameObject.scene.name;
            var oldPath = existing.transform.GetHierarchyPath();
            Diag.Warn("LOC", $"Duplicate key '{key}'. Replacing [{oldScene}] {oldPath} → [{scene}] {path}", point);
        }
        spawns[key] = point;
        Diag.Info("LOC", $"Registered '{key}' at {point.transform.position}  [{scene}] {path}", point);
    }
}
// helper extension (put in a static class file)

