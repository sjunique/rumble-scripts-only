using UnityEngine;
using UnityEngine.AI;
using System.Reflection;
using System.Collections;
using System;
using System.Linq;
 

[RequireComponent(typeof(NavMeshAgent))]
public class EmeraldFleeAdapter : MonoBehaviour
{
    [Header("Flee Steering")]
    public float fleeSeconds = 2.5f;     // default flee time if caller doesn't pass
    public float fleeDistance = 10f;     // how far we try to get away
    public float tick = 0.2f;            // how often we refresh destination
    public float initialWarp = 0.8f;     // small instant step away to break contact
    public float extraSpeedFactor = 1.2f;// temporarily run a bit faster

    [Header("Emerald Compat (optional)")]
    public bool pauseEmeraldWhileFlee = true;  // stop Emerald from immediately re-targeting
    public string emeraldNamespace = "EmeraldAI"; // in case the assembly name differs

    NavMeshAgent agent;
    Component emerald;    // EmeraldAI.EmeraldAISystem (via reflection)
    float baseSpeed;
    Coroutine fleeCo;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;

        // Try to find EmeraldAISystem without hard reference
      //  emerald = GetComponent(System.Type.GetType($"{emeraldNamespace}.EmeraldAISystem")) as Component;
var emerald = EmeraldTypeResolver.GetEmeraldSystemOn(gameObject);

        if (!emerald)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType($"{emeraldNamespace}.EmeraldAISystem");
                if (t != null) { emerald = GetComponent(t) as Component; break; }
            }
        }
    }

    /// <summary>External call from your laser or any hit logic.</summary>
    public void FleeFrom(Vector3 awayDir, float seconds, float initialKick)
    {
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.0001f) awayDir = transform.forward;
        awayDir.Normalize();

        if (fleeCo != null) StopCoroutine(fleeCo);
        fleeCo = StartCoroutine(FleeRoutine(awayDir, seconds > 0 ? seconds : fleeSeconds, initialKick));
    }

    IEnumerator FleeRoutine(Vector3 dir, float seconds, float initialKick)
    {
        float end = Time.time + seconds;

        // small warp to break overlap
        if (agent && initialWarp > 0f)
            agent.Warp(agent.transform.position + dir * Mathf.Max(initialWarp, 0.4f));

        float origAvoid = agent.obstacleAvoidanceType != 0 ? (int)agent.obstacleAvoidanceType : 0;
        float origAccel = agent.acceleration;
        agent.isStopped = false;
        agent.speed = baseSpeed * extraSpeedFactor;
        agent.acceleration = Mathf.Max(agent.acceleration, agent.speed * 4f);

        // Optional: softly pause Emerald’s combat AI so it doesn’t fight the steering
        object prevState = null;
        if (pauseEmeraldWhileFlee && emerald)
        {
            TryCall(emerald, "SetAIActive", false);         // some versions
            TrySet(emerald, "isDodging", true);             // others
            prevState = TryGet(emerald, "CurrentBehaviorState");
            TrySet(emerald, "CurrentBehaviorState", 0);     // 0 = idle (varies by version)
        }

        while (Time.time < end)
        {
            // steer away repeatedly
            Vector3 rawTarget = transform.position + dir * fleeDistance;
            if (NavMesh.SamplePosition(rawTarget, out var hit, 3.0f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            // micro warp helps when colliding with player shield
            agent.Warp(agent.transform.position + dir * 0.25f);

            yield return new WaitForSeconds(tick);
        }

        // restore
        agent.speed = baseSpeed;
        agent.acceleration = origAccel;

        if (pauseEmeraldWhileFlee && emerald)
        {
            TrySet(emerald, "CurrentBehaviorState", prevState);
            TryCall(emerald, "SetAIActive", true);
            TrySet(emerald, "isDodging", false);
        }
    }

    // ---- light reflection helpers (no hard Emerald dependency) ----
    static bool TryCall(object obj, string method, params object[] args)
    {
        if (obj == null) return false;
        var mi = obj.GetType().GetMethod(method, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (mi == null) return false;
        try { mi.Invoke(obj, args); return true; } catch { return false; }
    }
 static bool TrySet(object obj, string name, object value)
{
    if (obj == null) return false;
    var t = obj.GetType();
    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (p != null && p.CanWrite) { try { p.SetValue(obj, value); return true; } catch { } }
    var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (f != null) { try { f.SetValue(obj, value); return true; } catch { } }
    return false;
}

    static object TryGet(object obj, string name)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (p != null && p.CanRead) { try { return p.GetValue(obj); } catch { } }
        var f = t.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (f != null) { try { return f.GetValue(obj); } catch { } }
        return null;
    }
}

public static class EmeraldTypeResolver
{
    // Finds Emerald component type across old/new versions.
    public static Type FindEmeraldSystemType()
    {
        // Try the common full names first
        var t = Type.GetType("EmeraldAI.EmeraldSystem");
        if (t != null) return t;

        t = Type.GetType("EmeraldAISystem"); // very old
        if (t != null) return t;

        // Fallback: scan assemblies by simple name
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .FirstOrDefault(x =>
                x.Name == "EmeraldSystem" || x.FullName == "EmeraldAI.EmeraldSystem" ||
                x.Name == "EmeraldAISystem");
    }

    public static Component GetEmeraldSystemOn(GameObject go)
    {
        var t = FindEmeraldSystemType();
        if (t == null)
        {
            Debug.LogError("EmeraldTypeResolver: Emerald System type not found.");
            return null;
        }
        return go.GetComponent(t);
    }
}
