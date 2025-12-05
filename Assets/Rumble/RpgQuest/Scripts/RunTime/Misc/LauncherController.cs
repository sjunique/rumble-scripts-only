// Assets/Rumble/RpgQuest/Scripts/RunTime/Misc/LauncherController.cs
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RpgQuest
{
    [DisallowMultipleComponent]
    public class LauncherController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("Exact scene name as added to Build Settings")]
        public string gameSceneName = "GameScene";

        [Header("UI References")]
        public Text titleText;

        [Header("Character")]
        public Text characterHeader;
        public Button[] characterButtons; // expect 3

        [Header("Vehicle")]
        public Text vehicleHeader;
        public Button[] vehicleButtons; // expect 3

        [Header("Start & Loading")]
        public Button startButton;
        public GameObject loadingGroup; // container that holds the loading UI
        public Slider loadingBar;       // slider inside loadingGroup

        [Header("Theme (button tint)")]
        public Color normalButton = new Color(0.20f, 0.42f, 0.75f);
        public Color selectedButton = new Color(0.95f, 0.72f, 0.18f);
        public Color textColor = Color.white;

        // PlayerPrefs keys
        const string PP_CHAR = "Launcher.SelectedCharacter";
        const string PP_VEHI = "Launcher.SelectedVehicle";

        int selectedCharacter = 0;
        int selectedVehicle = 0;

        void Awake()
        {
            // Try to self-wire anything you didn’t assign
            TryAutoWire();

            // Validate & guard
            bool ok = ValidateUI();
            if (!ok)
            {
                Debug.LogError("[Launcher] UI is not fully wired. See warnings above. Disabling controller to avoid spam.");
                enabled = false;
                return;
            }

            // Restore last selections (if any)
            selectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt(PP_CHAR, 0), 0, characterButtons.Length - 1);
            selectedVehicle   = Mathf.Clamp(PlayerPrefs.GetInt(PP_VEHI, 0), 0, vehicleButtons.Length - 1);

            // Hook up listeners
            WireCharacterButtons();
            WireVehicleButtons();
            if (startButton) startButton.onClick.AddListener(StartGame);

            // Initial visual state
            ApplySelectionTint(characterButtons, selectedCharacter);
            ApplySelectionTint(vehicleButtons, selectedVehicle);

            if (loadingGroup) loadingGroup.SetActive(false);
            if (loadingBar) loadingBar.value = 0f;

            // Nice defaults
            if (titleText) titleText.text = "RPG Quest";
            if (characterHeader) characterHeader.text = "Choose Character";
            if (vehicleHeader) vehicleHeader.text = "Choose Vehicle";

            Debug.Log("[Launcher] Ready.");
        }

        // -------------------- Wiring --------------------

        void TryAutoWire()
        {
            // Only try auto-find if missing
            if (!startButton)
                startButton = FindInChildren<Button>("Start");

            if (!loadingGroup)
                loadingGroup = transform.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject).FirstOrDefault(go => go.name.ToLower().Contains("loading"));

            if (!loadingBar && loadingGroup)
                loadingBar = loadingGroup.GetComponentInChildren<Slider>(true);

            if (characterButtons == null || characterButtons.Length == 0)
                characterButtons = FindOptionButtons("character");

            if (vehicleButtons == null || vehicleButtons.Length == 0)
                vehicleButtons = FindOptionButtons("vehicle");
        }

        Button[] FindOptionButtons(string keyword)
        {
            keyword = keyword.ToLower();
            // Prefer a section named like "Character Section" or "Vehicle Section"
            var section = transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.ToLower().Contains(keyword) && t.GetComponentsInChildren<Button>(true).Length > 0);
            if (section)
            {
                var hits = section.GetComponentsInChildren<Button>(true);
                // take first 3 to stay deterministic
                return hits.Take(3).ToArray();
            }

            // Fallback: any buttons with keyword in their name
            var allButtons = transform.GetComponentsInChildren<Button>(true)
                .Where(b => b.name.ToLower().Contains(keyword)).Take(3).ToArray();

            return allButtons;
        }

        T FindInChildren<T>(string nameContains) where T : Component
        {
            if (string.IsNullOrEmpty(nameContains)) return null;
            nameContains = nameContains.ToLower();
            return transform.GetComponentsInChildren<T>(true)
                .FirstOrDefault(c => c.name.ToLower().Contains(nameContains));
        }

        bool ValidateUI()
        {
            bool ok = true;

            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogWarning("[Launcher] gameSceneName is empty.");
                ok = false;
            }

            if (characterButtons == null || characterButtons.Length < 1)
            {
                Debug.LogWarning("[Launcher] No character buttons assigned/found.");
                ok = false;
            }
            else if (characterButtons.Length != 3)
            {
                Debug.LogWarning($"[Launcher] Character buttons count is {characterButtons.Length} (expected 3). Using what we have.");
            }

            if (vehicleButtons == null || vehicleButtons.Length < 1)
            {
                Debug.LogWarning("[Launcher] No vehicle buttons assigned/found.");
                ok = false;
            }
            else if (vehicleButtons.Length != 3)
            {
                Debug.LogWarning($"[Launcher] Vehicle buttons count is {vehicleButtons.Length} (expected 3). Using what we have.");
            }

            if (!startButton)
            {
                Debug.LogWarning("[Launcher] Start button not assigned/found.");
                ok = false;
            }

            if (!loadingGroup)
            {
                Debug.LogWarning("[Launcher] Loading group not assigned/found.");
                ok = false;
            }

            if (!loadingBar)
            {
                Debug.LogWarning("[Launcher] Loading bar (Slider) not assigned/found.");
                ok = false;
            }

            return ok;
        }

        void WireCharacterButtons()
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                int idx = i;
                if (!characterButtons[i]) continue;
                characterButtons[i].onClick.RemoveAllListeners();
                characterButtons[i].onClick.AddListener(() =>
                {
                    selectedCharacter = idx;
                    PlayerPrefs.SetInt(PP_CHAR, selectedCharacter);
                    ApplySelectionTint(characterButtons, selectedCharacter);
                    Debug.Log($"[Launcher] Character selected {idx}");
                });
            }
        }

        void WireVehicleButtons()
        {
            for (int i = 0; i < vehicleButtons.Length; i++)
            {
                int idx = i;
                if (!vehicleButtons[i]) continue;
                vehicleButtons[i].onClick.RemoveAllListeners();
                vehicleButtons[i].onClick.AddListener(() =>
                {
                    selectedVehicle = idx;
                    PlayerPrefs.SetInt(PP_VEHI, selectedVehicle);
                    ApplySelectionTint(vehicleButtons, selectedVehicle);
                    Debug.Log($"[Launcher] Vehicle selected {idx}");
                });
            }
        }

        void ApplySelectionTint(Button[] buttons, int selectedIdx)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                if (!b) continue;

                var img = b.targetGraphic as Graphic;
                if (img) img.color = (i == selectedIdx) ? selectedButton : normalButton;

                // Also make sure label (child Text) stays readable
                var txt = b.GetComponentInChildren<Text>(true);
                if (txt) txt.color = textColor;
            }
        }

        // -------------------- Start Game --------------------

        public void StartGame()
        {
            if (loadingGroup) loadingGroup.SetActive(true);
            if (startButton) startButton.interactable = false;

            StartCoroutine(LoadGameScene());
        }

        IEnumerator LoadGameScene()
        {
            // Small delay so the loading UI can paint
            yield return null;

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(gameSceneName);
            }
            catch
            {
                Debug.LogError($"[Launcher] Scene '{gameSceneName}' not found. Did you add it to Build Settings?");
                if (loadingGroup) loadingGroup.SetActive(false);
                if (startButton) startButton.interactable = true;
                yield break;
            }

            op.allowSceneActivation = true; // simple mode

            while (!op.isDone)
            {
                // Unity progress goes 0..0.9 while loading, then 1 when activated
                float p = Mathf.Clamp01(op.progress / 0.9f);
                if (loadingBar) loadingBar.value = p;
                yield return null;
            }
        }
    }
}
