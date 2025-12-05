using UnityEngine;
using Unity.Cinemachine;
using Invector.vCharacterController;
 
 
using Invector.vCharacterController;

[DefaultExecutionOrder(-150)]
public class CameraRigBinder : MonoBehaviour
{
    public CarCameraRig rig; // assign in Inspector or auto-find

    vThirdPersonController player;
    GameObject car;

    void Awake()
    {
        if (!rig) rig = FindObjectOfType<CarCameraRig>(true);

        var link = PlayerCarLinker.Instance;
        if (link)
        {
            player = link.player;
            car    = link.carRoot;
        }

        // Ensure the rig knows which player cam to use
        var playerCam = FindPlayerFreeLookCam(player ? player.transform : null);
        if (rig && playerCam)
            rig.SetPlayerFreeLookCam(playerCam);

        // Initialize to player mode at start (player active, car inactive)
        if (rig && player)
        {
            rig.InitializeForPlayer(player.transform);
            rig.SetMode(CarCameraRig.Mode.Player_Default);
        }

        // Hook enter/exit to flip modes
        var enterExit = FindObjectOfType<CarTeleportEnterExit>(true);
        if (enterExit)
        {
            enterExit.OnPlayerEnter += HandlePlayerEnterCar;
            enterExit.OnPlayerExit  += HandlePlayerExitCar;
        }
    }

    void HandlePlayerEnterCar()
    {
        var link = PlayerCarLinker.Instance;
        if (!link || !rig) return;

        // If a new car got spawned, refresh the refs and car targets
        if (!car) car = link.carRoot;

        rig.InitializeForPlayer(link.player.transform); // keep player cam remembered
        rig.SetCarTargets(car.transform, link.driverSeat);
        rig.SetMode(CarCameraRig.Mode.Car_Default);
    }

    void HandlePlayerExitCar()
    {
        var link = PlayerCarLinker.Instance;
        if (!link || !rig) return;

        // Reconfirm player cam (in case it was created late)
        var playerCam = FindPlayerFreeLookCam(link.player ? link.player.transform : null);
        if (playerCam) rig.SetPlayerFreeLookCam(playerCam);

        rig.InitializeForPlayer(link.player.transform);
        rig.SetMode(CarCameraRig.Mode.Player_Default);
    }

    CinemachineCamera FindPlayerFreeLookCam(Transform playerRoot)
    {
        if (!playerRoot) return null;

        // 1) Prefer a CinemachineCamera under the player hierarchy
        var cams = playerRoot.GetComponentsInChildren<CinemachineCamera>(true);
        foreach (var c in cams)
        {
            string n = c.gameObject.name.ToLowerInvariant();
            if (n.Contains("player") || n.Contains("freelook") || n.Contains("third") || n.Contains("tps"))
                return c;
            if (c.Follow == playerRoot || c.LookAt == playerRoot)
                return c;
        }

        // 2) Fallback: search the scene for a cam that targets the player
        var all = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c.Follow == playerRoot || c.LookAt == playerRoot)
                return c;
        }

        return null;
    }
}


/*
[DefaultExecutionOrder(-150)]
public class CameraRigBinder : MonoBehaviour
{
    public CarCameraRig rig; // assign if you want; auto-finds otherwise

    CinemachineFreeLook playerFL;
    CinemachineFreeLook carFL;
    vThirdPersonController player;
    GameObject car;

    void Awake()
    {
        // Pull from linker
        if (!rig) rig = FindObjectOfType<CarCameraRig>(true);
        var link = PlayerCarLinker.Instance;
        if (link)
        {
            player = link.player;
            car    = link.carRoot;
        }

        // Auto-find FreeLooks under player & car
        playerFL = player ? player.GetComponentInChildren<CinemachineFreeLook>(true) : null;
        carFL    = car    ? car.GetComponentInChildren<CinemachineFreeLook>(true)    : null;

        // Initialize rig targets immediately so player cam is valid at start
        if (rig && player)
        {
            rig.InitializeForPlayer(player.transform);
            rig.SetMode(CarCameraRig.Mode.Player_Default);
        }

        // Subscribe to car enter/exit
        var enterExit = FindObjectOfType<CarTeleportEnterExit>(true);
        if (enterExit)
        {
            enterExit.OnPlayerEnter += HandlePlayerEnterCar;
            enterExit.OnPlayerExit  += HandlePlayerExitCar;
        }
    }

    void HandlePlayerEnterCar()
    {
        var link = PlayerCarLinker.Instance;
        if (link && rig)
        {
            // Refresh refs in case car got spawned/summoned just now
            if (!car) car = link.carRoot;
            if (car && !carFL) carFL = car.GetComponentInChildren<CinemachineFreeLook>(true);

            rig.InitializeForCar(car, link.driverSeat);
            rig.SetCarTargets(car.transform, link.driverSeat);
            // If your rig needs to know the actual CM cams, set them here:
            // rig.AssignCarFreeLook(carFL); // (optional if your rig exposes it)
            rig.SetMode(CarCameraRig.Mode.Car_Default);
        }
    }

    void HandlePlayerExitCar()
    {
        var link = PlayerCarLinker.Instance;
        if (link && rig)
        {
            if (!player) player = link.player;
            if (player && !playerFL) playerFL = player.GetComponentInChildren<CinemachineFreeLook>(true);

            rig.InitializeForPlayer(player.transform);
            // rig.AssignPlayerFreeLook(playerFL); // (optional)
            rig.SetMode(CarCameraRig.Mode.Player_Default);
        }
    }
}

*/
