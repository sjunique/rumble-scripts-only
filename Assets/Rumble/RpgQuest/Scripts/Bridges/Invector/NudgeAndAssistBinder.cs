using UnityEngine;
using Invector.vCharacterController;
using System.Linq;
using System.Collections;
using System.Reflection;

[DefaultExecutionOrder(10)] // runs after GameLauncher(-50) & Linker(-1000) Start()
public class NudgeAndAssistBinder : MonoBehaviour
{
    [Header("Optional explicit refs")]
    public PathFollowerNudge nudge;          // will auto-add on player if missing
    public KidModeAssist kidAssist;          // will auto-add on player if missing
    public WaypointPathVisualizer pathHint;  // if set, uses this path

    [Header("Path selection")]
    public string questPathTag = "QuestPath";
    public bool preferTaggedPath = true;
    public bool chooseNearestIfNoTag = true;

    [Header("Behaviour")]
    public bool enableNudgeComponent = true;
    public bool enableAssistComponent = true;
    public bool preferPlayerAsReferenceFrame = true; // fixes “moves straight” issue
    public bool tryEnableAutopilotFlags = true;      // via reflection if fields exist

    Transform playerRoot;

    IEnumerator Start()
    {
        // Wait for Linker + Player to exist (in case we’re early)
        float t = 0f;
        while ((PlayerCarLinker.Instance == null || PlayerCarLinker.Instance.player == null) && t < 2f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (PlayerCarLinker.Instance == null || PlayerCarLinker.Instance.player == null)
        {
            Debug.LogWarning("[NAB] PlayerCarLinker or player missing; will retry next frame.");
            yield return null;
        }

        yield return null; // one more frame to let spawns settle
        Physics.SyncTransforms();

        RefreshBindings();
        var link = PlayerCarLinker.Instance;
var ok = nudge && nudge.referenceFrame == link.player.transform;
Debug.Log($"[NAB] referenceFrame is player? {ok}  (ref={(nudge? nudge.referenceFrame?.name : "<null>")})");

    }

    [ContextMenu("Refresh Now")]
    public void RefreshBindings()
    {
        var link = PlayerCarLinker.Instance;
        if (link == null || link.player == null)
        {
            Debug.LogError("[NAB] No PlayerCarLinker.Instance or link.player.");
            return;
        }

        playerRoot = link.player.transform;

        // Ensure components on the player
        nudge = nudge ? nudge : GetOrAdd<PathFollowerNudge>(playerRoot.gameObject);
        kidAssist = kidAssist ? kidAssist : GetOrAdd<KidModeAssist>(playerRoot.gameObject);

        // Resolve path
        var path = ResolvePath(playerRoot.position);
        if (!path)
        {
            Debug.LogWarning("[NAB] No WaypointPathVisualizer found. Nudge/Assist bound without a path.");
        }

        // Reference frame: prefer player to avoid “straight line” heading
        Transform referenceFrame = playerRoot;
        if (!preferPlayerAsReferenceFrame && Camera.main) referenceFrame = Camera.main.transform;

        // Wire Nudge
        if (nudge)
        {
            if (!nudge.path) nudge.path = path;
            if (!nudge.playerRoot) nudge.playerRoot = playerRoot;
            if (!nudge.referenceFrame) nudge.referenceFrame = referenceFrame;

            if (enableNudgeComponent) nudge.enabled = true;
            if (tryEnableAutopilotFlags) TryEnableAutopilot(nudge);
        }

        // Wire Kid Assist
        if (kidAssist)
        {
            if (!kidAssist.visualizer) kidAssist.visualizer = path;
            if (!kidAssist.nudge) kidAssist.nudge = nudge;

            if (!kidAssist.playerRoot) kidAssist.playerRoot = playerRoot;

            // if (!kidAssist.cc)  kidAssist.cc  = link.player;
            // if (!kidAssist.inv) kidAssist.inv = link.player.GetComponent<vThirdPersonInput>();

            if (enableAssistComponent) kidAssist.enabled = true;
            if (tryEnableAutopilotFlags) TryEnableAutopilot(kidAssist);
        }
// After wiring fields…
if (nudge)
{
    // Arm the route explicitly
   // TryCallSetRoute(nudge, path);
 
   ////// TryEnableAssistLikeMethods(nudge);
}

if (kidAssist)
{
   // TryEnableAssistLikeMethods(kidAssist);
}




        Debug.Log($"[NAB] Bound nudge/assist. path={(path ? path.name : "<none>")}, " +
                  $"player={playerRoot.name}, ref={(referenceFrame ? referenceFrame.name : "<none>")}");

        //Debug.Log($"[NAB] Bound nudge/assist. path={(path?path.name:\"<none>\")} player={playerRoot.name} ref={(referenceFrame?referenceFrame.name:\"<none>\")}");
    }

    WaypointPathVisualizer ResolvePath(Vector3 from)
    {
        if (pathHint) return pathHint;

        var all = FindObjectsOfType<WaypointPathVisualizer>(true);
        if (all == null || all.Length == 0) return null;

        if (preferTaggedPath)
        {
            var tagged = all.FirstOrDefault(p => p.CompareTag(questPathTag));
            if (tagged) return tagged;
        }

        if (!chooseNearestIfNoTag) return null;

        WaypointPathVisualizer best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var p in all)
        {
            float sq = (p.transform.position - from).sqrMagnitude;
            if (sq < bestSqr) { bestSqr = sq; best = p; }
        }
        return best;
    }

