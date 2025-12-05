using UnityEngine;

public static class SceneCameraUtil
{
    public static void RestoreGameplayCamera(GameObject player)
    {
        var sceneCamGO = GameObject.Find("SceneCamera");
        var sceneCam    = sceneCamGO ? sceneCamGO.GetComponent<Camera>() : Camera.main;
        var pauseCamGO  = GameObject.Find("PauseMenu_UITK/Camera");
        if (pauseCamGO) pauseCamGO.SetActive(false); // or delete this camera

        if (sceneCam) sceneCam.enabled = true;

        // Cinemachine brain pulse
        var brain = sceneCam ? sceneCam.GetComponent<Unity.Cinemachine.CinemachineBrain>() : null;
        if (brain) { brain.enabled = false; brain.enabled = true; }

        // Invector camera retarget
        var invCam = Invector.vCamera.vThirdPersonCamera.instance;
        if (invCam && player) { invCam.SetMainTarget(player.transform); invCam.Init(); }

        // Bind camera to input so WASD uses the right view
        var respawn = player ? player.GetComponent<SimpleRespawn>() : null;
        if (respawn) respawn.SendMessage("EnsureCameraBindingForMovement", SendMessageOptions.DontRequireReceiver);
    }
}
