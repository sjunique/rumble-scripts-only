using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class NewShopUIToolkitController : MonoBehaviour
{


[Header("Prefabs & Refs")]
[SerializeField] private GameObject laserBeltPrefab;
[SerializeField] private Transform playerWaistBone; // assign from Animator "Hips" or "Waist"

[SerializeField] private GameObject petPrefab;
[SerializeField] private Transform petSpawnPoint;

[SerializeField] private GameObject bodyguardPrefab;
[SerializeField] private Transform bodyguardSpawnPoint;

// Keep track of spawned objects
private GameObject laserBeltInstance;
private GameObject petInstance;
private GameObject bodyguardInstance;





    [Header("Data")]
    [SerializeField] private UpgradeDatabase database;

    [Header("Templates (optional)")]
    [SerializeField] private VisualTreeAsset cardTemplate;   // optional: a template for a single card (must contain 'card', 'card-icon', 'card-title', 'card-description', 'card-button')

    [System.Serializable] public struct UpgradeIcon { public UpgradeId id; public Sprite icon; }
    [Header("Icons")]
    [SerializeField] private List<UpgradeIcon> icons = new();

    [Header("Upgrades to show (order)")]
    [SerializeField] private List<UpgradeId> upgrades = new List<UpgradeId>
    {
        UpgradeId.Shield, UpgradeId.Scuba, UpgradeId.Pet, UpgradeId.BodyGuard, UpgradeId.LaserBeam
    };

    [Header("Behaviour")]
    public KeyCode toggleKey = KeyCode.Tab;
    public bool pauseOnOpen = true;

    // UI
    private UIDocument doc;
    private VisualElement root;
    private VisualElement panel;                  // main container we show/hide
    private VisualElement topBar;
    private Button btnBack, btnStatus;
    private Label  titleLabel;                    // "ITEM SHOP"
    private Label  pointsLabel;                   // inside right/status button
    private ScrollView scroll;
    private VisualElement flowContainer;          // where cards go

    private Dictionary<UpgradeId, Sprite> iconMap = new();
    private bool isOpen;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        root = doc ? doc.rootVisualElement : null;
        if (root == null)
        {
            Debug.LogError("[ShopUI] UIDocument/rootVisualElement missing.");
            enabled = false; return;
        }

        // Panel/root (we’ll hide/show this). If there’s no named panel, use root.
        panel = root.Q<VisualElement>("shop-panel") ?? root.Q<VisualElement>(className: "main-container") ?? root;

        // --- Top bar (robust queries by name or class) ---
        topBar     = root.Q<VisualElement>("TopBar") ?? root.Q<VisualElement>(className: "top-bar");
        btnBack    = root.Q<Button>("btn-back")      ?? root.Q<Button>(className: "left-button");
        btnStatus  = root.Q<Button>("btn-status")    ?? root.Q<Button>(className: "right-button");
        titleLabel = root.Q<Label>("center-title")   ?? root.Q<Label>(className: "center-title");
        // points label is the label inside the right/status button
        pointsLabel = btnStatus != null
            ? (btnStatus.Q<Label>("right-text") ?? btnStatus.Q<Label>(className: "right-text"))
            : null;

        // --- Scroll / list targets ---
        scroll        = root.Q<ScrollView>("shop-scroll") ?? root.Q<ScrollView>(className: "card-scrollview");
        flowContainer = root.Q<VisualElement>("flow-container") ?? root.Q<VisualElement>(className: "flow-container");

        if (scroll == null)
        {
            Debug.LogWarning("[ShopUI] ScrollView not found. Using root for flow parent.");
            flowContainer = flowContainer ?? root;
        }

        // Build icon map
        iconMap.Clear();
        foreach (var e in icons)
            if (e.icon && !iconMap.ContainsKey(e.id))
                iconMap[e.id] = e.icon;

        // Buttons
        if (btnBack != null)   btnBack.clicked += () => SetOpen(false);
        if (btnStatus != null) btnStatus.clicked += () => UpgradeStateManager.Instance?.AddPoints(50); // test add

        // Start closed
        SetOpen(false);

        // Manager events
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnPointsChanged += OnPointsChanged;
            mgr.OnUpgradeLevelChanged += OnUpgradeLevelChanged;
            mgr.OnStateLoaded += RefreshAll;
        }
        else
        {
            Debug.LogWarning("[ShopUI] UpgradeStateManager.Instance is null at Awake. UI will still build.");
        }

        // Initial build
        RebuildCards();
        RefreshPoints();
    }

    void OnDestroy()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnPointsChanged -= OnPointsChanged;
            mgr.OnUpgradeLevelChanged -= OnUpgradeLevelChanged;
            mgr.OnStateLoaded -= RefreshAll;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) SetOpen(!isOpen);
        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null)
            panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;

        if (pauseOnOpen) Time.timeScale = open ? 0f : 1f;
  UnityEngine.Cursor.visible = open;
        UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }


 

    // ---------------- Build cards dynamically ----------------
    void RebuildCards()
    {
        if (flowContainer == null)
        {
            Debug.LogError("[ShopUI] flowContainer not found; cannot build cards.");
            return;
        }

        flowContainer.Clear();

        if (database == null)
        {
            Debug.LogError("[ShopUI] UpgradeDatabase not assigned.");
            return;
        }

        foreach (var id in upgrades)
        {
            var def = database.Get(id);
            var title = def ? def.name : id.ToString();
            var desc  = def ? def.description : "—";
            var icon  = (iconMap.TryGetValue(id, out var s) ? s : (def ? def.icon : null));

            var card = (cardTemplate != null) ? cardTemplate.Instantiate() : BuildCardFallback();

            // grab parts (by name or class)
            var cardRoot   = card.Q<VisualElement>(className: "card") ?? card;
            var iconVE     = card.Q<VisualElement>("card-icon") ?? card.Q<VisualElement>(className: "card-icon");
            var titleLabel = card.Q<Label>("card-title")        ?? card.Q<Label>(className: "card-title");
            var descLabel  = card.Q<Label>("card-description")  ?? card.Q<Label>(className: "card-description");
            var buyBtn     = card.Q<Button>("card-button")      ?? card.Q<Button>(className: "card-button");

            if (iconVE != null && icon != null)
            {
                iconVE.style.backgroundImage = new StyleBackground(icon);
                iconVE.style.unityBackgroundImageTintColor = Color.white;
            }
            if (titleLabel != null) titleLabel.text = title;
            if (descLabel != null)  descLabel.text  = string.IsNullOrEmpty(desc) ? "Upgrade your powers." : desc;

            RefreshCardPriceAndState(id, buyBtn);

            if (buyBtn != null)
            {
                buyBtn.clicked += () =>
                {
                    var mgr = UpgradeStateManager.Instance;
                    if (mgr != null && mgr.TryPurchase(id))
                    {
                        RefreshCardPriceAndState(id, buyBtn);
                        RefreshPoints();
                    }
                };
            }

            flowContainer.Add(cardRoot);
        }
    }

    VisualElement BuildCardFallback()
    {
        // Mimics your card UXML in code, in case no template is assigned.
        var card = new VisualElement(); card.AddToClassList("card");

        var icon = new VisualElement(); icon.AddToClassList("card-icon");
        var title = new Label("Title"); title.AddToClassList("card-title");
        var desc = new Label("Description"); desc.AddToClassList("card-description");
        var btn  = new Button(){ text = "Buy - ?" }; btn.AddToClassList("card-button");

        card.Add(icon); card.Add(title); card.Add(desc); card.Add(btn);
        return card;
    }

    void RefreshCardPriceAndState(UpgradeId id, Button buyBtn)
    {
        var mgr = UpgradeStateManager.Instance;
        var def = database ? database.Get(id) : null;

        if (buyBtn == null) return;

        if (mgr == null || def == null)
        {
            buyBtn.text = "N/A";
            buyBtn.SetEnabled(false);
            return;
        }

        int lvl   = mgr.GetLevel(id);
        bool maxed= mgr.IsMaxed(id);
        int cost  = mgr.GetNextCost(id);

        buyBtn.text = maxed ? "MAX" : $"Buy - {cost}";
        buyBtn.SetEnabled(!maxed && mgr.CanAffordNext(id));
    }

    // ---------------- Refresh / Events ----------------
    void RefreshAll()
    {
        RebuildCards();
        RefreshPoints();
    }

    void RefreshPoints()
    {
        if (pointsLabel != null)
            pointsLabel.text = (UpgradeStateManager.Instance?.Points ?? 0).ToString();
    }

    void OnPointsChanged(int oldPts, int newPts) => RefreshPoints();
    void OnUpgradeLevelChanged(UpgradeId id, int oldLvl, int newLvl)
    {
        // Refresh only that card’s button text/enabled state
        RefreshPoints();
        // (To be precise you’d locate that card’s button and call RefreshCardPriceAndState,
        // but rebuilding is cheap for now. If you want per-card refresh, say the word.)
    }
}
