using UnityEngine;
using Invector.vCharacterController;

[DefaultExecutionOrder(-200)]
public class SceneBootstrapperTest : MonoBehaviour
{
    [Header("Optional: assign, else auto-find")]
    public vThirdPersonController playerInstance;
    public GameObject carInstance;
    public Transform carDriverSeatTransform;
    public Transform carExitPointTransform;

    [Header("Optional names/tags to search under car")]
    public string[] seatNames = { "DriverSeat", "Seat_Driver", "Seat" };
    public string[] exitNames = { "ExitPoint", "Exit_Player", "Exit" };
    public string seatTag = "Seat";
    public string exitTag = "Exit";

    void Start()
    {
        // Player
        if (!playerInstance) playerInstance = FindObjectOfType<vThirdPersonController>(true);

        // Car root
        if (!carInstance)
        {
            var ctrl = FindObjectOfType<RpgHoverController>(true);
            if (ctrl) carInstance = ctrl.gameObject;
            if (!carInstance)
                carInstance = GameObject.FindWithTag("Vehicle") ?? GameObject.FindWithTag("Car");
        }

        // Seat/Exit auto-find if empty
        if (carInstance)
        {
            if (!carDriverSeatTransform)
                carDriverSeatTransform = FindChildByNamesOrTag(carInstance.transform, seatNames, seatTag);

            if (!carExitPointTransform)
                carExitPointTransform = FindChildByNamesOrTag(carInstance.transform, exitNames, exitTag);
        }

        // Initialize linker
        PlayerCarLinker.Instance.Initialize(
            playerInstance,
            carInstance,
            carDriverSeatTransform,
            carExitPointTransform
        );

        // Push into components so they refresh refs even if their Awake already ran
        var enterExit = FindObjectOfType<CarTeleportEnterExit>(true);
        var summon    = FindObjectOfType<WaterCarSummon>(true);
        PlayerCarLinker.Instance.PushInto(enterExit);
        PlayerCarLinker.Instance.PushInto(summon);

        // Optional: quick log hooks
        if (enterExit != null)
        {
            enterExit.OnPlayerEnter += () => Debug.Log("[Bind] Player entered car");
            enterExit.OnPlayerExit  += () => Debug.Log("[Bind] Player exited car");
        }
        if (summon != null)
        {
            summon.OnCarSummoned += (car,pos) => Debug.Log($"[Bind] Car summoned at {pos}");
        }
    }

    Transform FindChildByNamesOrTag(Transform root, string[] names, string tag)
    {
        // Try tag first
        if (!string.IsNullOrEmpty(tag))
        {
            var tagged = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in tagged)
                if (t.CompareTag(tag)) return t;
        }
        // Try names (case-insensitive contains)
        if (names != null)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var n in names)
            {
                foreach (var t in all)
                    if (t.name.ToLowerInvariant().Contains(n.ToLowerInvariant()))
                        return t;
            }
        }
        return null;
    }
}
