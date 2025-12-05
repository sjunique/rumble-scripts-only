using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(10000)]
public class UITKSmokeTestPanel : MonoBehaviour
{
    private UIDocument uiDoc;
    private PanelSettings ps;
    private VisualElement plate;
    private Label title;

    void OnEnable()
    {
        Debug.Log("[UITKSmoke] OnEnable");

        uiDoc = GetComponent<UIDocument>();
        if (!uiDoc) uiDoc = gameObject.AddComponent<UIDocument>();

        // Force a PanelSettings so Unity won’t prompt
        if (!uiDoc.panelSettings)
        {
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.targetDisplay = 0;
            ps.sortingOrder = 3000; // on top of others
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.match = 0.5f;
            ps.clearDepthStencil = true;
            ps.clearColor = false;
            uiDoc.panelSettings = ps;
            Debug.Log("[UITKSmoke] Created runtime PanelSettings");
        }

        // Ensure we’re not expecting UXML
        uiDoc.visualTreeAsset = null;

        var root = uiDoc.rootVisualElement;
        root.style.flexGrow = 1;
        root.style.display = DisplayStyle.Flex; // force visible

        // Big plate at top-right
        plate = new VisualElement { name = "SmokePlate" };
        plate.style.position = Position.Absolute;
        plate.style.top = 12; plate.style.right = 12;
        plate.style.width = 320; plate.style.height = 160;
        plate.style.backgroundColor = new Color(0.10f, 0.55f, 0.85f, 0.85f); // bright blue
        plate.style.borderTopLeftRadius = 10;
        plate.style.borderTopRightRadius = 10;
        plate.style.borderBottomLeftRadius = 10;
        plate.style.borderBottomRightRadius = 10;
        plate.style.borderLeftWidth = 2;  plate.style.borderLeftColor = Color.black;
        plate.style.borderRightWidth = 2; plate.style.borderRightColor = Color.black;
        plate.style.borderTopWidth = 2;   plate.style.borderTopColor = Color.black;
        plate.style.borderBottomWidth = 2;plate.style.borderBottomColor = Color.black;

        // Title text (explicit font + white)
        title = new Label("UITK Smoke Panel");
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.fontSize = 20;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
        title.style.color = Color.white;
        title.style.position = Position.Absolute;
        title.style.left = 0; title.style.right = 0;
        title.style.top = 10; title.style.height = 30;

        // Buttons (plain text labels so we see them)
        var row = new VisualElement();
        row.style.position = Position.Absolute;
        row.style.left = 10; row.style.right = 10;
        row.style.bottom = 12; row.style.height = 36;
        row.style.flexDirection = FlexDirection.Row;

        Button bStart = new Button(() => Debug.Log("[UITKSmoke] Start clicked")) { text = "Start ▶" };
        Button bStop  = new Button(() => Debug.Log("[UITKSmoke] Stop clicked"))  { text = "Stop ⏹" };
        foreach (var b in new[] { bStart, bStop })
        {
            b.style.width = 140; b.style.height = 36; b.style.marginRight = 10;
            b.style.backgroundColor = new Color(0.15f, 0.18f, 0.22f, 0.95f);
            b.style.borderTopLeftRadius = 6; b.style.borderTopRightRadius = 6;
            b.style.borderBottomLeftRadius = 6; b.style.borderBottomRightRadius = 6;

            // Force the inner TextElement to white
            var te = b.Q<TextElement>();
            if (te != null)
            {
                te.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
                te.style.color = Color.white;
                te.style.fontSize = 16;
                te.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
        }

        row.Add(bStart); row.Add(bStop);
        plate.Add(title);
        plate.Add(row);
        root.Add(plate);

        // Re-apply text color next frame in case children build lazily
        root.schedule.Execute(() =>
        {
            var te1 = bStart.Q<TextElement>(); if (te1 != null) te1.style.color = Color.white;
            var te2 = bStop.Q<TextElement>();  if (te2 != null) te2.style.color = Color.white;
            title.style.color = Color.white;
        }).StartingIn(10);

        Debug.Log($"[UITKSmoke] Root children: {root.childCount}");
    }

    void Update()
    {
        // Toggle visibility so you can test easily
        if (Input.GetKeyDown(KeyCode.F9))
        {
            bool showing = plate != null && plate.style.display != DisplayStyle.None;
            if (plate != null) plate.style.display = showing ? DisplayStyle.None : DisplayStyle.Flex;
            Debug.Log("[UITKSmoke] Toggle panel => " + (!showing));
        }
    }
}
