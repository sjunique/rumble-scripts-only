using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UpgradeShopButton : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private UpgradeId id;
    [SerializeField] private UpgradeDatabase database;

    [Header("UI")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text stateText;

    private UpgradeDef _def;
    private bool _initialized;
    private Coroutine _initCo;

    void Awake()
    {
        if (!buyButton) buyButton = GetComponent<Button>();
    }

    void OnEnable()
    {
        // lazy init (in case manager isn't ready yet)
        if (_initCo != null) StopCoroutine(_initCo);
        _initCo = StartCoroutine(InitWhenReady());
    }

    void OnDisable()
    {
        if (_initCo != null) { StopCoroutine(_initCo); _initCo = null; }
        Unsubscribe();
    }

    private IEnumerator InitWhenReady()
    {
        // wait up to ~0.5s for manager to exist
        float timeout = 0.5f;
        while (UpgradeStateManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        var mgr = UpgradeStateManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[ShopButton] Init: UpgradeStateManager.Instance is still null.");
            // show disabled UI
            if (stateText) stateText.text = "N/A";
            if (costText)  costText.text  = "-";
            if (buyButton) buyButton.interactable = false;
            yield break;
        }

        _def = database ? database.Get(id) : null;
        if (_def == null)
        {
            Debug.LogError($"[ShopButton] Init: UpgradeDef not found for {id}. Is the database assigned and contains this upgrade?");
            if (stateText) stateText.text = "N/A";
            if (costText)  costText.text  = "-";
            if (buyButton) buyButton.interactable = false;
            yield break;
        }

        if (buyButton) buyButton.onClick.AddListener(Buy);

        mgr.OnPointsChanged += OnPoints;
        mgr.OnUpgradeLevelChanged += OnLevel;
        mgr.OnStateLoaded += Refresh;

        _initialized = true;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (!_initialized) return;
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnPointsChanged -= OnPoints;
            mgr.OnUpgradeLevelChanged -= OnLevel;
            mgr.OnStateLoaded -= Refresh;
        }
        if (buyButton) buyButton.onClick.RemoveListener(Buy);
        _initialized = false;
    }

    private void OnPoints(int _, int __) => Refresh();

    private void OnLevel(UpgradeId changed, int oldLvl, int newLvl)
    {
        if (changed == id) Refresh();
    }

    private void Refresh()
    {
        var mgr = UpgradeStateManager.Instance;
        if (!_initialized || mgr == null || _def == null)
        {
            if (buyButton) buyButton.interactable = false;
            return;
        }

        int lvl = mgr.GetLevel(id);
        int max = _def.MaxLevel;
        bool maxed = mgr.IsMaxed(id);
        int nextCost = mgr.GetNextCost(id);

        if (stateText) stateText.text = maxed ? $"Lv {lvl}/{max} (MAX)" : $"Lv {lvl}/{max}";
        if (costText)  costText.text  = maxed ? "-" : nextCost.ToString();

        if (buyButton) buyButton.interactable = !maxed && mgr.CanAffordNext(id);
    }

    private void Buy()
    {
        var mgr = UpgradeStateManager.Instance;
        if (!_initialized || mgr == null || _def == null) return;

        if (mgr.IsMaxed(id))
        {
            Debug.Log($"[ShopButton] {id} already MAXED.");
            Refresh();
            return;
        }

        int cost = mgr.GetNextCost(id);
        if (!mgr.CanAffordNext(id))
        {
            Debug.Log($"[ShopButton] Not enough points. Need {cost} for {id}.");
            Refresh();
            return;
        }

        if (mgr.TryPurchase(id))
        {
            Debug.Log($"[ShopButton] Purchased {id}. New Lv {mgr.GetLevel(id)}, points left {mgr.Points}.");
        }
        else
        {
            Debug.LogWarning($"[ShopButton] TryPurchase returned false for {id}.");
        }
        Refresh();
    }
}
