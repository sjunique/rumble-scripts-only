using UnityEngine;

public class UpgradeEnabler : MonoBehaviour
{
    [SerializeField] private UpgradeId id;
    [SerializeField] private GameObject target;       // e.g., ForceShield child or ScubaGear child
    [SerializeField] private int requiredLevel = 1;   // enable when level >= this

  private void OnEnable()
{
    if (UpgradeStateManager.Instance == null) return;
    UpgradeStateManager.Instance.OnUpgradeLevelChanged += HandleLevelChanged;
    UpgradeStateManager.Instance.OnUpgradeEquippedChanged += HandleEquipped;
    UpgradeStateManager.Instance.OnStateLoaded += RefreshNow;
    RefreshNow();
}

private void OnDisable()
{
    if (UpgradeStateManager.Instance == null) return;
    UpgradeStateManager.Instance.OnUpgradeLevelChanged -= HandleLevelChanged;
    UpgradeStateManager.Instance.OnUpgradeEquippedChanged -= HandleEquipped;
    UpgradeStateManager.Instance.OnStateLoaded -= RefreshNow;
}

private void HandleEquipped(UpgradeId changed, bool on)
{
    if (changed == id) RefreshNow();
}

    private void HandleLevelChanged(UpgradeId changed, int oldLvl, int newLvl)
    {
        if (changed == id) RefreshNow();
    }

    
private void RefreshNow()
{
    if (target == null || UpgradeStateManager.Instance == null) return;
    int lvl = UpgradeStateManager.Instance.GetLevel(id);
    bool owned = lvl >= requiredLevel;
    bool equipped = UpgradeStateManager.Instance.IsEquipped(id);
    target.SetActive(owned && equipped);
}

}
