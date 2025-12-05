using UnityEngine;
using UnityEngine.UIElements;
using System.Linq; // <-- add at top of the file
public class ScreenUpgradeController : MonoBehaviour
{
    [SerializeField] UIDocument ui;
    [SerializeField] UpgradeDatabase database;
    [SerializeField] VisualTreeAsset itemInfoPopupUXML;
    [SerializeField] VisualTreeAsset cardTemplateUXML;   // assign ShopCard.uxml

    ShopItemInfoPopup popup;
    VisualElement root, itemsRoot;

void Awake()
{
    root = ui ? ui.rootVisualElement : null;

    if (root == null)
    {
        Debug.LogError("[Shop] UIDocument is null on ScreenUpgradeController.");
        return;
    }

    // Build only after the document is attached to a panel (UpgradePanelToggle logs show it's late)
    root.RegisterCallback<AttachToPanelEvent>(_ =>
    {
        itemsRoot = root.Q<VisualElement>("Items");
   
        
Debug.Log($"[Shop] Attached. Items found? {(itemsRoot != null)}  DB count={ (database != null && database.upgrades != null ? database.upgrades.Count : -1) }");






        // (Optional) dump the tree once to verify names/classes
        DumpTree(root, 0, 1);

        popup = new ShopItemInfoPopup(root, itemInfoPopupUXML);
        popup.OnBuy += HandleBuy;

        BuildGrid();
        RefreshWalletLabel();
    });
}


void DumpTree(VisualElement ve, int depth, int maxDepth)
{
    if (ve == null || depth > maxDepth) return;

    string type = ve.GetType().Name;
    string nm   = string.IsNullOrEmpty(ve.name) ? "" : $" name='{ve.name}'";
    // safer across versions than ve.classList
    var classes = ve.GetClasses();
    string cls  = (classes != null && classes.Any()) ? $" .{string.Join(".", classes)}" : "";

    Debug.Log($"{new string(' ', depth * 2)}{type}{nm}{cls}");

    foreach (var ch in ve.Children())
        DumpTree(ch, depth + 1, maxDepth);
}



    void RefreshWalletLabel()
    {
        var wallet = root.Q<Label>("WalletLabel");
        //if (wallet) wallet.text = PlayerWallet.Coins.ToString();

          if (wallet != null) wallet.text = PlayerWallet.Coins.ToString();
    }
void BuildGrid()
{
    if (itemsRoot == null)
    {
        Debug.LogError("[Shop] No 'Items' container found in ScreenUpgrade.uxml.");
        return;
    }
    if (database == null || database.upgrades == null)
    {
        Debug.LogError("[Shop] UpgradeDatabase is not assigned or empty.");
        return;
    }

    itemsRoot.Clear();
Debug.Log($"[Shop] Building {database.upgrades.Count} cards…");

    foreach (var def in database.upgrades)
    {
        if (def == null) continue;

        var card = cardTemplateUXML ? cardTemplateUXML.Instantiate() : new VisualElement();
        if (cardTemplateUXML == null) card.AddToClassList("card"); // fallback size

        var icon     = card.Q<Image>("Icon")     ?? new Image(){ name="Icon" };
        var title    = card.Q<Label>("Title")    ?? new Label(){ name="Title" };
        var subtitle = card.Q<Label>("Subtitle") ?? new Label(){ name="Subtitle" };
        var buyBtn   = card.Q<Button>("BuyBtn")  ?? new Button(){ name="BuyBtn" };

        if (icon.parent == null)     card.Add(icon);
        if (title.parent == null)    card.Add(title);
        if (subtitle.parent == null) card.Add(subtitle);
        if (buyBtn.parent == null)   card.Add(buyBtn);

        title.text = def.id.ToString();
        subtitle.text = string.IsNullOrEmpty(def.description) ? "Upgrade your powers." : def.description;
        if (def.icon) icon.image = def.icon.texture;
        icon.pickingMode = PickingMode.Position;

        BindCard(card, def);
        itemsRoot.Add(card);
    }
}


    void BindCard(VisualElement card, UpgradeDef def)
    {
        var M = UpgradeStateManager.Instance;

        int level  = M.GetLevel(def.id);
        bool isMax = M.IsMaxed(def.id);
        int price  = isMax ? 0 : M.GetNextCost(def.id);

        var buyBtn = card.Q<Button>("BuyBtn");
        buyBtn.text = isMax ? "MAX" : $"Buy - {price}";
        buyBtn.SetEnabled(!isMax && PlayerWallet.CanAfford(price));
        buyBtn.clicked += () => HandleBuy(def.id.ToString());

        var icon = card.Q<Image>("Icon");
if (icon != null) {
    icon.pickingMode = PickingMode.Position;
}



        if (icon != null)
        {
            icon.pickingMode = PickingMode.Position; // ensure it receives clicks
            icon.RegisterCallback<ClickEvent>(_ => OpenInfo(def, level, price, isMax));
        }

        // click anywhere on card, except buttons
        card.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target is Button) return;
            OpenInfo(def, level, price, isMax);
        });
    }

    void OpenInfo(UpgradeDef def, int currentLevel, int price, bool isMax)
    {
        var data = new ShopItemData {
            id          = def.id.ToString(),
            displayName = def.id.ToString(),
            shortDesc   = def.description,
            longDesc    = def.description,
            icon        = def.icon ? def.icon.texture : null,
            cost        = price,
            level       = currentLevel,
            maxLevel    = def.MaxLevel,
            stats       = new System.Collections.Generic.List<string> {
                $"Level {currentLevel}/{def.MaxLevel}"
            }
        };

        popup.Show(data, PlayerWallet.CanAfford(price), isMax);
    }

    void HandleBuy(string itemId)
    {
        if (!System.Enum.TryParse<UpgradeId>(itemId, out var id)) return;

        var M = UpgradeStateManager.Instance;
        if (M.TryPurchase(id))           // deducts points & raises level
        {
            RefreshWalletLabel();
            BuildGrid();                 // re-bind buttons/labels
        }
    }
}
