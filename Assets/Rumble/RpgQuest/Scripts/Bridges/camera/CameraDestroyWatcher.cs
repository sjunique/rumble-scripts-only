using UnityEngine;

// CameraDestroyWatcher.cs — attach to your SceneCamera
using UnityEngine;
using System;

public class CameraDestroyWatcher : MonoBehaviour
{
    void OnDestroy()
    {
        Debug.LogError($"[CameraDestroyWatcher] Camera '{name}' destroyed. Stack:\n{Environment.StackTrace}");
    }
}
