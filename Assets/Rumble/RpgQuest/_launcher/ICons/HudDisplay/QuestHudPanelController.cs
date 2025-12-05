using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class QuestHudPanelController : MonoBehaviour
{

[Header("Assets")]
[SerializeField] private Sprite shieldIcon;
[SerializeField] private Sprite scubaIcon;
[SerializeField] private Sprite laserBeamIcon;
    [SerializeField] private Sprite wolfIcon;

[SerializeField] private Sprite bodyGuardIcon;
   


    [Header("Data")]
    [SerializeField] private UpgradeDatabase database;
    [SerializeField] private List<UpgradeId> upgrades = new()
    {
        UpgradeId.Shield, UpgradeId.Scuba, UpgradeId.LaserBeam, UpgradeId.Pet, UpgradeId.BodyGuard
    };

    [Header("Assets (optional)")]
    [SerializeField] private Sprite slashOverlay;   // if not using resource("slash") in USS
    [SerializeField] private List<SpriteBinding> iconSprites; // optional per-upgrade icon override

    [Serializable]
    public struct SpriteBinding { public UpgradeId id; public Sprite icon; }

    private UIDocument doc;
    private VisualElement root;
    private readonly Dictionary<UpgradeId, IconRefs> icons = new();

    private class IconRefs
    {
        public VisualElement block;  // hud-icon
        public VisualElement image;  // hud-image
        public Label level;          // hud-level
        public VisualElement overlay;// hud-overlay
    }

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        root = doc ? doc.rootVisualElement : null;
        Debug.Log("[QuestHUD] Awake'{doc}'");
        Debug.LogWarning($"[QuestHUD] DOCUMENTROOT element '{doc}' in UXML.");

        if (root == null)
        {
            Debug.LogError("[QuestHUD] No UIDocument/rootVisualElement.");
            enabled = false;
            return;
        }

        // Map elements for each requested upgrade
        foreach (var id in upgrades)
        {
            var name = $"icon-{id.ToString().ToLower()}";
            var block = root.Q<VisualElement>(name);
            if (block == null)
            {
                Debug.LogWarning($"[QuestHUD] Missing element '{name}' in UXML.");
                continue;
            }

            var image   = block.Q<VisualElement>(className: "hud-image");
            var level   = block.Q<Label>(className: "hud-level");
            var overlay = block.Q<VisualElement>(className: "hud-overlay");

            // assign sprite to the image if provided
            var sprite = FindIconSprite(id);
            if (sprite != null)
                image.style.backgroundImage = new StyleBackground(sprite);

            // set overlay sprite via code if you didn’t use resource() in USS
            if (slashOverlay != null)
                overlay.style.backgroundImage = new StyleBackground(slashOverlay);

            icons[id] = new IconRefs { block = block, image = image, level = level, overlay = overlay };
        }

    // subscribe to manager events
    var mgr = UpgradeStateManager.Instance;
    if (mgr != null)
    {
        mgr.OnUpgradeLevelChanged += HandleLevelChanged;
        mgr.OnPointsChanged += HandlePointsChanged;   // might show/hide afford indicator if you add it
        mgr.OnStateLoaded += RefreshAll;
    }

    // Assign specific icons based on UpgradeId
    foreach (var id in upgrades)
    {
        if (!icons.TryGetValue(id, out var iconRefs)) continue;
        var image = iconRefs.image;
        switch (id)
        {
            case UpgradeId.Shield:
                if (shieldIcon) image.style.backgroundImage = new StyleBackground(shieldIcon);
                break;
            case UpgradeId.Scuba:
                if (scubaIcon) image.style.backgroundImage = new StyleBackground(scubaIcon);
                break;
            case UpgradeId.LaserBeam:
                if (laserBeamIcon) image.style.backgroundImage = new StyleBackground(laserBeamIcon);
                break;
            case UpgradeId.BodyGuard:
                if (bodyGuardIcon) image.style.backgroundImage = new StyleBackground(bodyGuardIcon);
                break;
            case UpgradeId.Pet:
                if (wolfIcon) image.style.backgroundImage = new StyleBackground(wolfIcon);
                break;
        }

        // Assign slash overlay sprite (once per element)
        if (slashOverlay && iconRefs.overlay != null)
            iconRefs.overlay.style.backgroundImage = new StyleBackground(slashOverlay);
    }

    RefreshAll();
}

    void OnDestroy()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr != null)
        {
            mgr.OnUpgradeLevelChanged -= HandleLevelChanged;
            mgr.OnPointsChanged -= HandlePointsChanged;
            mgr.OnStateLoaded -= RefreshAll;
        }
    }

    private Sprite FindIconSprite(UpgradeId id)
    {
        if (iconSprites == null) return null;
        foreach (var b in iconSprites)
            if (b.id.Equals(id)) return b.icon;
        // fallback to UpgradeDef icon
        var def = database ? database.Get(id) : null;
        return def ? def.icon : null;
    }

    private void HandleLevelChanged(UpgradeId id, int oldLvl, int newLvl)
    {
        RefreshOne(id);
    }

    private void HandlePointsChanged(int oldPts, int newPts)
    {
        // not required for HUD; left in case you want to tint affordables later
    }

    private void RefreshAll()
    {
        foreach (var id in upgrades)
            RefreshOne(id);
    }

    private void RefreshOne(UpgradeId id)
    {
        if (!icons.TryGetValue(id, out var r)) return;

        var mgr = UpgradeStateManager.Instance;
        var def = database ? database.Get(id) : null;
        if (mgr == null || def == null)
        {
            r.level.text = "Lv ?";
            r.overlay.style.opacity = 1f;
            r.block.AddToClassList("locked");
            return;
        }

        int lvl = mgr.GetLevel(id);
        r.level.text = $"Lv {lvl}";
        bool locked = lvl <= 0;
        r.overlay.style.opacity = locked ? 1f : 0f;

        if (locked) r.block.AddToClassList("locked");
        else        r.block.RemoveFromClassList("locked");
    }
}

