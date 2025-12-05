using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Invector.vCharacterController;
using Invector;
using Invector.vCamera;
// run after almost everything
[DefaultExecutionOrder(32000)]
public class SceneCameraEnforcer : MonoBehaviour
{
    [Header("References")]
   [SerializeField] public Camera sceneCamera;                   // your gameplay camera object
    public vThirdPersonCamera invectorCamOnMain; // the Invector camera component sitting on the SceneCamera (or found)
    public bool enableCinemachineBrain = false;  // keep false for player control at start

    [Header("Durations")]
    public int framesToEnforce = 8;              // how many frames to hammer the state

    void Awake()
    {
        if (!sceneCamera)
        {
            // Prefer tagged MainCamera if you have it set
            sceneCamera = Camera.main;
            if (!sceneCamera)
            {
                var go = GameObject.Find("SceneCamera");
                if (go) sceneCamera = go.GetComponent<Camera>();
            }
        }
        if (!invectorCamOnMain && sceneCamera)
            invectorCamOnMain = sceneCamera.GetComponent<vThirdPersonCamera>();
    }

    void OnEnable()
    {
        PlayerCarSpawner.OnPlayerSpawned += HandlePlayerAppeared;
        SimpleRespawn.OnAnyRespawn        += HandlePlayerAppeared;
    }

    void OnDisable()
    {
        PlayerCarSpawner.OnPlayerSpawned -= HandlePlayerAppeared;
        SimpleRespawn.OnAnyRespawn        -= HandlePlayerAppeared;
    }

    void Start()
    {
        // First-time scene entry – try immediately; if no player yet, no-op
        var p = TryFindPlayer();
        if (p) StartCoroutine(EnforceForFrames(p));
    }

    void HandlePlayerAppeared(GameObject player)
    {
        if (player) StartCoroutine(EnforceForFrames(player));
    }

    GameObject TryFindPlayer()
    {
        var byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag) return byTag;

        var input = FindObjectOfType<vThirdPersonInput>(true);
        return input ? input.gameObject : null;
    }

    IEnumerator EnforceForFrames(GameObject player)
    {
        // run several frames to beat any late toggles
        // inside IEnumerator EnforceForFrames(GameObject player)
bool pulsed = false;

for (int i = 0; i < framesToEnforce; i++)
{
    if (sceneCamera)
    {
        sceneCamera.gameObject.SetActive(true);

        // Do the enable pulse only once to avoid flicker
        if (!pulsed)
        {
            sceneCamera.enabled = false;
            sceneCamera.enabled = true;
            pulsed = true;
        }

        if (sceneCamera.tag != "MainCamera")
            sceneCamera.tag = "MainCamera";

        var brain = sceneCamera.GetComponent<CinemachineBrain>();
        if (brain) brain.enabled = enableCinemachineBrain; // usually false for player
    }

    if (invectorCamOnMain && player)
    {
        invectorCamOnMain.enabled = true;
        invectorCamOnMain.SetMainTarget(player.transform);
        invectorCamOnMain.Init();
    }

    var input = player.GetComponent<vThirdPersonInput>();
    if (input && sceneCamera)
    {
        input.cameraMain = sceneCamera;
        input.SetLockAllInput(false);
        input.SetLockBasicInput(false);
        input.lockMoveInput = false;
    }

    var tpc = player.GetComponent<vThirdPersonController>();
    if (tpc)
    {
        tpc.isDead       = false;
        tpc.customAction = false;
        tpc.lockMovement = false;
        tpc.lockRotation = false;

        if (sceneCamera) tpc.UpdateMoveDirection(sceneCamera.transform);
        tpc.ResetInputAnimatorParameters();
    }

    yield return null; // next frame
}


//        Debug.Log("[SceneCameraEnforcer] SceneCamera asserted & input bound to camera.");
    }
}

