using UnityEngine;
using UnityEditor;
public class LauncherUpgradeBootstrap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameLauncher launcher;
    [SerializeField] private UpgradeStateManager upgradeManager;
    [SerializeField] private UpgradeDatabase catalog;

    [Header("Player Children (auto-find by name)")]
    [SerializeField] private string shieldChildName = "ForceShield";
    [SerializeField] private string scubaChildName  = "ScubaGear";
    [SerializeField] private GameObject explicitShieldRoot;
    [SerializeField] private GameObject explicitScubaRoot;

    [Header("Debug / Testing")]
    [SerializeField] private bool seedPointsForTest = true;
    [SerializeField] private int seedPoints = 100;
    [SerializeField] private bool grantShieldLv1 = true;
    [SerializeField] private bool grantScubaLv1  = true;
static bool SkipGrantsOnce;
    void Awake()
    {
        if (!upgradeManager)
            upgradeManager = FindObjectOfType<UpgradeStateManager>();

        if (!catalog)
            catalog = Resources.Load<UpgradeDatabase>("UpgradeDatabase");
    }

    void Start()
    {
        BootstrapUpgrades();
    }

    private void BootstrapUpgrades()
    {
        if (!upgradeManager)
        {
            Debug.LogError("[LauncherUpgradeBootstrap] No UpgradeStateManager found!");
            return;
        }

        // --- Debug/test seeding ---
        if (seedPointsForTest) 
            upgradeManager.AddPoints(seedPoints);

        if (grantShieldLv1) 
            upgradeManager.GrantFromQuest(UpgradeId.Shield);

        if (grantScubaLv1)  
            upgradeManager.GrantFromQuest(UpgradeId.Scuba);

        // Refresh all listeners (HUD, gates, etc.)
        upgradeManager.ForceStateLoaded();

        Debug.Log("[LauncherUpgradeBootstrap] Upgrades bootstrapped successfully.");
    }

    // =========================
    // RESET ALL (Inspector/Editor)
    // =========================
 [ContextMenu("Reset All Upgrades")]
public void ResetAllUpgrades()
{
    upgradeManager.ResetAll(true);
    SkipGrantsOnce = true;           // tell Start() to skip grants once
    upgradeManager.ForceStateLoaded();
}

    [ContextMenu("Reset All Upgrades")]
    public void ResetAllUpgradess()
    {
        if (!upgradeManager)
        {
            upgradeManager = FindObjectOfType<UpgradeStateManager>();
            if (!upgradeManager)
            {
                Debug.LogWarning("[LauncherUpgradeBootstrap] ResetAllUpgrades: No UpgradeStateManager in scene.");
                return;
            }
        }

        // Reset all and notify listeners
        upgradeManager.ResetAll(true);
        upgradeManager.ForceStateLoaded();
        Debug.Log("[LauncherUpgradeBootstrap] All upgrades and points reset.");
    }
}

#if UNITY_EDITOR


[CustomEditor(typeof(LauncherUpgradeBootstrap))]
public class LauncherUpgradeBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var bootstrap = (LauncherUpgradeBootstrap)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset All Upgrades"))
            {
                // In edit mode, make sure scene changes are recorded properly
                Undo.RecordObject(bootstrap, "Reset All Upgrades");
                bootstrap.ResetAllUpgrades();
                EditorUtility.SetDirty(bootstrap);
            }

            if (Application.isPlaying && GUILayout.Button("Re-Bootstrap Now"))
            {
                // Re-run the bootstrap to regrant seed and test levels
                var m = typeof(LauncherUpgradeBootstrap).GetMethod("Start",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (m != null) m.Invoke(bootstrap, null);
            }
        }
    }

    [MenuItem("Tools/Upgrades/Reset All Upgrades")]
    public static void ResetAllUpgradesMenu()
    {
        var bootstrap = FindObjectOfType<LauncherUpgradeBootstrap>();
        if (!bootstrap)
        {
            Debug.LogWarning("[LauncherUpgradeBootstrap] Menu Reset: No LauncherUpgradeBootstrap found in scene.");
            return;
        }
        bootstrap.ResetAllUpgrades();
    }
}
#endif
