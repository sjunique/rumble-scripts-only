using UnityEngine;
 
using System.Diagnostics;

public class WhoSpawnedMe : MonoBehaviour
{
    void Awake()
    {
        var st = new StackTrace(1, true); // skip this frame, include file/line
        UnityEngine.Debug.Log($"[WhoSpawnedMe] Spawned: {name}\n{st}");
    }

    void OnEnable()
    {
        UnityEngine.Debug.Log($"[WhoSpawnedMe] Enabled: {name}");
    }
}
