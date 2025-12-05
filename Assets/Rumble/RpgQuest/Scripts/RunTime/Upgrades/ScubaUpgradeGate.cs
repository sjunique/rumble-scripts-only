using UnityEngine;

public class ScubaUpgradeGate : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject scubaTarget;     // old alias: scubaVisuals
    [SerializeField] private Behaviour swimExt;          // old alias: swimControllerExtension (optional)

    [Header("Gating")]
    [Min(1)][SerializeField] private int requiredLevel = 1;

    // --- Required upgrade (typed) for bootstrap/back-compat ---
    public UpgradeId required
    {
        get => UpgradeId.Scuba;
        set
        {
            if (value != UpgradeId.Scuba)
                Debug.LogWarning($"[ScubaUpgradeGate] Unexpected required UpgradeId '{value}'. Expected 'Scuba'.");
        }
    }

    // --- Old field name aliases (read/write) ---
    public GameObject scubaVisuals
    {
        get => scubaTarget;
        set { scubaTarget = value; TryRefresh(); }
    }

    public Behaviour swimControllerExtension
    {
        get => swimExt;
        set { swimExt = value; TryRefresh(); }
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
        if (id == UpgradeId.Scuba) TryRefresh();
    }

    private void TryRefresh()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        int lvl = mgr.GetLevel(UpgradeId.Scuba);
        bool enabled = lvl >= requiredLevel;

        if (scubaTarget) scubaTarget.SetActive(enabled);
        if (swimExt) swimExt.enabled = enabled;
    }
}
