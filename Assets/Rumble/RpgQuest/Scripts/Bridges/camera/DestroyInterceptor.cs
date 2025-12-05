using UnityEngine;
// DestroyInterceptor.cs
 

public class DestroyInterceptor : MonoBehaviour
{
    void OnDisable()
    {
        // OnDisable runs BEFORE OnDestroy for explicit Destroy() calls
        Debug.LogError($"[DestroyInterceptor] '{name}' DISABLED in frame {Time.frameCount}. Likely Destroy() incoming.\n" +
                       $"Hierarchy path: {GetPath(transform)}");
    }

    void OnDestroy()
    {
        Debug.LogError($"[DestroyInterceptor] '{name}' DESTROYED in frame {Time.frameCount}.\n" +
                       $"Hierarchy path: {GetPath(transform)}");
    }

    private string GetPath(Transform t)
    {
        string path = "/" + t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = "/" + t.name + path;
        }
        return path;
    }
}
