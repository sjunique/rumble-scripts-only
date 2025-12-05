 

 
using UnityEngine;
 
using System.Collections;

 
using Invector;           // vThirdPersonCamera
using Invector.vCamera;
 
using Unity.Cinemachine;          // CM3
using Invector.vCamera;           // vThirdPersonCamera
using System.Collections;

public partial class CarCameraRig : MonoBehaviour
{
    public enum Mode { Car_Default, Player_Default }

    [Header("Scene Camera (Main)")]
    [SerializeField] private Camera mainCam;                 // MUST be the MainCamera
    [SerializeField] private CinemachineBrain brain;         // on the MainCamera
    [SerializeField] private vThirdPersonCamera invectorCamOnMain; // vTPC on MainCamera

    [Header("Car Cameras")]
    [SerializeField] private CinemachineCamera carTopDownCam;
    [SerializeField] private CinemachineCamera carFreeLookCam;
    [SerializeField] private CinemachineCamera carFirstPersonCam;

    [Header("First Person Mounts")]
    [SerializeField] private Transform fpUnderChassis;
    [SerializeField] private Transform fpWindshield;
    [SerializeField] private Transform fpNose;

    [Header("Player Camera")]
    [SerializeField] private CinemachineCamera playerFreeLookCam;

    // Hard-locked default hotkeys (kept from your latest)
    private const KeyCode TOP_DOWN_KEY = KeyCode.Alpha5;
    private const KeyCode FREE_LOOK_KEY = KeyCode.Alpha6;
    private const KeyCode FP_UNDER_KEY = KeyCode.Alpha7;
    private const KeyCode FP_SHIELD_KEY = KeyCode.Alpha8;
    private const KeyCode FP_NOSE_KEY = KeyCode.Alpha9;
    private const KeyCode PLAYER_CAM_KEY = KeyCode.Alpha0;

    // cached targets
    Transform carFollowTarget, carLookAtTarget;

    void Awake()
    {
        // Auto-wire the scene camera, brain, and stub vTPC
        if (!mainCam) mainCam = Camera.main;
        if (!mainCam && Camera.main) mainCam = Camera.main;
        if (!brain && mainCam) brain = mainCam.GetComponent<CinemachineBrain>();
        if (!invectorCamOnMain && mainCam) invectorCamOnMain = mainCam.GetComponent<vThirdPersonCamera>();

        // Safety: ensure MainCamera tag (aim rays & FindMain rely on it)
        if (mainCam && mainCam.tag != "MainCamera") mainCam.tag = "MainCamera";
    }

    // ---------- external API (unchanged calls) ----------

    public void InitializeForCar(GameObject carRoot, Transform driverSeat)
    {
        using (var _ = Diag.Begin("CAM", "InitForCar", this))
        {
            carFollowTarget = carRoot ? carRoot.transform : null;
            carLookAtTarget = driverSeat ? driverSeat : carFollowTarget;

            AssignTarget(carTopDownCam, carFollowTarget, carLookAtTarget);
            AssignTarget(carFreeLookCam, carFollowTarget, carLookAtTarget);

            if (carFirstPersonCam) carFirstPersonCam.LookAt = null; // FP uses mount Follow only

            Diag.Info("CAM", $"Targets set  follow={carFollowTarget?.name}  look={carLookAtTarget?.name}", this);

            // Player→Car policy: Cinemachine drives, Invector disabled
            SetInvectorEnabled(false);
            SetBrainEnabled(true);
        }
    }

 

public void InitializeForPlayer(Transform playerRoot)
{
    // You removed autodetect, that’s fine.
    // Make sure the Invector cam knows who to follow:
    BindInvectorTarget(playerRoot);

    // Player mode: Invector drives, Brain off
    SetBrainEnabled(false);
    SetInvectorEnabled(true);

    // Optional: snap the camera once so there’s no pop
    if (invectorCamOnMain) {
        var camT = invectorCamOnMain.transform;
        camT.position = playerRoot.position + Vector3.up * 1.6f; // rough eye height
        camT.rotation = Quaternion.LookRotation(playerRoot.forward, Vector3.up);
    }








}














