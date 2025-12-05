using UnityEngine;
using System;
using System.Reflection;

public class SceneCameraResetter : MonoBehaviour, ICameraReset
{
    [Header("Optional target (auto-found if empty)")]
    public Transform player;

    [Header("Fallback offset when no active Cinemachine brain")]
    public Vector3 fallbackOffset = new Vector3(0f, 3f, -5.5f);

    Camera _cam;
    Component _brain;          // Unity.Cinemachine.CinemachineBrain or Cinemachine.CinemachineBrain
    MonoBehaviour _invTpsCam;  // Invector vThirdPersonCamera (if present)

    void Awake()
    {
        _cam = GetComponent<Camera>();

        // Try CM3 then CM2 without compile-time dependency
        _brain = GetComponent(FindType("Unity.Cinemachine.CinemachineBrain"))
                 ?? GetComponent(FindType("Cinemachine.CinemachineBrain"));

        // Try to fetch Invector camera without hard ref
        _invTpsCam = GetComponent("vThirdPersonCamera") as MonoBehaviour;

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    public void ResetCamera() => ResetCamera(player);

    public void ResetCamera(Transform target)
    {
        if (!target) { Debug.LogWarning("[SceneCam] No target to reset."); return; }

        if (TryCinemachineReset(target)) return;
        if (TryInvectorReset(target))    return;

        // Pure fallback: place physical camera behind & look at target
        var desired = target.TransformPoint(fallbackOffset);
        transform.position = desired;
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        Debug.Log("[SceneCam] Fallback placement reset.");
    }

    // ---------- Helpers ----------

    bool TryCinemachineReset(Transform target)
    {
        if (_brain == null || !_brain.gameObject.activeInHierarchy) return false;

        var brainType = _brain.GetType();

        // Brain.enabled?
        var enabledProp = brainType.GetProperty("enabled");
        if (enabledProp != null && enabledProp.PropertyType == typeof(bool))
        {
            bool enabled = (bool)enabledProp.GetValue(_brain);
            if (!enabled) return false;
        }

        // ActiveVirtualCamera (ICinemachineCamera)
        var activeProp = brainType.GetProperty("ActiveVirtualCamera", BindingFlags.Public | BindingFlags.Instance);
        if (activeProp == null) return false;

        var activeVCam = activeProp.GetValue(_brain);
        if (activeVCam == null) return false;

        // OutputCamera (Camera)
        var outCamProp = brainType.GetProperty("OutputCamera", BindingFlags.Public | BindingFlags.Instance);
        var outCam = outCamProp != null ? outCamProp.GetValue(_brain) as Camera : null;
        if (outCam == null) return false;

        // Compute player→camera delta and warp vcam
        var camTr = outCam.transform;
        var delta = target.position - camTr.position;

        // ICinemachineCamera.OnTargetObjectWarped(Transform, Vector3)
        var onWarp = activeVCam.GetType().GetMethod("OnTargetObjectWarped",
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(Transform), typeof(Vector3) }, null);
        if (onWarp != null)
        {
            onWarp.Invoke(activeVCam, new object[] { target, delta });
        }

        // Invalidate previous state so damping rebuilds immediately
        var vbaseType = FindType("Unity.Cinemachine.CinemachineVirtualCameraBase")
                        ?? FindType("Cinemachine.CinemachineVirtualCameraBase");
        if (vbaseType != null && vbaseType.IsInstanceOfType(activeVCam))
        {
            var prevValid = vbaseType.GetProperty("PreviousStateIsValid", BindingFlags.Public | BindingFlags.Instance);
            prevValid?.SetValue(activeVCam, false);
        }

        // Optional nicety: recentre FreeLook axes
        var freeType = FindType("Unity.Cinemachine.CinemachineFreeLook")
                       ?? FindType("Cinemachine.CinemachineFreeLook");
        if (freeType != null && freeType.IsInstanceOfType(activeVCam))
        {
            var xAxis = freeType.GetField("m_XAxis", BindingFlags.Public | BindingFlags.Instance);
            var yAxis = freeType.GetField("m_YAxis", BindingFlags.Public | BindingFlags.Instance);
            var xObj = xAxis?.GetValue(activeVCam);
            var yObj = yAxis?.GetValue(activeVCam);
            // Both v2 and v3 axes expose Value as float
            var valProp = xObj?.GetType().GetProperty("Value") ?? xObj?.GetType().GetField("Value") as MemberInfo;
            if (valProp is PropertyInfo vpX) vpX.SetValue(xObj, 0f);
            if (valProp is FieldInfo    vfX) vfX.SetValue(xObj, 0f);

            valProp = yObj?.GetType().GetProperty("Value") ?? yObj?.GetType().GetField("Value") as MemberInfo;
            if (valProp is PropertyInfo vpY) vpY.SetValue(yObj, 0.5f);
            if (valProp is FieldInfo    vfY) vfY.SetValue(yObj, 0.5f);
        }

        Debug.Log("[SceneCam] Cinemachine reset via warp/invalidate (v2/v3 safe).");
        return true;
    }

    bool TryInvectorReset(Transform target)
    {
        if (_invTpsCam == null) return false;

        var t = _invTpsCam.GetType();

        // vThirdPersonCamera.SetMainTarget(Transform) – if present
        var setMainTarget = t.GetMethod("SetMainTarget", BindingFlags.Public | BindingFlags.Instance);
        if (setMainTarget != null) setMainTarget.Invoke(_invTpsCam, new object[] { target });

        // Optional: ForceUpdatePosition() – if present
        var forceUpdate = t.GetMethod("ForceUpdatePosition", BindingFlags.Public | BindingFlags.Instance);
        if (forceUpdate != null) forceUpdate.Invoke(_invTpsCam, null);

        Debug.Log("[SceneCam] Reset using Invector vThirdPersonCamera.");
        return true;
    }

    static Type FindType(string fullName)
    {
        // Try fast path
        var t = Type.GetType(fullName);
        if (t != null) return t;

        // Scan loaded assemblies (handles editor + player)
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var a in asms)
        {
            try
            {
                t = a.GetType(fullName);
                if (t != null) return t;
            }
            catch { /* ignored */ }
        }
        return null;
    }
}
