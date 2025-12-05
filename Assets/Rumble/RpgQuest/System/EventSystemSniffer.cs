 #if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
 
using UnityEngine.SceneManagement;
public class EventSystemSniffer : MonoBehaviour
{
    void Start()
    {
        var all = FindObjectsOfType<EventSystem>(true);
        foreach (var es in all)
        {
            var root = es.transform.root;
            var sceneName = es.gameObject.scene.name;
            Debug.Log($"[ES] '{es.name}' in eventsystem '{sceneName}' root='{root.name}' path='{es.transform.GetHierarchyPathDebug()}'");
        }
    }
}
static class TFExt {
    public static string GetHierarchyPathDebug(this Transform t)
    {
        string p = t.name;
        while (t.parent) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}

#endif
