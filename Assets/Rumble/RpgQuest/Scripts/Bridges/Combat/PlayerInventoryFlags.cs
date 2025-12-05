using UnityEngine;

public class PlayerInventoryFlags : MonoBehaviour
{
    public bool hasShield;
    public bool hasScuba;

    [Header("Links (optional)")]
    public ShieldRepelField shieldField; // auto-found if null

    void Awake()
    {
        if (!shieldField) shieldField = GetComponentInChildren<ShieldRepelField>(true);
    }

    public void GrantShield(bool activateNow = true)
    {
        hasShield = true;
        if (shieldField) shieldField.shieldActive = activateNow;
        RewardUI.Instance?.ShowShieldIcon(true);
        RewardUI.Instance?.Toast("Shield acquired!");
    }

    public void GrantScuba()
    {
        hasScuba = true;
        RewardUI.Instance?.ShowScubaIcon(true);
        RewardUI.Instance?.Toast("Scuba gear acquired!");
        // hook your underwater systems here (oxygen, swim speed, etc.)
    }
}

