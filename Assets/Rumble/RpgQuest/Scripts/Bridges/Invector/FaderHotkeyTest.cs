 
using System.Collections;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // <-- Add this line

public class FaderHotkeyTest : MonoBehaviour
{
    public ScreenFader fader;

    // Add a dictionary to hold spawns
    public Dictionary<string, Transform> spawns = new Dictionary<string, Transform>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) StartCoroutine(DumpSpawns());
    }

    // DumpSpawns should be an IEnumerator for StartCoroutine
    public IEnumerator DumpSpawns()
    {
        foreach (var kv in spawns)
        {
            var sp = kv.Value;
            if (!sp) continue;
            var scene = sp.gameObject.scene.name;
            // Replace Diag.Info with Debug.Log for demonstration
            Debug.Log($"Key='{kv.Key}'  Pos={sp.transform.position}  [{scene}] {GetHierarchyPath(sp.transform)}");
        }
        yield return null;
    }

    // Stub for GetHierarchyPath
    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
