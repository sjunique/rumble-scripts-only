// Assets/Rumble/RpgQuest/Bridges/SimpleItemPopup.cs
using UnityEngine;
using UnityEngine.UIElements;

public static class SimpleItemPopup
{
    static VisualElement overlay;
    static Label titleLbl, descLbl, priceLbl;
    static Image iconImg;
    static Button buyBtn, closeBtn;
    static System.Action onBuy;

    public static void Ensure(VisualElement root)
    {
        if (overlay != null) return;
        if (root == null) { Debug.LogError("[ShopPopup] root == null in Ensure"); return; }

        Debug.Log("[ShopPopup] Creating overlay…");

        overlay = new VisualElement { name = "SimpleShopPopup" };
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0;
        overlay.style.top = 0;  overlay.style.bottom = 0;
        overlay.style.display = DisplayStyle.None;   // start hidden
        root.Add(overlay);

        var blocker = new VisualElement();
        blocker.style.position = Position.Absolute;
        blocker.style.left = 0; blocker.style.right = 0;
        blocker.style.top = 0;  blocker.style.bottom = 0;
        blocker.style.backgroundColor = new Color(0,0,0,0.45f);
        overlay.Add(blocker);

        var card = new VisualElement();
        card.style.width = 520;
        card.style.backgroundColor = new Color(0.14f,0.16f,0.18f);
        card.style.marginLeft = Length.Percent(50);
        card.style.marginTop  = Length.Percent(10);
        card.style.translate  = new Translate(-260, 0, 0);
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop  = 16; card.style.paddingBottom = 16;
        card.style.borderTopLeftRadius = 10;
        card.style.borderTopRightRadius = 10;
        card.style.borderBottomLeftRadius = 10;
        card.style.borderBottomRightRadius = 10;
        overlay.Add(card);

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        card.Add(header);

        titleLbl = new Label("ITEM");
        titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLbl.style.fontSize = 18;
        titleLbl.style.color = new Color(0.91f,0.93f,0.96f);
        header.Add(titleLbl);

        closeBtn = new Button(Hide) { text = "✕" };
        closeBtn.style.width = 28; closeBtn.style.height = 28;
        header.Add(closeBtn);

        var body = new VisualElement();
        body.style.marginTop = 10;
        body.style.flexDirection = FlexDirection.Row;
        body.style.alignItems = Align.FlexStart;
        card.Add(body);

        iconImg = new Image();
        iconImg.style.width = 64; iconImg.style.height = 64;
        iconImg.style.marginRight = 12;
        body.Add(iconImg);

        var rightCol = new VisualElement();
        rightCol.style.flexDirection = FlexDirection.Column;
        body.Add(rightCol);

        var nameLbl = new Label();
        nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLbl.style.fontSize = 16;
        nameLbl.style.color = new Color(0.91f,0.93f,0.96f);
        rightCol.Add(nameLbl);

        descLbl = new Label();
        descLbl.style.whiteSpace = WhiteSpace.Normal;
        descLbl.style.color = new Color(0.75f,0.78f,0.84f);
        rightCol.Add(descLbl);

        // mirror title into name
        titleLbl.RegisterValueChangedCallback(_ => nameLbl.text = titleLbl.text);

        var footer = new VisualElement();
        footer.style.marginTop = 10;
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.style.alignItems = Align.Center;
        card.Add(footer);

        priceLbl = new Label();
        priceLbl.style.color = new Color(1.0f,0.83f,0.42f);
        priceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        footer.Add(priceLbl);

        buyBtn = new Button(() => { Debug.Log("[ShopPopup] Buy clicked"); onBuy?.Invoke(); Hide(); }) { text = "Buy" };
        footer.Add(buyBtn);

        blocker.RegisterCallback<ClickEvent>(_ => { Debug.Log("[ShopPopup] Blocker click → Hide"); Hide(); });

        Hide();
    }

    public static void Show(VisualElement root, string title, string desc, Texture2D icon, int price, bool canAfford, bool isMax, System.Action onBuyAction)
    {
        if (root == null) { Debug.LogError("[ShopPopup] Show root == null"); return; }
        Ensure(root);

        Debug.Log($"[ShopPopup] Show title='{title}' price={price} canAfford={canAfford} max={isMax}");

        titleLbl.text = string.IsNullOrEmpty(title) ? "ITEM" : title;
        descLbl.text  = string.IsNullOrEmpty(desc)  ? "Upgrade your powers." : desc;
        iconImg.image = icon;

        priceLbl.text = isMax ? "MAX" : $"Cost: {price}";
        buyBtn.text   = isMax ? "MAX" : "Buy";
        buyBtn.SetEnabled(!isMax && canAfford);

        onBuy = onBuyAction;
        overlay.style.display = DisplayStyle.Flex;
    }

    public static void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[ShopPopup] Hide");
        }
        onBuy = null;
    }
}
