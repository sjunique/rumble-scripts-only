using UnityEngine;

 

public class UpgradeHotkeys : MonoBehaviour
{
    [SerializeField] private KeyCode toggleShield    = KeyCode.Z;
    [SerializeField] private KeyCode toggleScuba     = KeyCode.X;
    [SerializeField] private KeyCode toggleLaserBelt = KeyCode.Alpha6;
    [SerializeField] private KeyCode togglePet       = KeyCode.Alpha7;
    [SerializeField] private KeyCode toggleBodyguard =KeyCode.Alpha8;

    void Update()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        if (Input.GetKeyDown(toggleShield))
            ToggleEquip(mgr, UpgradeId.Shield);

        if (Input.GetKeyDown(toggleScuba))
            ToggleEquip(mgr, UpgradeId.Scuba);

        if (Input.GetKeyDown(toggleLaserBelt))
            ToggleEquip(mgr, UpgradeId.LaserBeam);

        if (Input.GetKeyDown(togglePet))
            ToggleEquip(mgr, UpgradeId.Pet);

        if (Input.GetKeyDown(toggleBodyguard))
            ToggleEquip(mgr, UpgradeId.BodyGuard);
    }

    private void ToggleEquip(UpgradeStateManager mgr, UpgradeId id)
    {
        if (!mgr.CanEquip(id)) return;
        bool next = !mgr.IsEquipped(id);
        mgr.SetEquipped(id, next);
        Debug.Log($"[Hotkeys] {(next ? "Equipped" : "Unequipped")} {id}");
    }
}

