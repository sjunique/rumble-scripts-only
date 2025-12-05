using UnityEngine;
using Invector.vCharacterController;

public class AutopilotManager : MonoBehaviour
{
    public static AutopilotManager Instance { get; private set; }

    [Header("Hotkey")]
    public KeyCode toggleKey = KeyCode.T;
    public bool listenForHotkey = true;

    // cached live refs
    vThirdPersonController player;
    vThirdPersonInput inv;
    PathFollowerNudge nudge;
    KidModeAssist assist;
    PathNudgeInputMixer mixer;

    // last known route (set by binder or discovered)
    WaypointPathVisualizer lastPath;

    bool _isOn;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Resolve();
        Apply(false); // start OFF
    }

    void Update()
    {
        if (listenForHotkey && Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        Resolve();
        Apply(!_isOn);
        Debug.Log($"[AP] Autopilot = {(_isOn ? "ON" : "OFF")}");
    }

    public void EnableWithPath(WaypointPathVisualizer path)
    {
        lastPath = path ? path : lastPath;
        Resolve();
        Apply(true);
    }

    public void Enable()  { Resolve(); Apply(true); }
    public void Disable() { Resolve(); Apply(false); }

    public void RegisterPath(WaypointPathVisualizer path)
    {
        if (path) lastPath = path;
    }

    void Resolve()
    {
        var link = PlayerCarLinker.Instance;
        player = link ? link.player : null;
        inv    = player ? player.GetComponent<vThirdPersonInput>() : null;

        if (player)
        {
            nudge  = player.GetComponent<PathFollowerNudge>();
            assist = player.GetComponent<KidModeAssist>();
            mixer  = player.GetComponent<PathNudgeInputMixer>();
        }

        // If we don't have a path yet, try to find an active one
        if (!lastPath)
        {
            // Prefer an active, tagged path; else nearest active
            var all = FindObjectsOfType<WaypointPathVisualizer>(true);
            WaypointPathVisualizer best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var p in all)
            {
                if (!p.gameObject.activeInHierarchy) continue;
                if (!best && p.CompareTag("QuestPath")) { best = p; break; }
                if (!best)
                {
                    float d = (player ? (p.transform.position - player.transform.position).sqrMagnitude : 0f);
                    if (d < bestSqr) { bestSqr = d; best = p; }
                }
            }
            if (best) lastPath = best;
        }
    }

    void Apply(bool turnOn)
    {
        // Ensure components exist if we’re turning on
        if (turnOn && player)
        {
            if (!nudge)  nudge  = player.gameObject.GetComponent<PathFollowerNudge>() ?? player.gameObject.AddComponent<PathFollowerNudge>();
            if (!assist) assist = player.gameObject.GetComponent<KidModeAssist>()     ?? player.gameObject.AddComponent<KidModeAssist>();
        }

        // Reference frame MUST be the player
        if (nudge && player)
        {
            if (!nudge.playerRoot)     nudge.playerRoot     = player.transform;
            if (!nudge.referenceFrame) nudge.referenceFrame = player.transform;
        }

        if (turnOn)
        {
            // Arm route
            if (!lastPath && nudge) lastPath = nudge.path; // try existing binding
            if (nudge && lastPath)
            {
                nudge.path = lastPath;
                // DIRECT call strongly preferred (adjust signature if needed)
                try { nudge.SetRoute(lastPath, true, true, true); } catch { /* ignore if different sig */ }
            }

            if (assist)
            {
                if (!assist.playerRoot && player) assist.playerRoot = player.transform;
                if (!assist.visualizer && lastPath) assist.visualizer = lastPath;
                if (!assist.nudge) assist.nudge = nudge;
            }

            // Enable the trio
            if (assist) assist.enabled = true;
            if (nudge)  nudge.enabled  = true;
            if (mixer)  mixer.enabled  = true;

            // Keep player input alive
            if (player)
            {
                player.lockMovement = player.lockMovement; // no change
                player.lockRotation = player.lockRotation; // no change
            }
            if (inv) inv.enabled = true;

            _isOn = true;
        }
        else
        {
            // Disable the trio
            if (mixer)  mixer.enabled  = false;
            if (assist) assist.enabled = false;

            if (nudge)
            {
                // If your nudge exposes a stop/clear, call it:
                // try { nudge.ClearRoute(); } catch {}
                nudge.enabled = false;
            }

            // Unlock player for manual control
            if (player)
            {
                player.lockMovement = false;
                player.lockRotation = false;
            }
            if (inv) inv.enabled = true;

            _isOn = false;
        }
    }
}
