using UnityEngine;

// Binds UpgradeStateManager equip/ownership to player drivers & attachments.
// Works with hotkeys, shop, and runtime clones.

public class PlayerUpgradeRuntimeBinder : MonoBehaviour
{
    [Header("Upgrade IDs (match your database)")]
    public UpgradeId shieldId = UpgradeId.Shield;
    public UpgradeId scubaId  = UpgradeId.Scuba;
    public UpgradeId laserId  = UpgradeId.LaserBeam;
    public bool useJumpUpgrade = false;
    public UpgradeId jumpId    = (UpgradeId)999; // set if you have a Jump perk

    [Header("Drivers on Player")]
    public ShieldImmunityDriver shield;
    public ScubaImmunityDriver  scuba;
    public JumpIFrameDriver     jump;

    [Header("Laser Belt attach")]
    public GameObject laserBeltPrefab;
    public Transform  playerWaistBone;   // e.g., Animator "Hips"/"Spine"
    public bool       destroyLaserOnUnequip = true;

    [Header("Resilience")]
    [Tooltip("Optional safety poll (sec) in case events fire before this binds, or for late-spawned players.")]
    public float resyncSeconds = 0.5f;

    GameObject laserBeltInstance;

    void Awake()
    {
        // Try auto-wire drivers if not assigned
        if (!shield) shield = GetComponent<ShieldImmunityDriver>();
        if (!scuba)  scuba  = GetComponent<ScubaImmunityDriver>();
 

        if (!jump)   jump   = GetComponent<JumpIFrameDriver>();
    }

    void OnEnable()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnStateLoaded            += RefreshAll;
            mgr.OnUpgradeLevelChanged    += OnLevelChanged;
            mgr.OnUpgradeEquippedChanged += OnEquippedChanged;
        }
        RefreshAll();

        if (resyncSeconds > 0f)
            InvokeRepeating(nameof(RefreshAll), resyncSeconds, resyncSeconds);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(RefreshAll));
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnStateLoaded            -= RefreshAll;
            mgr.OnUpgradeLevelChanged    -= OnLevelChanged;
            mgr.OnUpgradeEquippedChanged -= OnEquippedChanged;
        }
    }

    void OnLevelChanged(UpgradeId id, int oldLvl, int newLvl)
    {
        if (id == shieldId || id == scubaId || id == laserId || (useJumpUpgrade && id == jumpId))
            RefreshAll();
    }

    void OnEquippedChanged(UpgradeId id, bool equipped)
    {
        if (id == shieldId || id == scubaId || id == laserId || (useJumpUpgrade && id == jumpId))
            RefreshAll();
    }

    public void RefreshAll()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        // --- Shield ---
        if (shield)
        {
            bool owned    = mgr.GetLevel(shieldId) > 0;
            bool equipped = mgr.IsEquipped(shieldId);
            shield.SetOwned(owned);
            // For now, treat "equipped" as "raised"; if you want to tie to block input later, flip this call.
            shield.SetRaised(owned && equipped);
        }

        // --- Scuba ---
        if (scuba)
        {
            scuba.ownsScuba     = mgr.GetLevel(scubaId) > 0;
            scuba.scubaEquipped = mgr.IsEquipped(scubaId);
            scuba.Refresh();
        }

        // --- Jump (optional) ---
        if (useJumpUpgrade && jump)
            jump.ownsJumpImmunity = (mgr.GetLevel(jumpId) > 0) && mgr.IsEquipped(jumpId);

        // --- Laser Belt (spawn/attach on equip) ---
        bool laserOwned    = mgr.GetLevel(laserId) > 0;
        bool laserEquipped = mgr.IsEquipped(laserId);
        HandleLaser(laserOwned && laserEquipped);
    }

    void HandleLaser(bool shouldBeOn)
    {
        if (shouldBeOn)
        {
            if (!laserBeltInstance && laserBeltPrefab && playerWaistBone)
            {
                laserBeltInstance = Instantiate(laserBeltPrefab, playerWaistBone);
                laserBeltInstance.transform.localPosition = Vector3.zero;
                laserBeltInstance.transform.localRotation = Quaternion.identity;
                laserBeltInstance.transform.localScale    = Vector3.one;
            }
            else if (laserBeltInstance && playerWaistBone && laserBeltInstance.transform.parent != playerWaistBone)
            {
                laserBeltInstance.transform.SetParent(playerWaistBone, worldPositionStays:false);
            }
            if (laserBeltInstance) laserBeltInstance.SetActive(true);
        }
        else
        {
            if (laserBeltInstance)
            {
                if (destroyLaserOnUnequip) Destroy(laserBeltInstance);
                else laserBeltInstance.SetActive(false);
                laserBeltInstance = null;
            }
        }
    }
}

