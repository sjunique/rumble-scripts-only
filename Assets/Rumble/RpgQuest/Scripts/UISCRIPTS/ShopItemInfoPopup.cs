using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ShopItemData
{
    public string id;
    public string displayName;
    public string shortDesc;
    public string longDesc;
    public Texture2D icon;
    public int cost;
    public int level;
    public int maxLevel;
    public List<string> stats;  // e.g. ["+10% shield", "+15% range"]
}

public class ShopItemInfoPopup
{
    readonly VisualElement root;
    readonly VisualElement modal;
    readonly Image icon;
    readonly Label title, name, sdesc, ldesc, cost;
    readonly Button buyBtn, closeBtn;
    readonly ListView statsList;

    public event Action<string> OnBuy;   // returns item id

    ShopItemData current;

    public ShopItemInfoPopup(VisualElement attachTo, VisualTreeAsset popupAsset)
    {
        root = attachTo;
        modal = popupAsset.Instantiate().Q<VisualElement>("InfoModal");
        modal.AddToClassList("hidden");
        root.Add(modal);

        icon = modal.Q<Image>("Icon");
        title = modal.Q<Label>("Title");
        name  = modal.Q<Label>("ItemName");
        sdesc = modal.Q<Label>("ShortDesc");
        ldesc = modal.Q<Label>("LongDesc");
        cost  = modal.Q<Label>("CostLabel");
        buyBtn = modal.Q<Button>("BuyBtn");
        closeBtn = modal.Q<Button>("CloseBtn");
        statsList = modal.Q<ListView>("StatsList");

        // Blocker closes
        modal.Q<VisualElement>("Blocker").RegisterCallback<ClickEvent>(_ => Hide());
        closeBtn.clicked += Hide;
        buyBtn.clicked += () => { if (current != null) OnBuy?.Invoke(current.id); };
    }

    public void Show(ShopItemData data, bool canAfford, bool isMaxed)
    {
        current = data;
        title.text = "ITEM";
        name.text = data.displayName;
        sdesc.text = data.shortDesc;
        ldesc.text = string.IsNullOrEmpty(data.longDesc) ? data.shortDesc : data.longDesc;
        icon.image = data.icon;

        cost.text = isMaxed ? "MAX" : $"Cost: {data.cost}";
        buyBtn.text = isMaxed ? "MAX" : $"Buy";
        buyBtn.SetEnabled(!isMaxed && canAfford);

        statsList.itemsSource = data.stats ?? new List<string>();



     //  statsList.makeItem = () => new Label() { classList = { "body" } };
        
statsList.makeItem = () => {
    var l = new Label();
    l.AddToClassList("body");
    return l;
};



        statsList.bindItem = (e, i) => ((Label)e).text = (string)statsList.itemsSource[i];

        modal.RemoveFromClassList("hidden");
        // focus for ESC close
        buyBtn?.Focus();
    }

    public void Hide() => modal.AddToClassList("hidden");
}

