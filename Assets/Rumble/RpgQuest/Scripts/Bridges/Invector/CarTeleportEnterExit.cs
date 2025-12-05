using System;
using UnityEngine;
using Invector.vCharacterController;
using UnityEngine.AddressableAssets; // (kept from your original)
using UnityEngine.ResourceManagement.AsyncOperations; // (kept)
using System.Collections;
using RpgQuest.Utilities; // for Diag + [Mandatory]

public class CarTeleportEnterExit : MonoBehaviour
{

    ImmunityController imm;

    // ── Events ───────────────────────────────────────────────────────────────────
    /// <summary>Fired after the player is seated and car control/cameras are set.</summary>
    public event Action OnPlayerEnter;
    /// <summary>Fired after the player exits and player control/cameras are set.</summary>
    public event Action OnPlayerExit;

    // ── Config (kept from your original) ─────────────────────────────────────────
    [SerializeField] private Behaviour[] extraCarDriveScripts; // e.g., input readers on the car

    [Mandatory][SerializeField] private WaterCarSummon carSummoner;
    [SerializeField] private bool summonCarToDockOnStart = false;   // (kept, unused unless you wire it)
    [Mandatory][SerializeField] private bool summonBeforeEnter = false;

    [Header("Scene Refs (instances, not prefabs)")]
    [Mandatory][SerializeField] private vThirdPersonController player;
    [Mandatory][SerializeField] private GameObject carRoot;
    [Mandatory][SerializeField] private Transform driverSeat;
    [Mandatory][SerializeField] private Transform exitPoint;
    [Mandatory][SerializeField] private MountTeleporter teleporter;
    [Mandatory][SerializeField] private CarCameraRig carCameras;
    [Mandatory][SerializeField] private RpgHoverController carController;

    [Header("Startup")]
    [Tooltip("If true, we boot seated in the car (player hidden/locked).")]
    [SerializeField] private bool startInsideCar = true;

    [Header("Input")]
    [SerializeField] private KeyCode enterKey = KeyCode.Y;
    [SerializeField] private KeyCode exitKey  = KeyCode.G;

    [Header("FP Mounts (optional quick swap)")]
    [Mandatory][SerializeField] private Transform fpUnderChassis;
    [Mandatory][SerializeField] private Transform fpWindshield;
    [Mandatory][SerializeField] private Transform fpNose;

    // ── State/Cache ──────────────────────────────────────────────────────────────
    bool inCar;
    bool isTransitioning;
    Rigidbody playerRb;
    CapsuleCollider playerCol;
    vThirdPersonInput playerInput;
    Invector.vCamera.vThirdPersonCamera invectorCam;

    // ── Public API for the Linker ───────────────────────────────────────────────
    public void SetLinkedPlayer(vThirdPersonController p)
    {
        player = p;
        CachePlayerBits();
    }

   

    // ── Unity Lifecycle ─────────────────────────────────────────────────────────
    void Reset()
    {
        if (!player)      player      = FindObjectOfType<vThirdPersonController>(true);
        if (!teleporter)  teleporter  = FindObjectOfType<MountTeleporter>(true);
        if (!carCameras)  carCameras  = FindObjectOfType<CarCameraRig>(true);
        if (!carController && carRoot) carController = carRoot.GetComponent<RpgHoverController>();
    }
     void CachePlayerBits()
    {
        if (!player) return;
        invectorCam  = player.GetComponentInChildren<Invector.vCamera.vThirdPersonCamera>(true);
        playerRb     = player.GetComponent<Rigidbody>();
        playerCol    = player.GetComponent<CapsuleCollider>();
        playerInput  = player.GetComponent<vThirdPersonInput>();
        if (player.tag != "Player") player.tag = "Player";
    }

    void BootOnFoot()
    {
        ToggleCarControl(false);
        if (!player.gameObject.activeSelf) player.gameObject.SetActive(true);
        TogglePlayerControl(true);
        SetRenderers(player.gameObject, true);

        if (carRoot && !carRoot.activeSelf) carRoot.SetActive(false);

        if (carCameras)
        {
            carCameras.InitializeForPlayer(player.transform);
            carCameras.SetMode(CarCameraRig.Mode.Player_Default);
        }
        if (invectorCam) invectorCam.gameObject.SetActive(false);

        inCar = false;
        Diag.Info("CAR", "Booted on foot.", this);
    }

    void ToggleCarControl(bool enable)
    {
        if (carController) carController.enabled = enable;
        if (extraCarDriveScripts != null)
            foreach (var b in extraCarDriveScripts) if (b) b.enabled = enable;
    }

