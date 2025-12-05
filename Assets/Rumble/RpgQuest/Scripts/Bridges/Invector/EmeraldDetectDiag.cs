using UnityEngine;
using System.Reflection;

public class EmeraldDetectDiag : MonoBehaviour
{
    void Start()
    {

       

        Debug.LogWarning($"DIAG[EmeraldDetectDiag] Checking {name}...");
        var em = GetComponent(typeof(Component).Assembly.GetType("EmeraldAI.EmeraldAISystem")) as Component;
        if (!em) { Debug.LogError($"{name}: EmeraldAISystem missing."); return; }

        string playerTag = GetStr(em, "PlayerTag");
        int angle = GetInt(em, "DetectionAngle");
        float radius = GetFloat(em, "DetectionRadius");
        int layerMaskInt = GetInt(em, "DetectionLayerMask");   // some versions
        var layerMaskLM = GetLayerMask(em, "DetectionLayers"); // other versions

        var maskVal = layerMaskLM.value != 0 ? layerMaskLM.value : layerMaskInt;
        Debug.LogWarning($"CHOMPER[EmeraldDetectDiag] {name} Tag={playerTag} Angle={angle} Radius={radius} LayersMask={maskVal}");

        // Warn if AI layer is included
        int aiLayer = LayerMask.NameToLayer("AI");
        if (aiLayer >= 0 && ((maskVal & (1 << aiLayer)) != 0))
            Debug.LogWarning($"{name}: AI layer is included in Detection Layers. Remove it.");

        // Warn if Default not included (your player is on Default)
        if ((maskVal & (1 << LayerMask.NameToLayer("Default"))) == 0)
            Debug.LogWarning($"{name}: Default layer not in Detection Layers; won’t detect the player.");
    }

    string GetStr(object o, string n){ var p=o.GetType().GetProperty(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); return p!=null?(p.GetValue(o) as string) ?? "":""; }
    int GetInt(object o, string n){ var p=o.GetType().GetProperty(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); if(p!=null && p.PropertyType==typeof(int)) return (int)p.GetValue(o); var f=o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); return f!=null && f.FieldType==typeof(int)?(int)f.GetValue(o):0; }
    float GetFloat(object o, string n){ var p=o.GetType().GetProperty(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); if(p!=null && p.PropertyType==typeof(float)) return (float)p.GetValue(o); var f=o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); return f!=null && f.FieldType==typeof(float)?(float)f.GetValue(o):0f; }
    LayerMask GetLayerMask(object o, string n){ var p=o.GetType().GetProperty(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); if(p!=null && p.PropertyType==typeof(LayerMask)) return (LayerMask)p.GetValue(o); var f=o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); return f!=null && f.FieldType==typeof(LayerMask)?(LayerMask)f.GetValue(o):default; }
}
