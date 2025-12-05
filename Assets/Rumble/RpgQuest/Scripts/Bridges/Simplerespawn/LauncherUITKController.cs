using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Michsky.LSS; // if you’re using LSS
  using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LauncherUITKController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private SelectionScreenController selection;
    [SerializeField] private SelectedLoadout loadout;

    [Header("Scene Keys (Addressables)")]
    [Tooltip("Must match your Addressable scene keys, e.g. Level_0_Main, Level_1_Enchanted, ...")]
    [SerializeField] private List<string> levelSceneKeys = new List<string>();

    private Button btnLoad;

    void Awake()
    {
        PreviewMode.IsActive = true;

        if (!uiDocument) uiDocument = GetComponent<UIDocument>();
        if (!selection) selection = FindObjectOfType<SelectionScreenController>(true);

        if (!loadout)
        {
            loadout = SelectedLoadout.Instance;
            if (!loadout)
            {
                var go = new GameObject("SelectedLoadout");
                DontDestroyOnLoad(go);
                loadout = go.AddComponent<SelectedLoadout>();
            }
        }

        if (uiDocument && uiDocument.rootVisualElement != null)
        {
            var root = uiDocument.rootVisualElement;

            btnLoad = root.Q<Button>(className: "load-button");
         //   btnLoad = root.Q<Button>("load-button");
            if (btnLoad != null) btnLoad.clicked += OnStartClicked;
        }

        if (selection != null)
        {
            selection.OnCharacterChanged += p => { if (p) loadout.Set(SelectedLoadout.Slot.Character, p); };
            selection.OnVehicleChanged  += p => { if (p) loadout.Set(SelectedLoadout.Slot.Vehicle,  p); };
        }
    }

    void Start()
    {
        if (selection != null && loadout != null)
        {
            if (selection.CurrentCharacterPrefab) loadout.Set(SelectedLoadout.Slot.Character, selection.CurrentCharacterPrefab);
            if (selection.CurrentVehiclePrefab)  loadout.Set(SelectedLoadout.Slot.Vehicle,  selection.CurrentVehiclePrefab);
        }
    }

    void OnDestroy()
    {
        if (btnLoad != null) btnLoad.clicked -= OnStartClicked;
        if (selection != null)
        {
            selection.OnCharacterChanged -= p => { if (p) loadout.Set(SelectedLoadout.Slot.Character, p); };
            selection.OnVehicleChanged -= p => { if (p) loadout.Set(SelectedLoadout.Slot.Vehicle, p); };
        }
    }

    private void OnStartClicked()
    {
        var lo = SelectedLoadout.Instance;
        if (!lo) { Debug.LogError("No SelectedLoadout instance."); return; }
        if (string.IsNullOrEmpty(lo.CharacterId) || string.IsNullOrEmpty(lo.VehicleId))
        {
            Debug.LogWarning("[LauncherUITK] Need both Character and Vehicle selected.");
            return;
        }

        var gf = FindObjectOfType<GameFlow>(true);
        if (!gf) { Debug.LogError("No GameFlow found in scene."); return; }

        if (levelSceneKeys.Count == 0) { Debug.LogError("No level scene keys configured."); return; }
        int idx = Mathf.Clamp(MenuBridge.PendingLevelIndex, 0, levelSceneKeys.Count - 1);
        string key = levelSceneKeys[idx];

        gf.PlayLevel(key);
    }
     



}
