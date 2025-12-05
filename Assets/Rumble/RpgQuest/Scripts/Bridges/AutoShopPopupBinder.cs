using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AutoShopPopupBinder : MonoBehaviour
{
    [Header("Optional")]
    public string walletLabelName = "WalletLabel";

    [Tooltip("Used only for fallback name→id match if your title isn't exactly the UpgradeId.ToString().")]
    public UpgradeDatabase database; // optional

    // Your UXML class names
    const string CardClass      = "card";
    const string IconClass      = "card-icon";
    const string TitleClass     = "card-title";
    const string DescClass      = "card-description";
    const string BuyButtonClass = "card-button";

    UIDocument ui;
    VisualElement root;
    bool attached;
    bool bound;
    int pollCount;

    void Awake()
    {
        ui = GetComponent<UIDocument>();
        root = ui ? ui.rootVisualElement : null;
        Debug.Log($"[ShopPopup] Awake on '{name}'  ui={(ui!=null)} root={(root!=null)}");
    }

    void OnEnable()
    {
        Debug.Log($"[ShopPopup] OnEnable on '{name}'");
        if (root != null)
        {
            root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }
    }

    void Start()
    {
        Debug.Log($"[ShopPopup] Start on '{name}'  panel={(root!=null ? (root.panel!=null).ToString() : "null-root")}");
        // If already attached, kick binding now
        if (root != null && root.panel != null) OnAttach(null);
    }

    void OnDisable()
    {
        Debug.Log($"[ShopPopup] OnDisable on '{name}'");
        if (root != null)
        {
            root.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            root.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        }
    }

    void Update()
    {
        // F6 = test popup (proves script is alive even if no cards yet)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("[ShopPopup] F6 pressed → showing test popup");
            SimpleItemPopup.Show(
                root,
                "TEST ITEM",
                "If you can see this, binder & popup are alive.",
                null,
                123,
                true,
                false,
                () => Debug.Log("[ShopPopup] Test BUY clicked"));
        }
    }

    void OnAttach(AttachToPanelEvent _)
    {
        attached = true; bound = false; pollCount = 0;
      //  Debug.Log($"[ShopPopup] AttachToPanelEvent on '{name}'. Will poll for cards…");

        // dump top of the tree (first two levels)
        DumpTree(root, 0, 2);

        // Start polling; many controllers swap SourceAsset after attach
        root.schedule.Execute(PollAndBind).Every(500).Until(() => bound || pollCount > 20);
    }

    void OnDetach(DetachFromPanelEvent _)
    {
        attached = false; bound = false;
//        Debug.Log($"[ShopPopup] DetachFromPanelEvent on '{name}'.");
    }

    void PollAndBind()
    {
        pollCount++;
        if (root == null) { Debug.LogError("[ShopPopup] root == null during poll"); return; }

        var cards = root.Query<VisualElement>(className: CardClass).ToList();
     //   Debug.Log($"[ShopPopup] poll#{pollCount}: found {cards.Count} '.{CardClass}'");

        if (cards.Count == 0)
        {
            if (pollCount == 1)
            {
                // first poll: list classes present to spot typos
                var classSample = root.Query<VisualElement>().ToList()
                                      .SelectMany(v => v.GetClasses())
                                      .Distinct()
                                      .OrderBy(s => s)
                                      .Take(30);
          //      Debug.Log("[ShopPopup] classes in tree (sample): " + string.Join(", ", classSample));
            }
            return;
        }

        BindNow(cards);
    }

    void BindNow(System.Collections.Generic.List<VisualElement> cards)
    {
        int wired = 0;
        Label wallet = root.Q<Label>(walletLabelName);
        if (wallet != null && UpgradeStateManager.Instance != null)
        {
            wallet.text = UpgradeStateManager.Instance.Points.ToString();
          //  Debug.Log($"[ShopPopup] WalletLabel '{walletLabelName}' set → {wallet.text}");
        }
        else
        {
       //     Debug.Log($"[ShopPopup] WalletLabel '{walletLabelName}' not found or no UpgradeStateManager.");
        }

        foreach (var card in cards)
            if (WireOneCard(card, wallet)) wired++;

        bound = true;
     //   Debug.Log($"[ShopPopup] Wired {wired}/{cards.Count} cards. (poll#{pollCount})");
    }

    bool WireOneCard(VisualElement card, Label walletLabel)
    {
        if (card == null) return false;

        var titleLbl = card.Q<Label>(className: TitleClass);
        var descLbl  = card.Q<Label>(className: DescClass);
        var buyBtn   = card.Q<Button>(className: BuyButtonClass);
        var iconVE   = card.Q(className: IconClass);

        string title = titleLbl != null ? titleLbl.text : "<no-title>";
        string desc  = descLbl  != null ? descLbl.text  : "";
        int price    = ParsePrice(buyBtn != null ? buyBtn.text : null);
        Texture2D iconTex = TryResolveIcon(iconVE);

       // Debug.Log($"[ShopPopup] card: title='{title}' price='{(buyBtn!=null?buyBtn.text:"<none>")}' iconVE={(iconVE!=null)}");
//
        Action onBuy = () =>
        {
            Debug.Log($"[ShopPopup] BUY '{title}'");
            if (Enum.TryParse<UpgradeId>(title, out var id))
            {
                if (UpgradeStateManager.Instance != null)
                {
                    bool ok = UpgradeStateManager.Instance.TryPurchase(id);
                 //   Debug.Log($"[ShopPopup] TryPurchase({id}) => {ok}");
                    if (ok && walletLabel != null)
                        walletLabel.text = UpgradeStateManager.Instance.Points.ToString();
                    if (buyBtn != null) RefreshCardPricing(buyBtn, id);
                }
                else Debug.LogError("[ShopPopup] UpgradeStateManager.Instance is null");
            }
            else
            {
            //    Debug.LogWarning($"[ShopPopup] Title '{title}' isn't a valid UpgradeId. If you use display names, change the card-title text to the enum string.");
            }
        };

        Action open = () =>
        {
            bool isMax = TryIsMax(title);
            int next   = TryNextCost(title);
            if (next <= 0) next = price;
            //Debug.Log($"[ShopPopup] OPEN '{title}' nextCost={next} isMax={isMax}");
            SimpleItemPopup.Show(root, title, desc, iconTex, next, PlayerWallet.CanAfford(next), isMax, onBuy);
        };

        if (iconVE != null)
        {
            iconVE.pickingMode = PickingMode.Position;
            iconVE.RegisterCallback<ClickEvent>(_ => { Debug.Log($"[ShopPopup] icon-click '{title}'"); open(); });
        }
        else
        {
            Debug.LogWarning($"[ShopPopup] No '.{IconClass}' in card '{title}'. Card body click will still open.");
        }

        card.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target is Button) return;
           // Debug.Log($"[ShopPopup] card-click '{title}'");
            open();
        });

        return true;
    }

    void RefreshCardPricing(Button buyBtn, UpgradeId id)
    {
        if (buyBtn == null || UpgradeStateManager.Instance == null) return;
        bool isMax = UpgradeStateManager.Instance.IsMaxed(id);
        int cost   = isMax ? 0 : UpgradeStateManager.Instance.GetNextCost(id);
        buyBtn.text = isMax ? "MAX" : $"Buy - {cost}";
        buyBtn.SetEnabled(!isMax && PlayerWallet.CanAfford(cost));
    //    Debug.Log($"[ShopPopup] refresh-price {id}: max={isMax} cost={cost}");
    }

    bool TryIsMax(string title)
    {
        if (UpgradeStateManager.Instance == null) return false;
        if (Enum.TryParse<UpgradeId>(title, out var id))
            return UpgradeStateManager.Instance.IsMaxed(id);
        return false;
    }

    int TryNextCost(string title)
    {
        if (UpgradeStateManager.Instance == null) return 0;
        if (Enum.TryParse<UpgradeId>(title, out var id))
            return UpgradeStateManager.Instance.GetNextCost(id);
        return 0;
    }

    int ParsePrice(string buttonText)
    {
        if (string.IsNullOrEmpty(buttonText)) return 0;
        if (buttonText.IndexOf("MAX", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
        int price = 0; string digits = "";
        foreach (char c in buttonText) if (char.IsDigit(c)) digits += c;
        int.TryParse(digits, out price);
        return price;
    }

    Texture2D TryResolveIcon(VisualElement ve)
    {
        if (ve is Image img && img.image is Texture2D t) return t;
        if (ve != null)
        {
            var bg = ve.resolvedStyle.backgroundImage;
            if (bg.texture != null) return bg.texture as Texture2D;
        }
        return null;
    }

    // small tree dump (first couple of levels)
    void DumpTree(VisualElement ve, int depth, int maxDepth)
    {
        if (ve == null || depth > maxDepth) return;
        var classes = string.Join(".", ve.GetClasses().ToArray());
        string line = $"{new string(' ', depth*2)}{ve.GetType().Name}";
        if (!string.IsNullOrEmpty(ve.name)) line += $" name='{ve.name}'";
        if (!string.IsNullOrEmpty(classes)) line += $" .{classes}";
      //  Debug.Log(line);
        foreach (var ch in ve.Children()) DumpTree(ch, depth+1, maxDepth);
    }
}