    void BindInvectorTarget(Transform playerRoot)
    {
        if (!invectorCamOnMain || !playerRoot) return;

        // Try the common API first (varies by Invector version)
        var m = typeof(Invector.vCamera.vThirdPersonCamera).GetMethod("SetMainTarget");
        if (m != null) { m.Invoke(invectorCamOnMain, new object[] { playerRoot }); return; }

        // Fallback: set a public field if exposed in your version
        var f = typeof(Invector.vCamera.vThirdPersonCamera).GetField("target")
             ?? typeof(Invector.vCamera.vThirdPersonCamera).GetField("mainTarget");
        if (f != null) { f.SetValue(invectorCamOnMain, playerRoot); }
    }




    public void SetMode(Mode mode)
    {
        using (var _ = Diag.Begin("CAM", $"SetMode → {mode}", this))
        {
            switch (mode)
            {
                case Mode.Car_Default:
                    // pick your default driving cam (you can swap to carFreeLookCam if preferred)
                    SetActive(carFirstPersonCam);
                    SetInvectorEnabled(false);
                    SetBrainEnabled(true);
                    break;

                case Mode.Player_Default:
                    SetActive(playerFreeLookCam);
                    SetBrainEnabled(false);
                    SetInvectorEnabled(true);
                    break;
            }
        }
    }

    public void SetCarTargets(Transform follow, Transform lookAt)
    {
        AssignTarget(carTopDownCam, follow, lookAt);
        AssignTarget(carFreeLookCam, follow, lookAt);
    }

    public void SnapFirstPersonToMount(Transform mount)
    {
        using (var _ = Diag.Begin("CAM", $"SnapFP → {mount?.name}", this))
        {
            if (!carFirstPersonCam || !mount) { Diag.Warn("CAM", "FP snap skipped (null cam or mount)", this); return; }

            carFirstPersonCam.Follow = mount;
            carFirstPersonCam.LookAt = null;

            var panTilt = carFirstPersonCam.GetComponent<CinemachinePanTilt>();
            if (panTilt) { panTilt.PanAxis.Value = 0f; panTilt.TiltAxis.Value = 0f; }

            SetActive(carFirstPersonCam);
        }
    }

    // For binders to inject a player vcam explicitly (unchanged)
    public void SetPlayerFreeLookCam(CinemachineCamera cam)
    {
        playerFreeLookCam = cam;
#if UNITY_EDITOR
        if (!playerFreeLookCam) Debug.LogWarning("[CarCameraRig] Player FreeLook cam was set to null.");
#endif
    }

    void Update()
    {
        // quick test keys (unchanged)
        if (Input.GetKeyDown(TOP_DOWN_KEY)) SetActive(carTopDownCam);
        if (Input.GetKeyDown(FREE_LOOK_KEY)) SetActive(carFreeLookCam);
        if (Input.GetKeyDown(FP_UNDER_KEY) && fpUnderChassis) SnapFirstPersonToMount(fpUnderChassis);
        if (Input.GetKeyDown(FP_SHIELD_KEY) && fpWindshield) SnapFirstPersonToMount(fpWindshield);
        if (Input.GetKeyDown(FP_NOSE_KEY) && fpNose) SnapFirstPersonToMount(fpNose);
        if (Input.GetKeyDown(PLAYER_CAM_KEY)) SetActive(playerFreeLookCam);
    }

// // CarCameraRig.cs  (inside the class)
 public void ForcePlayerOwnership(Transform playerRoot, Camera sceneCam = null)
{
    // lock the scene's main camera references
    if (!sceneCam) sceneCam = Camera.main;
    if (!mainCam)  mainCam  = sceneCam ? sceneCam : Camera.main;
    if (!brain && mainCam) brain = mainCam.GetComponent<CinemachineBrain>();
    if (!invectorCamOnMain && mainCam) invectorCamOnMain = mainCam.GetComponent<Invector.vCamera.vThirdPersonCamera>();
    if (mainCam && mainCam.tag != "MainCamera") mainCam.tag = "MainCamera";

    // Player mode: Invector on, Brain off (so none of the car vcams can preempt)
    SetBrainEnabled(false);
    SetInvectorEnabled(true);

    // Bind target so WASD resolves from the correct view
    BindInvectorTarget(playerRoot);

    // Drop all Cinemachine priorities so even if Brain gets enabled later,
    // nothing steals control until you explicitly call InitializeForCar/SetMode(...)
    SetPriority(carTopDownCam,    0);
    SetPriority(carFreeLookCam,   0);
    SetPriority(carFirstPersonCam,0);
    SetPriority(playerFreeLookCam,0);

    // Optional: initial snap to avoid a “pop”
    if (invectorCamOnMain && playerRoot)
    {
        var t = invectorCamOnMain.transform;
        t.position = playerRoot.position + Vector3.up * 1.6f;
        t.rotation = Quaternion.LookRotation(playerRoot.forward, Vector3.up);
    }

#if UNITY_EDITOR
    Debug.Log("[CarCameraRig] Forced Player ownership at game start.");
#endif
}