    T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (!c) c = go.AddComponent<T>();
        return c;
    }

    // Best-effort toggles for projects with different field/property names
    void TryEnableAutopilot(Component comp)
    {
        if (!comp) return;

        // Common names we’ve used across iterations
        string[] boolNames = {
            "autopilot", "autoPilot", "ap", "enableAutopilot", "kidMode", "assistEnabled",
            "nudgeEnabled", "pathAssist", "enabledOnStart"
        };

        var type = comp.GetType();
        foreach (var name in boolNames)
        {
            // property first
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
            {
                prop.SetValue(comp, true);
                Debug.Log($"[NAB] {type.Name}.{prop.Name}=true");
                // don’t break; set as many as are present
            }

            // field next
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(comp, true);
                Debug.Log($"[NAB] {type.Name}.{field.Name}=true");
            }
        }

        // Optional method patterns like EnableAutopilot(true)
        string[] methodNames = { "EnableAutopilot", "SetAutopilot", "SetAssist", "EnableAssist", "Enable" };
        foreach (var m in methodNames)
        {
            var mi = type.GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (mi == null) continue;
            var pars = mi.GetParameters();
            if (pars.Length == 1 && pars[0].ParameterType == typeof(bool))
            {
                mi.Invoke(comp, new object[] { true });
                Debug.Log($"[NAB] {type.Name}.{mi.Name}(true) invoked.");
            }
        }
    }

// --- add these helpers anywhere in the class ---

void TryCallSetRoute(PathFollowerNudge n, WaypointPathVisualizer p)
{
    if (!n || !p) return;

    // Prefer direct call if it’s in your API:
    // n.SetRoute(p, true, true, true);

    // If you’re not sure of the exact signature, use reflection:
    var t = n.GetType();
    var mi = t.GetMethod("SetRoute",
              System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    if (mi != null)
    {
        var parms = mi.GetParameters();
        try
        {
            if (parms.Length == 4 &&
                parms[0].ParameterType.IsAssignableFrom(typeof(WaypointPathVisualizer)) &&
                parms[1].ParameterType == typeof(bool) &&
                parms[2].ParameterType == typeof(bool) &&
                parms[3].ParameterType == typeof(bool))
            {
                mi.Invoke(n, new object[] { p, true, true, true });
                Debug.Log("[NAB] Called nudge.SetRoute(path, nudge:true, ap:true, start:true)");
                return;
            }
            // fallback common: (path, start)
            if (parms.Length == 2 &&
                parms[0].ParameterType.IsAssignableFrom(typeof(WaypointPathVisualizer)) &&
                parms[1].ParameterType == typeof(bool))
            {
                mi.Invoke(n, new object[] { p, true });
                Debug.Log("[NAB] Called nudge.SetRoute(path, start:true)");
                return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[NAB] SetRoute invoke failed: " + ex.Message, n);
        }
    }
}

void TryEnableAssistLikeMethods(Component comp)
{
    if (!comp) return;
    var t = comp.GetType();

    // Flip common bools
    string[] fields = { "autopilot", "autoPilot", "ap", "assistEnabled", "kidMode", "enabledOnStart", "nudgeEnabled" };
    foreach (var f in fields)
    {
        var fi = t.GetField(f,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
        if (fi != null && fi.FieldType == typeof(bool))
        {
            fi.SetValue(comp, true);
            Debug.Log($"[NAB] {t.Name}.{fi.Name}=true");
        }
        var pi = t.GetProperty(f,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
        if (pi != null && pi.CanWrite && pi.PropertyType == typeof(bool))
        {
            pi.SetValue(comp, true);
            Debug.Log($"[NAB] {t.Name}.{pi.Name}=true");
        }
    }

    // Try common methods like EnableAutopilot(true)
    string[] methods = { "EnableAutopilot", "SetAutopilot", "EnableAssist", "SetAssist", "Enable" };
    foreach (var m in methods)
    {
        var mi = t.GetMethod(m,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
        if (mi == null) continue;
        var ps = mi.GetParameters();
        try
        {
            if (ps.Length == 1 && ps[0].ParameterType == typeof(bool))
            {
                mi.Invoke(comp, new object[] { true });
                Debug.Log($"[NAB] {t.Name}.{mi.Name}(true)");
            }
        }
        catch { /* ignore */ }
    }
}




}