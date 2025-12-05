using UnityEngine;

// GlobalDestroyLogger.cs
using UnityEngine;
using System.Collections.Generic;

public class GlobalDestroyLogger : MonoBehaviour
{
    private static HashSet<int> known = new HashSet<int>();

    void Update()
    {
        // Track destruction by detecting missing instance IDs
        GameObject[] all = FindObjectsOfType<GameObject>();
        foreach (var go in all)
        {
            if (!known.Contains(go.GetInstanceID()))
                known.Add(go.GetInstanceID());
        }

        // This catches destroyed objects (ID disappears)
        known.RemoveWhere(id =>
        {
            GameObject go = null;
            // Try find something with this instance ID by brute
            bool exists = false;
            foreach (var obj in all)
                if (obj.GetInstanceID() == id)
                {
                    exists = true;
                    go = obj;
                    break;
                }

            if (!exists)
                Debug.LogError($"[GlobalDestroyLogger] Object with ID {id} destroyed this frame {Time.frameCount}.");

            return !exists;
        });
    }
}
