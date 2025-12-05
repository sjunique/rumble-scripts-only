using UnityEngine;

 
 

[DisallowMultipleComponent]
public class SceneCameraBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (gameObject.scene.name != null)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[SceneCameraBootstrap] DDOL camera '{name}' alive. scene='{gameObject.scene.name}' frame={Time.frameCount}");
        }
    }

    void OnEnable() => Debug.Log($"[SceneCameraBootstrap] Enabled {name}");
    void OnDisable() => Debug.Log($"[SceneCameraBootstrap] Disabled {name}");
    void OnDestroy() => Debug.LogWarning($"[SceneCameraBootstrap] Destroyed {name}");
}




// [DefaultExecutionOrder(-1000)]
// public class SceneCameraBootstrap : MonoBehaviour
// {
//     void Awake()
//     {
//         if (gameObject == null) return;
//         // Make sure this camera persists
//         DontDestroyOnLoad(gameObject);
//         Debug.Log($"[SceneCameraBootstrap] Marked '{gameObject.name}' as DontDestroyOnLoad in Awake.");
//         // Attach watcher if not already attached
//         if (GetComponent<CameraDestroyWatcher>() == null)
//             gameObject.AddComponent<CameraDestroyWatcher>();
//     }
// }