    IEnumerator BootSeatedRoutine()
    {
        if (carRoot && !carRoot.activeSelf) carRoot.SetActive(true);
        if (!player.gameObject.activeSelf)  player.gameObject.SetActive(true);

        if (summonBeforeEnter && carSummoner) { carSummoner.SummonNow(); yield return null; }

        TogglePlayerControl(false);
        SetRenderers(player.gameObject, false);
        SeatPlayerInstant();

        ToggleCarControl(true);

        if (carCameras)
        {
            carCameras.InitializeForCar(carRoot, driverSeat);
            carCameras.SetCarTargets(carRoot.transform, driverSeat);
            if (fpWindshield) carCameras.SnapFirstPersonToMount(fpWindshield);
            carCameras.SetMode(CarCameraRig.Mode.Car_Default);
        }

        inCar = true;
        Diag.Info("CAR", "Booted seated in car.", this);
        OnPlayerEnter?.Invoke();
    }

    void Update()
    {
        if (isTransitioning) return;

        if (Input.GetKeyDown(enterKey) && !inCar) StartCoroutine(EnterCar());
        if (Input.GetKeyDown(exitKey)  &&  inCar) StartCoroutine(ExitCar());

        if (inCar && carCameras)
        {
            if (Input.GetKeyDown(KeyCode.Alpha3) && fpUnderChassis) carCameras.SnapFirstPersonToMount(fpUnderChassis);
            if (Input.GetKeyDown(KeyCode.Alpha4) && fpWindshield)   carCameras.SnapFirstPersonToMount(fpWindshield);
            if (Input.GetKeyDown(KeyCode.Alpha5) && fpNose)         carCameras.SnapFirstPersonToMount(fpNose);
        }
    }

    IEnumerator EnterCar()
    {
        using (var op = Diag.Begin("CAR", "EnterCar", this))
        {
          Debug.Log("player: " + player + " carRoot: " + carRoot + " teleporter: " + teleporter);

            if ( !player || !carRoot || !teleporter)
   
            { Diag.Error("CAR", "Missing refs for EnterCar", this); yield break; }

            isTransitioning = true;

          //  if (!driverSeat.gameObject.activeInHierarchy) driverSeat.gameObject.SetActive(true);
            if (!carRoot.activeSelf) { carRoot.SetActive(true); Diag.Info("CAR", "Car activated.", this); }

            if (summonBeforeEnter && carSummoner) { carSummoner.SummonNow(); yield return null; }

            TogglePlayerControl(false);
           

if (imm) imm.Add(DamageCategory.All, "CarTransition", 0.75f); // brief protection during mount


            if (playerRb)
            {
#if UNITY_6000_0_OR_NEWER
                playerRb.linearVelocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero;
#else
                playerRb.velocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero;
#endif
            }

            op.Step("Teleport to seat");
            yield return teleporter.MoveToMount(player.transform, driverSeat, true, 0.15f);

            ToggleCarControl(true);

            if (carCameras)
            {
                carCameras.InitializeForCar(carRoot, driverSeat);
                carCameras.SetCarTargets(carRoot.transform, driverSeat);
                if (fpWindshield) carCameras.SnapFirstPersonToMount(fpWindshield);
                carCameras.SetMode(CarCameraRig.Mode.Car_Default);
            }

            SetRenderers(player.gameObject, false);
            inCar = true;
            isTransitioning = false;

            OnPlayerEnter?.Invoke();
        }


    if (carController)
        carController.canDrive = true;   

    }

    IEnumerator ExitCar()
    {
        using (var op = Diag.Begin("CAR", "ExitCar", this))
        {
            if (!player || !teleporter)
            { Diag.Error("CAR", "Missing refs for ExitCar", this); yield break; }

            isTransitioning = true;

            SetRenderers(player.gameObject, true);
            ToggleCarControl(false);

            op.Step("Teleport off mount");
            yield return teleporter.MoveOffMount(player.transform, exitPoint, 0.15f);

            if (playerRb)
            {
#if UNITY_6000_0_OR_NEWER
                playerRb.linearVelocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero;
#else
                playerRb.velocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero;
#endif
            }
             if (imm) imm.Add(DamageCategory.All, "CarTransition", 0.25f); // protect while cameras/controls swap
         if (imm) imm.Remove("CarTransition"); // or rely on the timed window above
            TogglePlayerControl(true);

            if (carCameras)
            {
                carCameras.InitializeForPlayer(player.transform);
                carCameras.SetMode(CarCameraRig.Mode.Player_Default);
            }

            inCar = false;
            isTransitioning = false;

            OnPlayerExit?.Invoke();
        }



    if (carController)
        carController.canDrive = false;  // Car stops reacting to WASD
    }

