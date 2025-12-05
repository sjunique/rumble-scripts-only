using UnityEngine;

public class ShieldUpgradeGate : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject shieldTarget;   // old alias: shieldRoot
    [SerializeField] private Behaviour extraBehaviour;  // optional extra behaviour to toggle

    [Header("Gating")]
    [Min(1)][SerializeField] private int requiredLevel = 1;

    // --- Required upgrade (typed) for bootstrap/back-compat ---
    // Bootstrap assigns UpgradeId here; for this gate it should always be Shield.
    public UpgradeId required
    {
        get => UpgradeId.Shield;
        set
        {
            // Optional sanity: warn if someone assigns the wrong ID
            if (value != UpgradeId.Shield)
                Debug.LogWarning($"[ShieldUpgradeGate] Unexpected required UpgradeId '{value}'. Expected 'Shield'.");
        }
    }

    // --- Old field name aliases (read/write) ---
    public GameObject shieldRoot
    {
        get => shieldTarget;
        set { shieldTarget = value; TryRefresh(); }
    }

    void OnEnable()
    {
        TryRefresh();
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnUpgradeLevelChanged += HandleUpgradeChanged;
            UpgradeStateManager.Instance.OnStateLoaded += TryRefresh;
        }
    }

    void OnDisable()
    {
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnUpgradeLevelChanged -= HandleUpgradeChanged;
            UpgradeStateManager.Instance.OnStateLoaded -= TryRefresh;
        }
    }

    private void HandleUpgradeChanged(UpgradeId id, int oldLvl, int newLvl)
    {
        if (id == UpgradeId.Shield) TryRefresh();
    }

    private void TryRefresh()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        int lvl = mgr.GetLevel(UpgradeId.Shield);
        bool enabled = lvl >= requiredLevel;

        if (shieldTarget) shieldTarget.SetActive(enabled);
        if (extraBehaviour) extraBehaviour.enabled = enabled;
    }
}
