// Assets/Rumble/RpgQuest/Scripts/RunTime/Misc/LauncherWithCarousels.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RpgQuest
{
    [DisallowMultipleComponent]
    public class LauncherWithCarousels : MonoBehaviour
    {
        [Header("Scene")]
        public string gameSceneName = "GameScene";

        [Header("Carousels")]
        public CarouselSelector characterCarousel;
        public CarouselSelector vehicleCarousel;

        [Header("Start & Loading")]
        public Button startButton;
        public GameObject loadingGroup;
        public Slider loadingBar;

        // PlayerPrefs keys (shared with your game loader)
        public const string PP_SelectedCharacterId = "Launcher.Character.Id";
        public const string PP_SelectedVehicleId   = "Launcher.Vehicle.Id";
        public const string PP_SelectedCharacterIx = "Launcher.Character.Index";
        public const string PP_SelectedVehicleIx   = "Launcher.Vehicle.Index";

        void Awake()
        {
            if (!characterCarousel || !vehicleCarousel || !startButton || !loadingGroup || !loadingBar)
            {
                Debug.LogWarning("[Launcher] Some references are missing. Please wire characterCarousel, vehicleCarousel, startButton, loadingGroup, loadingBar.");
            }

            if (startButton) startButton.onClick.AddListener(StartGame);

            // Persist current selection immediately (so quitting the menu also stores choice)
            SaveSelection();
            if (characterCarousel) characterCarousel.onSelectionChanged.AddListener((i, o) => SaveSelection());
            if (vehicleCarousel)   vehicleCarousel.onSelectionChanged.AddListener((i, o) => SaveSelection());

            if (loadingGroup) loadingGroup.SetActive(false);
            if (loadingBar) loadingBar.value = 0f;
        }

        void SaveSelection()
        {
            if (characterCarousel && characterCarousel.Current != null)
            {
                PlayerPrefs.SetString(PP_SelectedCharacterId, characterCarousel.Current.id);
                PlayerPrefs.SetInt   (PP_SelectedCharacterIx, characterCarousel.Index);
            }
            if (vehicleCarousel && vehicleCarousel.Current != null)
            {
                PlayerPrefs.SetString(PP_SelectedVehicleId, vehicleCarousel.Current.id);
                PlayerPrefs.SetInt   (PP_SelectedVehicleIx, vehicleCarousel.Index);
            }
            PlayerPrefs.Save();
        }

        public void StartGame()
        {
            SaveSelection();
            if (loadingGroup) loadingGroup.SetActive(true);
            if (startButton) startButton.interactable = false;
            StartCoroutine(LoadGameScene());
        }

        IEnumerator LoadGameScene()
        {
            yield return null; // let UI paint
            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(gameSceneName);
            }
            catch
            {
                Debug.LogError($"[Launcher] Scene '{gameSceneName}' not found. Add it to Build Settings.");
                if (loadingGroup) loadingGroup.SetActive(false);
                if (startButton) startButton.interactable = true;
                yield break;
            }

            while (!op.isDone)
            {
                float p = Mathf.Clamp01(op.progress / 0.9f);
                if (loadingBar) loadingBar.value = p;
                yield return null;
            }
        }
    }
}
