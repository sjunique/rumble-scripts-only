// Assets/Rumble/RpgQuest/Bridges/Vehicles/PlayerCarLinker.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Invector.vCharacterController;
using Unity.Cinemachine;


using System.Reflection;
public class PlayerCarLinker : MonoBehaviour
{
    public static PlayerCarLinker Instance { get; private set; }

    [Header("Lifecycle")]
    public bool keepAlive = true;
  private bool verbose = false;
    [Header("Resolved at runtime (instances, not prefabs)")]
    public vThirdPersonController player;
    public GameObject carRoot;
    public Transform driverSeat;
    public Transform exitPoint;
    public RpgHoverController carController;
    public CarCameraRig carCameras;
    public MountTeleporter teleporter;

    [Header("Child names to auto-find under car")]
    [SerializeField] string seatChildName = "DriverSeat";
    [SerializeField] string exitChildName = "PlayerExitPosition";

    [Header("Auto-bind polling")]
    [SerializeField] float pollEvery = 0.25f;
    [SerializeField] float pollForSeconds = 10f;

    Coroutine pollCo;

    void Awake()
    {
        Instance = this;
        if (keepAlive) DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartPolling();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopPolling();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Clear stale per-scene refs
        player = null; carRoot = null; driverSeat = null; exitPoint = null;
        carController = null; carCameras = null; teleporter = null;
        StartPolling();
    }

    // Public entry if your launcher wants to push explicit instances
    public void Initialize(
        vThirdPersonController p, GameObject car, Transform seat, Transform exit,
        RpgHoverController controller = null, CarCameraRig cameras = null, MountTeleporter tp = null)
    {
        player = p;
        carRoot = car;
        driverSeat = seat ? seat : ResolveChild(carRoot, seatChildName);
        exitPoint = exit ? exit : ResolveChild(carRoot, exitChildName);
        carController = controller ?? (carRoot ? carRoot.GetComponent<RpgHoverController>() : null);
        carCameras = cameras ?? FindObjectOfType<CarCameraRig>(true);
        teleporter = tp ?? FindObjectOfType<MountTeleporter>(true);

        SanitizeSeatAndExit();
        PushIntoAll();
    }

    // ---------- polling ----------
    void StartPolling()
    {
        if (pollCo != null) StopCoroutine(pollCo);
        pollCo = StartCoroutine(PollUntilBound());
    }

    void StopPolling()
    {
        if (pollCo != null) { StopCoroutine(pollCo); pollCo = null; }
    }

    IEnumerator PollUntilBound()
    {
        float until = Time.time + Mathf.Max(0.1f, pollForSeconds);

        while (Time.time < until)
        {
            AutoResolve();
            SanitizeSeatAndExit();

            if (player && carRoot && exitPoint)
            {
                //                Debug.Log($"[Linker] Bound → player={player.name}, car={carRoot.name}, exit={exitPoint.name}");
                PushIntoAll();
                yield break;
            }
            yield return new WaitForSeconds(pollEvery);
        }

        Debug.LogWarning("[Linker] Auto-bind timed out. If you spawn late, call Initialize(...) from the launcher.");
    }

