// SimpleCheckpointService.cs
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SimpleCheckpointService : MonoBehaviour
{
    [Header("Player & Spawn")]
    public Transform player;                    // leave empty to auto-find by tag
    public string playerTag = "Player";
    public Transform defaultSpawnPoint;         // leave empty to capture at Start

    // PlayerPrefs keys
    const string KeyHasCP = "cp_has";
    const string KeyScene = "cp_scene";
    const string KeyPX = "cp_px";
    const string KeyPY = "cp_py";
    const string KeyPZ = "cp_pz";
    const string KeyRX = "cp_rx";
    const string KeyRY = "cp_ry";
    const string KeyRZ = "cp_rz";
    const string KeyRW = "cp_rw";

    void Awake()
    {
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }
    }

    void Start()
    {
        // Capture initial spawn if not assigned
        if (!defaultSpawnPoint && player)
        {
            var sp = new GameObject("CapturedSpawnPoint").transform;
            sp.position = player.position;
            sp.rotation = player.rotation;
            sp.SetParent(transform);
            defaultSpawnPoint = sp;
        }
    }

    public void SaveCheckpoint()
    {
        if (!player) { Debug.LogWarning("[CP] No player to save."); return; }

        var s = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(KeyHasCP, 1);
        PlayerPrefs.SetString(KeyScene, s);

        var p = player.position;
        var r = player.rotation;
        PlayerPrefs.SetString(KeyPX, p.x.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(KeyPY, p.y.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(KeyPZ, p.z.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(KeyRX, r.x.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(KeyRY, r.y.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(KeyRZ, r.z.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(KeyRW, r.w.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();

        Debug.Log($"[CP] Saved checkpoint in scene '{s}' at {p}");
    }

    public async void LoadCheckpoint()
    {
        if (PlayerPrefs.GetInt(KeyHasCP, 0) == 0) { Debug.LogWarning("[CP] No checkpoint saved."); return; }
        if (!player) { Debug.LogWarning("[CP] No player to load to."); return; }

        string targetScene = PlayerPrefs.GetString(KeyScene, SceneManager.GetActiveScene().name);
        var active = SceneManager.GetActiveScene().name;

        // If checkpoint is in a different scene, load it first (Addressables Single)
        if (targetScene != active)
        {
            Debug.Log($"[CP] Loading scene '{targetScene}' for checkpoint…");
            AsyncOperationHandle<SceneInstance> h = Addressables.LoadSceneAsync(targetScene, LoadSceneMode.Single);
            await h.Task;
            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[CP] Failed to load scene '{targetScene}'"); return;
            }

            // After scene load, re-find player if reference broke
            if (!player)
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go) player = go.transform;
            }
        }

        // Apply pose
        var p = new Vector3(
            float.Parse(PlayerPrefs.GetString(KeyPX, "0"), CultureInfo.InvariantCulture),
            float.Parse(PlayerPrefs.GetString(KeyPY, "0"), CultureInfo.InvariantCulture),
            float.Parse(PlayerPrefs.GetString(KeyPZ, "0"), CultureInfo.InvariantCulture)
        );
        var r = new Quaternion(
            float.Parse(PlayerPrefs.GetString(KeyRX, "0"), CultureInfo.InvariantCulture),
            float.Parse(PlayerPrefs.GetString(KeyRY, "0"), CultureInfo.InvariantCulture),
            float.Parse(PlayerPrefs.GetString(KeyRZ, "0"), CultureInfo.InvariantCulture),
            float.Parse(PlayerPrefs.GetString(KeyRW, "1"), CultureInfo.InvariantCulture)
        );

        player.SetPositionAndRotation(p, r);
        Debug.Log($"[CP] Loaded checkpoint to {p}");
    }

    public void ResetToSpawn()
    {
        if (!player) { Debug.LogWarning("[CP] No player."); return; }
        if (!defaultSpawnPoint) { Debug.LogWarning("[CP] No spawn point."); return; }

        player.SetPositionAndRotation(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        Debug.Log($"[CP] Reset to spawn at {defaultSpawnPoint.position}");
    }
}

