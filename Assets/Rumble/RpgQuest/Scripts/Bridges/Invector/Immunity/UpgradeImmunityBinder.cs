// Assets/Rumble/RpgQuest/Combat/UpgradeImmunityBinder.cs
using UnityEngine;

public class UpgradeImmunityBinder : MonoBehaviour
{
    [Header("Map upgrades → drivers")]
    public UpgradeId shieldId = UpgradeId.Shield;
    public UpgradeId scubaId  = UpgradeId.Scuba;
    // Optional: only if you add a Jump perk to your enum/database
    public bool useJumpUpgrade = false;
    public UpgradeId jumpId    = (UpgradeId)999; // set in inspector if it exists

    ShieldImmunityDriver shield;
    ScubaImmunityDriver scuba;
    JumpIFrameDriver     jump;

    void Awake()
    {
        shield = GetComponent<ShieldImmunityDriver>();
        scuba  = GetComponent<ScubaImmunityDriver>();
        jump   = GetComponent<JumpIFrameDriver>();

        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnStateLoaded            += RefreshAll;
            mgr.OnUpgradeLevelChanged    += OnLevelChanged;
            mgr.OnUpgradeEquippedChanged += OnEquippedChanged;
        }
    }

    void OnDestroy()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnStateLoaded            -= RefreshAll;
            mgr.OnUpgradeLevelChanged    -= OnLevelChanged;
            mgr.OnUpgradeEquippedChanged -= OnEquippedChanged;
        }
    }

    void OnLevelChanged(UpgradeId id, int oldLvl, int newLvl)    { if (id == shieldId || id == scubaId || (useJumpUpgrade && id == jumpId)) RefreshAll(); }
    void OnEquippedChanged(UpgradeId id, bool on)                { if (id == shieldId || id == scubaId || (useJumpUpgrade && id == jumpId)) RefreshAll(); }

    public void RefreshAll()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        // Shield: owned+equipped => owned; “raised” can default to equipped (or bind to block input elsewhere)
        if (shield)
        {
            bool owned    = mgr.GetLevel(shieldId) > 0;
            bool equipped = mgr.IsEquipped(shieldId);
            shield.SetOwned(owned);
            shield.SetRaised(equipped); // swap to input-driven if you want: shield.SetRaised(controller.isBlocking)
        }

        // Scuba: owned+equipped => Drowning/Environmental immunity
        if (scuba)
        {
            scuba.ownsScuba     = mgr.GetLevel(scubaId) > 0;
            scuba.scubaEquipped = mgr.IsEquipped(scubaId);
            scuba.Refresh();
        }

        // Jump: if you add a real upgrade id later
        if (useJumpUpgrade && jump)
            jump.ownsJumpImmunity = mgr.GetLevel(jumpId) > 0 && mgr.IsEquipped(jumpId);
    }
}