    void TogglePlayerControl(bool enable)
    {
        if (!player) return;
        player.lockMovement = !enable;
        player.lockRotation = !enable;
        if (playerInput) playerInput.enabled = enable;
        if (playerCol)   playerCol.enabled = true;
        if (playerRb)    playerRb.isKinematic = !enable;

        if (invectorCam) invectorCam.gameObject.SetActive(false);
    }

    void SeatPlayerInstant()
    {
        if (!player || !driverSeat) return;
        var dst = driverSeat.position + Vector3.up * 0.15f;
        player.transform.SetPositionAndRotation(dst, driverSeat.rotation);
        player.transform.SetParent(driverSeat, true);
        if (playerRb)
        {
#if UNITY_6000_0_OR_NEWER
            playerRb.linearVelocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero;
#else
            playerRb.velocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero;
#endif
        }
    }

    static void SetRenderers(GameObject root, bool visible)
    {
        if (!root) return;
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) if (r) r.enabled = visible;
    }



public void SetCar(
    GameObject root,
    Transform seat = null,
    Transform exit = null,
    RpgHoverController controller = null,
    CarCameraRig cameras = null,
    MountTeleporter tp = null)
{
    if (root) carRoot = root;
    if (seat) driverSeat = seat;
    if (exit) exitPoint  = exit;
    if (controller) carController = controller;
    if (cameras)   carCameras    = cameras;
    if (tp)        teleporter    = tp;

    // extra safety: if exitPoint or carCameras are still null and PlayerCarLinker is present, pull from it
    var link = PlayerCarLinker.Instance;
    if (link != null)
    {
        if (!exitPoint) exitPoint = link.exitPoint;
        if (!carCameras) carCameras = link.carCameras;
        if (!carController && link.carController) carController = link.carController;
    }

    // If still missing exitPoint, create a local fallback under car root or compute near driverSeat
    if (!exitPoint && carRoot) CreateExitIfMissing();
    if (!carCameras && carRoot) carCameras = carRoot.GetComponentInChildren<CarCameraRig>(true) ?? FindObjectOfType<CarCameraRig>(true);
}

void Awake()
{
    using (var _ = Diag.Begin("CAR", "Awake", this))
    {
        // Pull from Linker if present
        var link = PlayerCarLinker.Instance;
        if (link)
        {
            if (!player)      player      = link.player;
            if (!carRoot)     carRoot     = link.carRoot;
            if (!driverSeat)  driverSeat  = link.driverSeat;
            if (!exitPoint)   exitPoint   = link.exitPoint;
            if (!carController) carController = link.carController ?? (carRoot ? carRoot.GetComponent<RpgHoverController>() : null);
            if (!carCameras)  carCameras  = link.carCameras ?? ResolveCarCamerasLocal();
            if (!teleporter)  teleporter  = link.teleporter ?? FindObjectOfType<MountTeleporter>(true);
        }

        // fallback local resolution
        if (!player) player = FindObjectOfType<vThirdPersonController>(true);
        if (!player) { Diag.Error("CAR", "No scene Player found.", this); enabled = false; return; }

        CachePlayerBits();

        // if exitPoint still missing, create a fallback (safe)
        if (!exitPoint && carRoot) CreateExitIfMissing();

        if (startInsideCar) StartCoroutine(BootSeatedRoutine());
        else BootOnFoot();
    }
}

// --- helper methods for CarTeleportEnterExit class ---

CarCameraRig ResolveCarCamerasLocal()
{
    if (carRoot != null)
    {
        var r = carRoot.GetComponentInChildren<CarCameraRig>(true);
        if (r != null) return r;
    }
    return FindObjectOfType<CarCameraRig>(true);
}

void CreateExitIfMissing()
{
    if (!carRoot) return;

    // prefer to base exit near driverSeat if available
    Vector3 pos = carRoot.transform.position;
    Quaternion rot = carRoot.transform.rotation;

    if (driverSeat != null)
    {
        pos = driverSeat.position + (driverSeat.right * 1.2f) + (Vector3.up * 0.05f);
        rot = driverSeat.rotation;
    }
    else
    {
        // fallback: place slightly to the right of carRoot
        pos = carRoot.transform.position + carRoot.transform.right * 1.2f + Vector3.up * 0.2f;
    }

    var go = new GameObject("PlayerExitPosition_Auto");
    go.transform.SetParent(carRoot.transform, false);
    go.transform.position = pos;
    go.transform.rotation = rot;
    exitPoint = go.transform;

    Diag.Info("CAR", $"Created fallback exitPoint '{go.name}' at {pos}", this);
}




}
