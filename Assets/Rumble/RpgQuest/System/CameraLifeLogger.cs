using UnityEngine;

public class CameraLifeLogger : MonoBehaviour
{
    void OnDisable()  { Debug.Log($"[CameraLife] {name} disabled at {Time.frameCount}."); }
    void OnDestroy()  { Debug.Log($"[CameraLife] {name} destroyed at {Time.frameCount}."); }
    void Start()      { Debug.Log($"[CameraLife] {name} started in scene '{gameObject.scene.name}'."); }
}
