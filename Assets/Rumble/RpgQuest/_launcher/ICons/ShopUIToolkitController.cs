using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ShopUIToolkitController : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private UpgradeDatabase database;      // assign your database
    [SerializeField] private Sprite frameSprite;            // optional (9-sliced background)
    [SerializeField] private VisualTreeAsset rowTemplate;   // optional (root must be a Button)
private Label pointsLabel;

    [Header("Upgrades to show (order)")]
    [SerializeField] private List<UpgradeId> upgrades = new List<UpgradeId>
    {
        UpgradeId.Shield,
        UpgradeId.Scuba,
        UpgradeId.Pet,
        UpgradeId.BodyGuard,
        UpgradeId.LaserBeam
    
    };

    [Header("Behaviour")]
    public KeyCode toggleKey = KeyCode.Tab;
    public bool pauseOnOpen = true;

    // UXML
    private UIDocument doc;
    private VisualElement root;
    private VisualElement panel;   // name="shop-panel"
    private ScrollView scroll;     // name="shop-scroll"
    private bool open;

    // Row model
    private class Row
    {
        public UpgradeId id;
        public Button button;
        public Label label;   // class="row-label"
        public Label cost;    // class="row-cost"
        public Label state;   // class="row-state"
    }
    private readonly List<Row> rows = new();

    void Awake()
    {

// pointsLabel = root.Q<Label>("pointsLabel");

//     var mgr = UpgradeStateManager.Instance;
//     if (mgr != null)
//         mgr.OnPointsChanged += (oldVal, newVal) => 
//             pointsLabel.text = newVal.ToString();




        doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;

        panel  = root.Q<VisualElement>("shop-panel");
        scroll = root.Q<ScrollView>("shop-scroll");
        if (panel == null || scroll == null)
        {
            Debug.LogError("[ShopUI] UXML missing 'shop-panel' or 'shop-scroll'.");
            return;
        }

        if (frameSprite != null)
        {
            panel.style.backgroundImage = new StyleBackground(frameSprite);
            // 9-slice is taken from sprite borders automatically.
        }

        SetOpen(false);

        BuildRowsFromList();

       var  mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnPointsChanged += OnPointsChanged;
            mgr.OnUpgradeLevelChanged += OnLevelChanged;
            mgr.OnStateLoaded += RefreshAll;
        }

        RefreshAll();
    }

    void OnDestroy()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnPointsChanged -= OnPointsChanged;
            mgr.OnUpgradeLevelChanged -= OnLevelChanged;
            mgr.OnStateLoaded -= RefreshAll;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) SetOpen(!open);
    }

    public void SetOpen(bool value)
    {
        open = value;
        if (panel == null) return;

        panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        if (pauseOnOpen) Time.timeScale = open ? 0f : 1f;

        UnityEngine.Cursor.visible = open;
        UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // ---------- Build rows from explicit list ----------
    private void BuildRowsFromList()
    {
        rows.Clear();
        scroll.contentContainer.Clear();

        if (database == null)
        {
            Debug.LogError("[ShopUI] UpgradeDatabase not assigned.");
            return;
        }

        foreach (var id in upgrades)
        {
            var def = database.Get(id); // your existing API
            string displayName = def ? def.name : id.ToString(); // no displayName field in your project

            var row = CreateRow(id, displayName);
            rows.Add(row);
            scroll.contentContainer.Add(row.button);
        }
    }

    private Row CreateRow(UpgradeId id, string displayName)
    {
        Button btn;
        if (rowTemplate != null)
        {
            var inst = rowTemplate.Instantiate();
            btn = inst.Q<Button>() ?? new Button();
            btn.Clear();
        }
        else
        {
            btn = new Button();
        }

        btn.AddToClassList("shop-row");
        btn.clicked += () => TryBuy(id);

        var lbl = new Label(displayName); lbl.AddToClassList("row-label");
        var cst = new Label("—");        cst.AddToClassList("row-cost");
        var st  = new Label("—");        st.AddToClassList("row-state");

        btn.Add(lbl);
        btn.Add(cst);
        btn.Add(st);

        return new Row { id = id, button = btn, label = lbl, cost = cst, state = st };
    }

    // ---------- Events / Refresh ----------
    private void OnPointsChanged(int _, int __) => RefreshAll();

    private void OnLevelChanged(UpgradeId changed, int oldLvl, int newLvl)
    {
        RefreshRow(changed);
    }

    private void RefreshAll()
    {
        foreach (var r in rows) RefreshRow(r.id);
    }

    private void RefreshRow(UpgradeId id)
    {
        var r = rows.Find(x => x.id == id);
        if (r == null) return;

        var mgr = UpgradeStateManager.Instance;
        var def = database?.Get(id);
        if (mgr == null || def == null)
        {
            r.label.text = id.ToString();
            r.cost.text  = "-";
            r.state.text = "N/A";
            r.button.SetEnabled(false);
            return;
        }

        int lvl     = mgr.GetLevel(id);
        int max     = def.MaxLevel;          // your def likely exposes this
        bool maxed  = mgr.IsMaxed(id);
        int nextCost= mgr.GetNextCost(id);

        r.state.text = maxed ? $"Lv {lvl}/{max} (MAX)" : $"Lv {lvl}/{max}";
        r.cost.text  = maxed ? "-" : nextCost.ToString();

        bool canBuy = !maxed && mgr.CanAffordNext(id);
        r.button.SetEnabled(canBuy);
    }

    private void TryBuy(UpgradeId id)
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        if (mgr.TryPurchase(id))
        {
            Debug.Log($"[ShopUI] Purchased {id}. Points {mgr.Points}, level {mgr.GetLevel(id)}");
        }
        else
        {
            Debug.Log($"[ShopUI] Cannot buy {id}. Need {mgr.GetNextCost(id)}, have {mgr.Points}.");
        }
        RefreshRow(id);
    }
}