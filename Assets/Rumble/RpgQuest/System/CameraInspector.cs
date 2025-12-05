using UnityEngine.SceneManagement;
using UnityEngine;
public class CameraInspector : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            var cams = FindObjectsOfType<Camera>(true);
            int mains = 0;
            foreach (var c in cams) if (c.CompareTag("MainCamera")) mains++;
            Debug.Log($"[CameraInspector] After loading '{s.name}', cameras={cams.Length}, MainCamera tag count={mains}.");
        };
    }
}
