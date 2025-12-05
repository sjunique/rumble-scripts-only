using UnityEngine;
 
public class DontDestroyNotifier : MonoBehaviour
{
    string initialScene;
    void Awake() => initialScene = gameObject.scene.name;
    void Update()
    {
        if (gameObject.scene.name != initialScene)
        {
            Debug.LogWarning($"[DontDestroyNotifier] '{name}' moved to scene '{gameObject.scene.name}' (was '{initialScene}')");
            enabled = false;
        }
    }
}