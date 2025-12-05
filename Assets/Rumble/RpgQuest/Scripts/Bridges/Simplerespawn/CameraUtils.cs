using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class CameraUtils
{
    public static void ForceCameraTargetTo(GameObject player)
    {
        if (player == null) { Debug.LogWarning("[CameraUtils] null player."); return; }

        // find any component whose type name contains "vThirdPersonCamera"
        var invectorCam = GameObject.FindObjectsOfType<Component>().FirstOrDefault(c => c != null && c.GetType().Name.Contains("vThirdPersonCamera"));
        if (invectorCam != null)
        {
            var type = invectorCam.GetType();
            var field = type.GetField("mainTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? type.GetField("target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(invectorCam, player.transform);
                Debug.Log("[CameraUtils] Set invector camera field to player: " + player.name);
                return;
            }
            var prop = type.GetProperty("mainTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? type.GetProperty("target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(invectorCam, player.transform, null);
                Debug.Log("[CameraUtils] Set invector camera property to player: " + player.name);
                return;
            }
        }

        // No invector camera found -> try to set Camera.main follower via name check (best-effort)
        if (Camera.main != null)
        {
            Debug.LogWarning("[CameraUtils] No invector camera found. Camera.main present: " + Camera.main.name + ". Consider adding a fallback follower.");
        }
        else Debug.LogWarning("[CameraUtils] No invector camera or Camera.main found to set target.");
    }
}
