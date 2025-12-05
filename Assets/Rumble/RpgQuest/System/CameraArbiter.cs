// CameraArbiter.cs
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)] // run early
public sealed class CameraArbiter : MonoBehaviour
{
    [Header("Optional: choose which roles win if multiple present")]
    public CameraRole[] priority = new[] { CameraRole.Gameplay, CameraRole.Location, CameraRole.Menu, CameraRole.Cutscene };

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene from, Scene to)
    {
        // Delay one frame: let addressable/scene objects finish Awake/OnEnable
        StartCoroutine(SelectCameraNextFrame());
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        StartCoroutine(SelectCameraNextFrame());
    }

    System.Collections.IEnumerator SelectCameraNextFrame()
    {
        yield return new WaitForEndOfFrame();
        SelectAndNormalizeCameras();
    }

    void SelectAndNormalizeCameras()
    {
        var activeScene = SceneManager.GetActiveScene();

        // 1) Gather all *scene-owned* cameras (ignore DontDestroyOnLoad)
        var allCams = GameObject.FindObjectsOfType<Camera>(true);
        var sceneCams = allCams.Where(c => c && c.gameObject.scene.IsValid() && c.gameObject.scene == activeScene).ToList();

        if (sceneCams.Count == 0)
        {
            Debug.LogWarning($"[CameraArbiter] No scene cameras found in '{activeScene.name}'. Leaving existing state.");
            return;
        }

        // 2) Prefer a camera with a CameraRoleTag by priority, else first enabled
        Camera pick = null;
        var tagged = sceneCams
            .Select(c => new { cam = c, tag = c.GetComponent<CameraRoleTag>() })
            .Where(x => x.tag != null)
            .ToList();

        foreach (var role in priority)
        {
            var candidate = tagged.Where(x => x.tag.role == role && x.tag.preferThisCamera)
                                  .Select(x => x.cam)
                                  .FirstOrDefault();
            if (candidate) { pick = candidate; break; }
        }
        if (!pick) // fallback: first enabled camera in scene
            pick = sceneCams.FirstOrDefault(c => c.enabled) ?? sceneCams[0];

        // 3) Ensure only one MainCamera tag and one AudioListener
        foreach (var c in allCams)
        {
            if (!c) continue;

            // Disable listeners elsewhere by default (avoid warnings)
            var al = c.GetComponent<AudioListener>();
            if (al) al.enabled = (c == pick);

            // If it’s in the active scene and not the pick, untag and (optionally) disable
            if (c != pick && c.gameObject.scene == activeScene)
            {
                if (c.CompareTag("MainCamera")) c.tag = "Untagged";
                // Don’t forcibly disable—your GameHelpers or Brain may drive rigs; but do disable if it’s a full camera:
                // c.enabled = false; // <-- enable if you want only one enabled Camera component
            }
        }

        // 4) Make the pick the sole MainCamera
        if (!pick.CompareTag("MainCamera")) pick.tag = "MainCamera";
        pick.enabled = true; // ensure enabled
        var pickAL = pick.GetComponent<AudioListener>(); if (pickAL) pickAL.enabled = true;

        Debug.Log($"[CameraArbiter] Active='{activeScene.name}' pick='{pick.name}'. SceneCams={sceneCams.Count}, AllCams={allCams.Length}");
    }
}