    // ---------- internals ----------

    CinemachineCamera TryAutodetectPlayerCam(Transform playerRoot)
    {
        if (!playerRoot) return null;

        // Prefer a cam under the player hierarchy that already follows/looks at the player
        var cams = playerRoot.GetComponentsInChildren<CinemachineCamera>(true);
        foreach (var c in cams)
        {
            var n = c.gameObject.name.ToLowerInvariant();
            if (n.Contains("player") || n.Contains("freelook") || n.Contains("third") || n.Contains("tps"))
                return c;
            if (c.Follow == playerRoot || c.LookAt == playerRoot)
                return c;
        }

        // Fallback: any scene cam that targets the player (and isn’t one of the car cams)
        var all = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c == carTopDownCam || c == carFreeLookCam || c == carFirstPersonCam) continue;
            if (c.Follow == playerRoot || c.LookAt == playerRoot) return c;
        }
#if UNITY_EDITOR
        Debug.LogWarning("[CarCameraRig] Could not auto-detect a Player FreeLook CinemachineCamera.");
#endif
        return null;
    }

    void AssignTarget(CinemachineCamera cam, Transform follow, Transform lookAt)
    {
        if (!cam) return;
        cam.Follow = follow;
        cam.LookAt = lookAt;
    }

    void SetActive(CinemachineCamera active)
    {
        using (var _ = Diag.Begin("CAM", $"SetActive → {active?.Name}", this))
        {
            SetPriority(carTopDownCam, active == carTopDownCam ? 100 : 0);
            SetPriority(carFreeLookCam, active == carFreeLookCam ? 100 : 0);
            SetPriority(carFirstPersonCam, active == carFirstPersonCam ? 100 : 0);
            SetPriority(playerFreeLookCam, active == playerFreeLookCam ? 100 : 0);

            if (isActiveAndEnabled) StartCoroutine(LogLiveNextFrame());
        }
    }

    IEnumerator LogLiveNextFrame()
    {
        yield return null; // let Brain evaluate once
        var b = brain ? brain : (Camera.main ? Camera.main.GetComponent<CinemachineBrain>() : null);
        if (b) Diag.Info("CAM", $"Live={b.ActiveVirtualCamera?.Name}   Blend={b.ActiveBlend}", b);
    }

    void SetPriority(CinemachineCamera cam, int p) { if (cam) cam.Priority = p; }

    void SetBrainEnabled(bool on)
    {
        if (!brain) return;
        brain.enabled = on;
    }

    void SetInvectorEnabled(bool on)
    {
        if (!invectorCamOnMain && mainCam)
            invectorCamOnMain = mainCam.GetComponent<vThirdPersonCamera>();

        if (!invectorCamOnMain) return;

        invectorCamOnMain.enabled = on;
        // IMPORTANT: never disable mainCam.gameObject — it holds Camera/Brain/AudioListener.
    }
}