    // ---------- resolve & push ----------
   // ---------- resolve & push ----------
void AutoResolve()
{
    if (!player) player = FindObjectOfType<vThirdPersonController>(true);
    if (!carRoot)
    {
        // Prefer a car with controller
        var ctrl = FindObjectOfType<RpgHoverController>(true);
        if (ctrl)
        {
            carRoot = ctrl.gameObject;
            carController = ctrl;
        }
        else
        {
            // Try tags: "Vehicle" first, then "CarRoot"
            var go = FindWithTagSafe("Vehicle");
            if (!go) go = FindWithTagSafe("CarRoot");

            // Optional: name-based fallbacks if tags aren't set
            if (!go) go = GameObject.Find("CarRoot");
            if (!go) go = GameObject.Find("Vehicle");

            carRoot = go;

            // If we found a root, try to grab a controller from it
            if (carRoot && !carController)
                carController = carRoot.GetComponentInChildren<RpgHoverController>(true);
        }
    }

    if (!carController && carRoot) carController = carRoot.GetComponent<RpgHoverController>();

    // Resolve car cameras: prefer cameras under the carRoot (children), fall back to scene
    if (!carCameras)
        carCameras = ResolveCarCamerasUnderRoot() ?? FindObjectOfType<CarCameraRig>(true);

    if (!teleporter) teleporter = FindObjectOfType<MountTeleporter>(true);

    if (carRoot)
    {
        if (!driverSeat) driverSeat = ResolveChild(carRoot, seatChildName);
        if (!exitPoint) exitPoint = ResolveChild(carRoot, exitChildName);

        // ensure we have an exit point (create a safe default if necessary)
        EnsureExitPoint();
    }
}

void SanitizeSeatAndExit()
{
    if (!carRoot) return;

    if (!driverSeat || driverSeat == carRoot.transform || driverSeat.name == carRoot.name)
        driverSeat = ResolveChild(carRoot, seatChildName);

    if (!exitPoint || exitPoint == carRoot.transform || exitPoint.name == carRoot.name)
        exitPoint = ResolveChild(carRoot, exitChildName);

    // final safety: if still null -> create an auto exit so other systems never encounter null
    EnsureExitPoint();
}

// --- helpers added to PlayerCarLinker ---
Transform ResolveChildSafe(GameObject root, string name)
{
    return ResolveChild(root, name);
}

/// <summary>
/// Try to find a CarCameraRig under the carRoot first (child/descendant).
/// Returns first matching CarCameraRig instance found within the carRoot children.
/// </summary>
CarCameraRig ResolveCarCamerasUnderRoot()
{
    if (carRoot == null) return null;
    var rigs = carRoot.GetComponentsInChildren<CarCameraRig>(true);
    if (rigs != null && rigs.Length > 0) return rigs[0];
    return null;
}

/// <summary>
/// Ensure there is an exitPoint Transform. If missing, create a small placeholder under carRoot.
/// The created placeholder is named PlayerExitPosition_Auto to match your expected child name pattern.
/// </summary>
void EnsureExitPoint()
{
    if (exitPoint != null) return;

    if (carRoot == null) return;

    // Try to resolve by name again (robust attempt)
    var found = ResolveChild(carRoot, exitChildName);
    if (found != null)
    {
        exitPoint = found;
        return;
    }

    // Create a placeholder Transform under carRoot
    var go = new GameObject(exitChildName + "_Auto");
    go.transform.SetParent(carRoot.transform, false);

    // position slightly behind/right of car root if possible (safe default)
    var carCenter = carRoot.transform.position;
    go.transform.position = carCenter + Vector3.right * 1.2f + Vector3.up * 0.2f;

    exitPoint = go.transform;

    if (verbose) Debug.Log($"[Linker] Created auto-exit '{go.name}' under '{carRoot.name}' at {go.transform.position}");
}

