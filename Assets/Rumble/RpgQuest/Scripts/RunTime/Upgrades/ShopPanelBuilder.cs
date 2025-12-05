using UnityEngine;

namespace RpgQuest
{using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
public class ShopPanelBuilder : MonoBehaviour
{
    [Header("Where to build")]
    [Tooltip("Scroll View ▶ Viewport ▶ Content RectTransform")]
    [SerializeField] private RectTransform content;

    [Header("Data")]
    [SerializeField] private UpgradeDatabase database;
    [Tooltip("Which upgrades to list, in order")]
    [SerializeField] private List<UpgradeId> upgrades = new List<UpgradeId> { UpgradeId.Shield, UpgradeId.Scuba };

    [Header("Skins / Visuals")]
    [Tooltip("Optional button background sprite (sliced works great)")]
    [SerializeField] private Sprite buttonSprite;
    [Tooltip("Row height in pixels")]
    [SerializeField] private float rowHeight = 64f;

    [Header("Typography")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private int fontSize = 24;

    [Header("Layout")]
    [SerializeField] private int paddingLeft = 12;
    [SerializeField] private int paddingTop = 8;
    [SerializeField] private int spacing = 12;

    [Header("Build Controls")]
    [SerializeField] private bool clearBeforeBuild = true;

#if UNITY_EDITOR
    [ContextMenu("Build Now")]
    public void BuildNow()
    {
        if (!content)
        {
            Debug.LogError("[ShopPanelBuilder] Assign Content (Scroll View ▶ Viewport ▶ Content).");
            return;
        }
        if (!database)
        {
            Debug.LogError("[ShopPanelBuilder] Assign UpgradeDatabase.");
            return;
        }

        if (clearBeforeBuild)
        {
            // delete existing rows
            var toDelete = new List<GameObject>();
            foreach (Transform child in content) toDelete.Add(child.gameObject);
            foreach (var go in toDelete) UnityEditor.Undo.DestroyObjectImmediate(go);
        }

        // ensure Content has the right layout components
        EnsureContentLayout(content);

        // build rows
        foreach (var id in upgrades)
        {
            var rowGO = CreateRow(content, id);
            UnityEditor.Undo.RegisterCreatedObjectUndo(rowGO, "Create Shop Row");
        }

        Debug.Log($"[ShopPanelBuilder] Built {upgrades.Count} row(s) under {content.name}.");
    }
#endif

    // ---------- helpers ----------

    private void EnsureContentLayout(RectTransform rt)
    {
        var vlg = rt.GetComponent<VerticalLayoutGroup>() ?? rt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = spacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        var fitter = rt.GetComponent<ContentSizeFitter>() ?? rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private GameObject CreateRow(RectTransform parent, UpgradeId id)
    {
        // Root (row)
        var row = new GameObject($"{id}_Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(HorizontalLayoutGroup), typeof(UpgradeShopButton));
        var rt = row.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0, rowHeight);

        var img = row.GetComponent<Image>();
        img.sprite = buttonSprite;
        img.type = (buttonSprite != null && buttonSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = true;

        var btn = row.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;

        var le = row.GetComponent<LayoutElement>();
        le.preferredHeight = rowHeight;
        le.minHeight = rowHeight;
        le.flexibleWidth = 1f;

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.UpperLeft; // <-- top-left!
        hlg.padding = new RectOffset(paddingLeft, paddingLeft, paddingTop, paddingTop);
        hlg.spacing = spacing;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Label
        var label = CreateTMPChild(rt, "Label", "Upgrade", TextAlignmentOptions.TopLeft);
        SetLayout(label.gameObject, minW: 120);

        // Cost
        var cost = CreateTMPChild(rt, "Cost", "50", TextAlignmentOptions.TopLeft);
        SetLayout(cost.gameObject, minW: 80);

        // State
        var state = CreateTMPChild(rt, "State", "Lv 0/3", TextAlignmentOptions.TopLeft);
        SetLayout(state.gameObject, minW: 100);

        // Stretch label to take extra space
        var leLabel = label.GetComponent<LayoutElement>();
        leLabel.flexibleWidth = 1f;

        // Wire UpgradeShopButton
        var usb = row.GetComponent<UpgradeShopButton>();
        usb.name = "Upgrade Shop Button";
        // fields via reflection-safe accessors
        // set config
        SetUSBFields(usb, id, database, btn, cost, state, label);

        return row;
    }

    private TMP_Text CreateTMPChild(RectTransform parent, string name, string text, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.fontSize = fontSize;
        if (font) tmp.font = font;
        tmp.raycastTarget = false;

        return tmp;
    }

    private void SetLayout(GameObject go, float minW)
    {
        var le = go.GetComponent<LayoutElement>();
        le.minWidth = minW;
        le.preferredWidth = minW;
        le.flexibleWidth = 0f;
        le.minHeight = rowHeight - paddingTop * 2;
        le.preferredHeight = rowHeight - paddingTop * 2;
    }

    private void SetUSBFields(UpgradeShopButton usb, UpgradeId id, UpgradeDatabase db, Button btn, TMP_Text cost, TMP_Text state, TMP_Text label)
    {
        // Configure our row label nicely
        label.text = id.ToString();

        // Wire UpgradeShopButton public fields
        var t = typeof(UpgradeShopButton);
        TrySetField(t, usb, "id", id);
        TrySetField(t, usb, "database", db);
        TrySetField(t, usb, "buyButton", btn);
        TrySetField(t, usb, "costText", cost);
        TrySetField(t, usb, "stateText", state);
    }

    private void TrySetField(System.Type t, object instance, string fieldName, object value)
    {
        var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f != null) f.SetValue(instance, value);
        else Debug.LogWarning($"[ShopPanelBuilder] Could not set field '{fieldName}' on {t.Name} (did the script change?).");
    }
}

}
