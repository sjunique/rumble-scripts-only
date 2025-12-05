using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

public class EmeraldRuntimeGuard : MonoBehaviour
{
    public string playerTag = "Player";
    public LayerMask detectLayers; // set to LayerMask.GetMask("Default") in Inspector

    void Awake()
    {
        var anim = GetComponent<Animator>();
        var agent = GetComponent<NavMeshAgent>();
        if (!anim) Debug.LogError($"{name}: Missing Animator.");
        if (!agent) Debug.LogError($"{name}: Missing NavMeshAgent.");

        var em = GetComponent(typeof(Component).Assembly.GetType("EmeraldAI.EmeraldAISystem")) as Component;
        if (!em) { Debug.LogError($"{name}: EmeraldAISystem not found."); return; }

        // Set PlayerTag and Detection settings via reflection (covers version differences)
        TrySet(em, "PlayerTag", playerTag);
        TrySet(em, "DetectionAngle", 360);
        TrySet(em, "DetectionRadius", 35f);
        TrySet(em, "DetectionLayers", detectLayers);
        TrySet(em, "DetectionLayerMask", detectLayers.value); // some versions use int

        // Ensure a CurrentFaction string exists (prevents some nulls during comparisons)
        TrySet(em, "CurrentFaction", "Creatures");
    }

    static void TrySet(object obj, string name, object value)
    {
        var t = obj.GetType();
        var f = t.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (f != null)
        {
            if (f.FieldType == typeof(int) && value is LayerMask lm) f.SetValue(obj, lm.value);
            else f.SetValue(obj, value);
            return;
        }
        var p = t.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (p != null && p.CanWrite)
        {
            if (p.PropertyType == typeof(int) && value is LayerMask lm) p.SetValue(obj, lm.value);
            else p.SetValue(obj, value);
        }
    }
}