/*
public partial class CarCameraRig : MonoBehaviour
{
    public enum Mode { Car_Default, Player_Default }

    [Header("References")]
    [SerializeField] private CinemachineBrain brain;

    [Header("Car Cameras")]
    [SerializeField] private CinemachineCamera carTopDownCam;
    [SerializeField] private CinemachineCamera carFreeLookCam;
    [SerializeField] private CinemachineCamera carFirstPersonCam;

    [Header("First Person Mounts")]
    [SerializeField] private Transform fpUnderChassis;
    [SerializeField] private Transform fpWindshield;
    [SerializeField] private Transform fpNose;

    [Header("Player Camera")]
    [SerializeField] private CinemachineCamera playerFreeLookCam;

    [Header("Hotkeys (optional)")]
    // [SerializeField] private KeyCode topDownKey = KeyCode.Alpha5;
    // [SerializeField] private KeyCode freeLookKey = KeyCode.Alpha6;
    // [SerializeField] private KeyCode fpUnderKey = KeyCode.Alpha7;
    // [SerializeField] private KeyCode fpShieldKey = KeyCode.Alpha8;
    // [SerializeField] private KeyCode fpNoseKey = KeyCode.Alpha9;
    // [SerializeField] private KeyCode playerCamKey = KeyCode.Alpha0;

    private const KeyCode TOP_DOWN_KEY = KeyCode.Alpha5;
    private const KeyCode FREE_LOOK_KEY = KeyCode.Alpha6;
    private const KeyCode FP_UNDER_KEY = KeyCode.Alpha7;
    private const KeyCode FP_SHIELD_KEY = KeyCode.Alpha8;
    private const KeyCode FP_NOSE_KEY = KeyCode.Alpha9;
    private const KeyCode PLAYER_CAM_KEY = KeyCode.Alpha0;





    Transform carFollowTarget;
    Transform carLookAtTarget;

    void Awake()
    {
        if (!brain && Camera.main) brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    public void InitializeForCar(GameObject carRoot, Transform driverSeat)
    {


        using (var op = Diag.Begin("CAM", "InitForCar", this))
        {
            carFollowTarget = carRoot ? carRoot.transform : null;
            carLookAtTarget = driverSeat ? driverSeat : carFollowTarget;

            AssignTarget(carTopDownCam, carFollowTarget, carLookAtTarget);
            AssignTarget(carFreeLookCam, carFollowTarget, carLookAtTarget);

            if (carFirstPersonCam) carFirstPersonCam.LookAt = null;

            Diag.Info("CAM", $"Targets set  follow={carFollowTarget?.name}  look={carLookAtTarget?.name}", this);
        }
    }


    public void InitializeForPlayer(Transform playerRoot)
    {
        // If already assigned, keep it; otherwise try to auto-detect
        // if (playerFreeLookCam == null)
        //     playerFreeLookCam = TryAutodetectPlayerCam(playerRoot);

        // using (var op = Diag.Begin("CAM", "InitForPlayer", this))
        // {
        //     AssignTarget(playerFreeLookCam, playerRoot, playerRoot);
        //     SetActive(playerFreeLookCam);
        // }
        if (!brain && Camera.main) brain = Camera.main.GetComponent<CinemachineBrain>();

    }


    private CinemachineCamera TryAutodetectPlayerCam(Transform playerRoot)
    {
        if (!playerRoot)
            return null;

        // 1) Prefer a CinemachineCamera under the player hierarchy
        var cams = playerRoot.GetComponentsInChildren<CinemachineCamera>(true);
        foreach (var c in cams)
        {
            // Heuristics: name hints for player freelook
            string n = c.gameObject.name.ToLowerInvariant();
            if (n.Contains("player") || n.Contains("freelook") || n.Contains("third") || n.Contains("tps"))
                return c;

            // Or if it already follows/looks at the player
            if (c.Follow == playerRoot || c.LookAt == playerRoot)
                return c;
        }

        // 2) Fallback: search the scene for a cam that follows the player
        var all = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c == carTopDownCam || c == carFreeLookCam || c == carFirstPersonCam) continue;
            if (c.Follow == playerRoot || c.LookAt == playerRoot)
                return c;
        }

#if UNITY_EDITOR
        Debug.LogWarning("[CarCameraRig] Could not auto-detect a Player FreeLook CinemachineCamera.");
#endif
        return null;
    }


    public void SetMode(Mode mode)
    {
        using (var op = Diag.Begin("CAM", $"SetMode → {mode}", this))
        {
            switch (mode)
            {
                case Mode.Car_Default:
                    SetActive(carFirstPersonCam); // default; you can change to carFreeLookCam if preferred
                    break;
                case Mode.Player_Default:
                    SetActive(playerFreeLookCam);
                    break;
            }
        }
    }

    public void SetCarTargets(Transform follow, Transform lookAt)
    {
        AssignTarget(carTopDownCam, follow, lookAt);
        AssignTarget(carFreeLookCam, follow, lookAt);
    }

    public void SnapFirstPersonToMount(Transform mount)
    {
        using (var op = Diag.Begin("CAM", $"SnapFP → {mount?.name}", this))
        {
            if (!carFirstPersonCam || !mount) { Diag.Warn("CAM", "FP snap skipped (null cam or mount)", this); return; }

            carFirstPersonCam.Follow = mount;
            carFirstPersonCam.LookAt = null;

            var panTilt = carFirstPersonCam.GetComponent<CinemachinePanTilt>();
            if (panTilt)
            {
                panTilt.PanAxis.Value = 0f;
                panTilt.TiltAxis.Value = 0f;
            }

            SetActive(carFirstPersonCam);
        }
    }


    // Public setter so external binders can inject the player cam explicitly
    public void SetPlayerFreeLookCam(CinemachineCamera cam)
    {
        playerFreeLookCam = cam;
#if UNITY_EDITOR
        if (playerFreeLookCam == null)
            Debug.LogWarning("[CarCameraRig] Player FreeLook cam was set to null.");
#endif
    }

    void Update()
    {
        // optional keyboard switching for quick tests
        if (Input.GetKeyDown(TOP_DOWN_KEY)) SetActive(carTopDownCam);
        if (Input.GetKeyDown(FREE_LOOK_KEY)) SetActive(carFreeLookCam);
        if (Input.GetKeyDown(FP_UNDER_KEY) && fpUnderChassis) SnapFirstPersonToMount(fpUnderChassis);
        if (Input.GetKeyDown(FP_SHIELD_KEY) && fpWindshield) SnapFirstPersonToMount(fpWindshield);
        if (Input.GetKeyDown(FP_NOSE_KEY) && fpNose) SnapFirstPersonToMount(fpNose);
        if (Input.GetKeyDown(PLAYER_CAM_KEY)) SetActive(playerFreeLookCam);
    }

    private void AssignTarget(CinemachineCamera cam, Transform follow, Transform lookAt)
    {
        if (!cam) return;
        cam.Follow = follow;
        cam.LookAt = lookAt;
    }

    private void SetActive(CinemachineCamera active)
    {
        using (var op = Diag.Begin("CAM", $"SetActive → {active?.Name}", this))
        {
            SetPriority(carTopDownCam, active == carTopDownCam ? 100 : 0);
            SetPriority(carFreeLookCam, active == carFreeLookCam ? 100 : 0);
            SetPriority(carFirstPersonCam, active == carFirstPersonCam ? 100 : 0);
            SetPriority(playerFreeLookCam, active == playerFreeLookCam ? 100 : 0);

            // only run the debug log coroutine if this rig is enabled & active
            if (isActiveAndEnabled) StartCoroutine(LogLiveNextFrame());
        }
    }

    private IEnumerator LogLiveNextFrame()
    {
        yield return null; // let Brain evaluate once
        var b = brain ? brain : (Camera.main ? Camera.main.GetComponent<CinemachineBrain>() : null);
        if (b) Diag.Info("CAM", $"Live={b.ActiveVirtualCamera?.Name}   Blend={b.ActiveBlend}", b);
    }

    private void SetPriority(CinemachineCamera cam, int p)
    {
        if (cam) cam.Priority = p;
    }
}
 
 */