    private static GameObject FindWithTagSafe(string tag)
    {
        try { return GameObject.FindWithTag(tag); }
        catch { return null; } // tag might not exist
    }


 
    static Transform ResolveChild(GameObject root, string name)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] && all[i].name == name) return all[i];
        return null;
    }

    void PushIntoAll()
    {
        // 1) Summoner ← player, car, referenceFrame
        var summoners = FindObjectsOfType<WaterCarSummon>(true);
        foreach (var s in summoners)
        {
            s.AssignLinked(player, carRoot); // explicit public hook. :contentReference[oaicite:3]{index=3}
            if (!s.referenceFrame)
            {
                var cam = Camera.main ? Camera.main.transform : (player ? player.transform : null);
                s.referenceFrame = cam; // used by NearPlayer spawn. :contentReference[oaicite:4]{index=4}
            }
        }

        // 2) Car camera rig ← brain + player FreeLook cam
        var rigs = FindObjectsOfType<CarCameraRig>(true);
        foreach (var rig in rigs)
        {
            // brain auto-fills from Camera.main inside Awake if null. :contentReference[oaicite:5]{index=5}
            // Inject player freelook if we can find one:
            var pCam = FindPlayerFreeLook(player ? player.transform : null, rig);
            if (pCam) rig.SetPlayerFreeLookCam(pCam);               // :contentReference[oaicite:6]{index=6}
            if (player) rig.InitializeForPlayer(player.transform);  // sets Follow/LookAt & activates. :contentReference[oaicite:7]{index=7}
        }

        // 3) Enter/Exit binder ← all the refs
        var portals = FindObjectsOfType<CarTeleportEnterExit>(true);
        foreach (var e in portals)
        {
            e.SetLinkedPlayer(player); // sets caches. :contentReference[oaicite:8]{index=8}
            e.SetCar(carRoot, driverSeat, exitPoint, carController, carCameras, teleporter); // :contentReference[oaicite:9]{index=9}

            // If there’s a WaterCarSummon in the scene, wire the closest as its summoner if empty
            if (summoners.Length > 0)
            {
                var so = e.GetType().GetField("carSummoner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (so != null && so.GetValue(e) == null) so.SetValue(e, summoners[0]); // private field in your class
            }
        }
    }

    CinemachineCamera FindPlayerFreeLook(Transform playerRoot, CarCameraRig rig)
    {
        if (!playerRoot) return null;

        // Prefer a CinemachineCamera under the player (FreeLook/Third/Player in name)
        var cams = playerRoot.GetComponentsInChildren<CinemachineCamera>(true);
        foreach (var c in cams)
        {
            var n = c.gameObject.name.ToLowerInvariant();
            if (n.Contains("player") || n.Contains("freelook") || n.Contains("third") || n.Contains("tps"))
                return c;
            if (c.Follow == playerRoot || c.LookAt == playerRoot) return c;
        }

        // Fallback: any scene cam that follows the player and isn't one of the car cams
        var all = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c == null) continue;
            if (rig != null)
            {
                // skip the rig’s own car cams (we only want the player one)
                var isRigCam = false;
                // can't access private fields directly; ok to leave as heuristic
                if (c.Name.ToLower().Contains("car")) isRigCam = true;
                if (isRigCam) continue;
            }
            if (playerRoot && (c.Follow == playerRoot || c.LookAt == playerRoot))
                return c;
        }
        return null;
    }


    // at top (with your other usings)


    // ------------------------
    // Add inside PlayerCarLinker class
    // ------------------------

    public void PushInto(WaterCarSummon s)
    {
        if (!s) return;
        // player & car
        s.AssignLinked(player, carRoot);
        // reference frame for Near Player spawn (camera first, then player)
        if (!s.referenceFrame)
            s.referenceFrame = Camera.main ? Camera.main.transform : player ? player.transform : null;
    }

    public void PushInto(CarTeleportEnterExit e)
    {
        if (!e) return;
        // player & all car refs
        e.SetLinkedPlayer(player);
        e.SetCar(carRoot, driverSeat, exitPoint, carController, carCameras, teleporter);

        // if CarTeleportEnterExit has a private 'carSummoner' and it's empty, fill it
        var fld = typeof(CarTeleportEnterExit).GetField("carSummoner",
                BindingFlags.NonPublic | BindingFlags.Instance);
        if (fld != null && fld.GetValue(e) == null)
        {
            var s = FindObjectOfType<WaterCarSummon>(true);
            if (s) fld.SetValue(e, s);
        }
    }
    public void PushInto(CarCameraRig rig)
    {
        if (!rig) return;

        // Find a player-following CinemachineCamera (FreeLook or 3rd-person) and inject it
        var pCam = FindPlayerFreeLook(player ? player.transform : null, rig);
        if (pCam) rig.SetPlayerFreeLookCam(pCam);

        // Wire Follow/LookAt to the player and activate the correct camera
        if (player) rig.InitializeForPlayer(player.transform);

        // NOTE: CarCameraRig will handle its CinemachineBrain internally in Awake,
        // so we do not (and cannot) set rig.brain here.
    }

    // Convenience generic so old call sites that pass 'Component' still compile
    public void PushInto(Component c)
    {
        if (!c) return;
        if (c is WaterCarSummon s) PushInto(s);
        else if (c is CarTeleportEnterExit e) PushInto(e);
        else if (c is CarCameraRig r) PushInto(r);
        else Debug.LogWarning($"[Linker] PushInto: unsupported target '{c.GetType().Name}'.");
    }



}
