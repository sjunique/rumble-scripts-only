using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUpgradeIcon_v2 : MonoBehaviour
{
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private UpgradeDatabase database;
    [SerializeField] private UpgradeId id;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image slashOverlay;
    [SerializeField] private TMP_Text levelText;
    // add this field
    [SerializeField] private Image lockImage;   // optional padlock icon
    private UpgradeDef _def;

    void Awake()
    {
        _def = database?.Get(id);
        if (_def != null && iconImage != null)
            iconImage.sprite = _def.icon;
    }

    void OnEnable()
    {
        Refresh();
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnUpgradeLevelChanged += HandleLevelChanged;   // (UpgradeId,int,int)
            UpgradeStateManager.Instance.OnPointsChanged += HandlePointsChanged;        // (int,int)
            UpgradeStateManager.Instance.OnStateLoaded += Refresh;
            UpgradeStateManager.Instance.OnUpgradeEquippedChanged += HandleEquipped;
        }
    }

    void OnDisable()
    {
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnUpgradeLevelChanged -= HandleLevelChanged;
            UpgradeStateManager.Instance.OnPointsChanged -= HandlePointsChanged;
            UpgradeStateManager.Instance.OnStateLoaded -= Refresh;
            UpgradeStateManager.Instance.OnUpgradeEquippedChanged -= HandleEquipped;
        }
    }




    private void HandleEquipped(UpgradeId changed, bool on)
    {
        if (changed == id) Refresh();
    }












    private void HandleLevelChanged(UpgradeId changed, int oldLvl, int newLvl)
    {
        if (changed == id) Refresh();
    }

    private void HandlePointsChanged(int oldPts, int newPts) => Refresh();

    private void Refresh()
    {
        if (_def == null || UpgradeStateManager.Instance == null) return;

        int lvl = UpgradeStateManager.Instance.GetLevel(id);
        int max = _def.MaxLevel;
        bool locked = lvl <= 0;

        if (levelText) levelText.text = $"Lv {lvl}/{max}";

        // Make overlay visibility unambiguous: toggle the GameObject
        if (slashOverlay)
        {
            if (slashOverlay.gameObject.activeSelf != locked)
                slashOverlay.gameObject.SetActive(locked);
        }


        // Optional lock icon (if you wired it)
        if (lockImage)
        {
            if (lockImage.gameObject.activeSelf != locked)
                lockImage.gameObject.SetActive(locked);
        }

        if (iconImage)
        {
            bool canBuy = !UpgradeStateManager.Instance.IsMaxed(id) &&
                          UpgradeStateManager.Instance.CanAffordNext(id);
            iconImage.transform.localScale = canBuy ? Vector3.one * 1.05f : Vector3.one;
        }

        Debug.Log($"[HUDUpgradeIcon] Refresh {id}: lvl={lvl} locked={locked} (overlay active={slashOverlay && slashOverlay.gameObject.activeSelf})");
   bool owned = lvl > 0;
    bool equipped = UpgradeStateManager.Instance.IsEquipped(id);

    if (equippedBadge) equippedBadge.SetActive(owned && equipped);
  
    }

 




    // Optional purchase click
    public void OnClickPurchase()
    {
        if (UpgradeStateManager.Instance == null) return;
        UpgradeStateManager.Instance.TryPurchase(id);
    }
}
