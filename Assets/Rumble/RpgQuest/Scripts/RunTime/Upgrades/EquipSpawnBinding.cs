using UnityEngine;

public class EquipSpawnBinding : MonoBehaviour
{
    [Header("Config")]
    public UpgradeId upgradeId;               // Pet or BodyGuard
    public Transform spawnPoint;              // required
    public GameObject spawnPrefab;            // required
    public bool despawnOnUnequip = true;

    [Header("Follow (optional)")]
    [Tooltip("If the spawned prefab has CompanionFollow, we’ll set player by tag.")]
    public string playerTag = "Player";

    [Header("Diagnostics")]
    public bool logVerbose = true;

    private GameObject _instance;
    private bool _lastEquipped;

    void Start()
    {
        // initial apply from current state
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null)
        {
            Log("UpgradeStateManager.Instance == null at Start()");
            return;
        }

        _lastEquipped = mgr.IsEquipped(upgradeId);
        Apply(_lastEquipped);
        Log($"Start → Equipped={_lastEquipped}, Owned(level)={mgr.GetLevel(upgradeId)}");
    }

    void Update()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        bool eq = mgr.IsEquipped(upgradeId);
        if (eq != _lastEquipped)
        {
            Log($"Equip change detected → {upgradeId}: {eq}");
            _lastEquipped = eq;
            Apply(eq);
        }
    }

    void Apply(bool equipped)
    {
        if (equipped) SpawnIfNeeded();
        else DespawnIfNeeded();
    }

    void SpawnIfNeeded()
    {
        if (_instance != null)
        {
            _instance.SetActive(true);
            return;
        }

        if (!spawnPoint || !spawnPrefab)
        {
            LogWarn("Missing spawnPoint or spawnPrefab.");
            return;
        }

        _instance = Instantiate(spawnPrefab, spawnPoint.position, spawnPoint.rotation);
        Log($"Spawned '{spawnPrefab.name}' at '{spawnPoint.name}'.");

        // auto-wire CompanionFollow if present
        var follow = _instance.GetComponent<CompanionFollow>();
        if (follow && !follow.player)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player) follow.player = player.transform;
            Log($"CompanionFollow wired to {(player ? player.name : "null")}.");
        }
    }

    void DespawnIfNeeded()
    {
        if (!_instance) return;

        if (despawnOnUnequip)
        {
            Log("Despawn (Destroy).");
            Destroy(_instance);
            _instance = null;
        }
        else
        {
            Log("Despawn (SetActive false).");
            _instance.SetActive(false);
        }
    }

    // ---------- Helpers ----------
    void Log(string msg)
    {
        if (logVerbose) Debug.Log($"[EquipSpawnBinding:{upgradeId}] {msg}", this);
    }
    void LogWarn(string msg)
    {
        Debug.LogWarning($"[EquipSpawnBinding:{upgradeId}] {msg}", this);
    }

    // Context-menu quick tests (in Play Mode)
    [ContextMenu("TEST Spawn")]
    void TestSpawn() { Apply(true); }

    [ContextMenu("TEST Despawn")]
    void TestDespawn() { Apply(false); }
}
