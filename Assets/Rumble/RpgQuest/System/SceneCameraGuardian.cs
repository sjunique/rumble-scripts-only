using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public sealed class SceneCameraGuardian : MonoBehaviour
{
    // Optional: list menu scenes where the persistent camera stays active.
    [SerializeField] string[] menuSceneNames = new[] { "BootLoader", "MainMenu", "SelectionMenu" };

    // If you want to destroy the persistent camera when returning to menu, set this true.
    [SerializeField] bool destroyOnReturnToMenu = false;

    Camera _cam;
    AudioListener _al;
    static SceneCameraGuardian _instance;

    void Awake()
    {
        // Singleton: if another persistent camera exists, keep the *first* one.
        if (_instance && _instance != this)
        {
            // If this is a duplicate instantiated later, prefer the existing one.
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _cam = GetComponent<Camera>();
        _al  = GetComponent<AudioListener>();
        DontDestroyOnLoad(gameObject);

        // Start with brain disabled if that's your pattern; you can toggle it per scene.
        var brain = GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brain) brain.enabled = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always enforce single audio listener and single active MainCamera
        EnforceSingleListenerAndCamera(scene);
    }

    void OnActiveSceneChanged(Scene from, Scene to)
    {
        // Optionally auto-enable/disable the Brain based on scene role
        bool isMenu = IsMenuScene(to.name);

        var brain = GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brain) brain.enabled = !isMenu; // brain off in menus, on in gameplay

        // If returning to menu and you want to clean up the persistent camera:
        if (destroyOnReturnToMenu && isMenu)
        {
            Destroy(gameObject);
            return;
        }

        // In gameplay scenes, prefer the persistent camera over any scene camera.
        // If you *do* have a special cutscene camera in a scene, just disable this one temporarily.
        EnforceSingleListenerAndCamera(to);
    }

    bool IsMenuScene(string sceneName)
    {
        if (menuSceneNames == null) return false;
        foreach (var s in menuSceneNames)
            if (!string.IsNullOrEmpty(s) && sceneName == s)
                return true;
        return false;
    }

    void EnforceSingleListenerAndCamera(Scene active)
    {
        // 1) Keep THIS camera enabled; disable others (especially any lingering menu camera)
        var allCams = GameObject.FindObjectsOfType<Camera>(true);
        foreach (var c in allCams)
        {
            if (c.gameObject == gameObject) continue;
            // Disable other cameras by default; re-enable per scene if needed
            c.enabled = false;
            var otherAL = c.GetComponent<AudioListener>();
            if (otherAL) otherAL.enabled = false;
        }

        // 2) Make sure *this* camera and its listener are enabled
        if (_cam) _cam.enabled = true;
        if (_al)  _al.enabled = true;

        // 3) If you tag your camera as MainCamera, ensure only one object has that tag
        if (CompareTag("MainCamera"))
        {
            foreach (var c in allCams)
            {
                if (c == _cam) continue;
                if (c.CompareTag("MainCamera")) c.tag = "Untagged";
            }
        }
    }

    // Public helpers you can call from your player/vehicle enter/exit code:
    public static void EnableBrain(bool enable)
    {
        if (!_instance) return;
        var b = _instance.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (b) b.enabled = enable;
    }

    public static Camera Camera => _instance ? _instance._cam : null;
}
