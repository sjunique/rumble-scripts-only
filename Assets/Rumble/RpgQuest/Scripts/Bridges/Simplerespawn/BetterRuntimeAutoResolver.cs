using UnityEngine;

// BetterRuntimeAutoResolver.cs
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class BetterRuntimeAutoResolver : MonoBehaviour
{
    public float retryDuration = 2f; // seconds
    public bool runOnStart = true;

    void Start()
    {
        if (runOnStart) StartCoroutine(RunResolve());
    }

    public IEnumerator RunResolve()
    {
        float start = Time.realtimeSinceStartup;
        GameObject candidate = null;
        while (Time.realtimeSinceStartup - start < retryDuration)
        {
            candidate = FindValidPlayerCandidate();
            if (candidate != null) break;
            yield return new WaitForSeconds(0.1f);
        }

        if (candidate == null)
        {
            Debug.LogWarning("[BetterRuntimeAutoResolver] No valid player found within timeout.");
            yield break;
        }

        // If you want, set a public target field here or force the camera
        Debug.Log("[BetterRuntimeAutoResolver] Resolved player -> " + candidate.name);
        // example: force camera
        ForceInvectorCameraTarget(candidate.transform);
    }

    private GameObject FindValidPlayerCandidate()
    {
        // use PlayerCandidateFinder (you should have this in the project)
        var cand = PlayerCandidateFinder.FindBestPlayerCandidate();
        if (cand == null) return null;
        // double-check cand has meaningful components
        bool ok = cand.GetComponentInChildren<Invector.vCharacterController.vThirdPersonController>(true) != null
               || cand.GetComponentInChildren<Invector.vHealthController>(true) != null;
        if (!ok) return null;
        return cand;
    }

    private void ForceInvectorCameraTarget(Transform target)
    {
        if (target == null) return;
        var invectorCamComp = GameObject.FindObjectsOfType<Component>()
                                        .FirstOrDefault(c => c != null && c.GetType().Name.Contains("vThirdPersonCamera"));
        if (invectorCamComp == null) return;
        var type = invectorCamComp.GetType();
        var field = type.GetField("mainTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 ?? type.GetField("target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) { field.SetValue(invectorCamComp, target); return; }
        var prop = type.GetProperty("mainTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 ?? type.GetProperty("target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite) { prop.SetValue(invectorCamComp, target, null); return; }
    }
}
