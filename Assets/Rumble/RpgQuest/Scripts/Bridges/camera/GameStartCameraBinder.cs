 

using System.Collections;
using UnityEngine;
using Invector.vCharacterController;

public class GameStartCameraBinder : MonoBehaviour
{
    [Tooltip("How long to wait for a player to appear before giving up (seconds).")]
    public float waitTimeout = 8f;

    bool _bound;

    void OnEnable()
    {
        // If your spawner exposes this, we'll get an immediate callback.
        PlayerCarSpawner.OnPlayerSpawned += HandleSpawned;

        // Also (re)bind on respawn just to be safe.
        SimpleRespawn.OnAnyRespawn += HandleSpawned;
    }

    void OnDisable()
    {
        PlayerCarSpawner.OnPlayerSpawned -= HandleSpawned;
        SimpleRespawn.OnAnyRespawn -= HandleSpawned;
    }

    IEnumerator Start()
    {
        // Try to find a player immediately; if not, poll for a short time.
        var deadline = Time.unscaledTime + waitTimeout;

        while (!_bound && Time.unscaledTime < deadline)
        {
            var player = FindPlayerRobust();
            if (player)
            {
                BindCamera(player);
                yield break;
            }
            yield return null; // next frame
        }

        if (!_bound)
            Debug.LogError("[GameStartCameraBinder] No Player found; cannot bind camera.");
    }

    void HandleSpawned(GameObject player)
    {
        if (_bound || !player) return;
        BindCamera(player);
        InputContextFixer.EnsureGameplayMap(player);
        var tpc = GetComponent<Invector.vCharacterController.vThirdPersonController>();
if (tpc)
{
    tpc.lockMovement = false;
    tpc.lockRotation = false;
    tpc.customAction = false;
    tpc.isDead = false;
    // Refresh move direction off the (now valid) camera
    var cam = Camera.main ? Camera.main.transform : null;
    if (cam) tpc.UpdateMoveDirection(cam);
}

    }

    GameObject FindPlayerRobust()
    {
        // 1) Preferred: by tag
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) return tagged;

        // 2) Fallback: a vThirdPersonInput in the scene
        var input = FindObjectOfType<vThirdPersonInput>(true);
        if (input) return input.gameObject;

        // 3) Fallback: any vHealthController (your player has one)
        var health = FindObjectOfType<Invector.vHealthController>(true);
        if (health) return health.gameObject;

        return null;
    }

    void BindCamera(GameObject player)
    {
        var sceneCam = Camera.main;

        // Give ownership to Player (no brain pulse, no priority wars)
        var rig = FindObjectOfType<CarCameraRig>(true);
        if (rig)
            rig.ForcePlayerOwnership(player.transform, sceneCam);

        // Ensure WASD uses correct view
        var input = player.GetComponent<vThirdPersonInput>();
        if (input && sceneCam)
            input.cameraMain = sceneCam;

        _bound = true;
        Debug.Log("[GameStartCameraBinder] Scene camera bound to Player.");
    }
}
