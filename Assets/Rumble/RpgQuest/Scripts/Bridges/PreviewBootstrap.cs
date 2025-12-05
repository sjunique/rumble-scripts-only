// PreviewBootstrap.cs
using UnityEngine;
using System;
using System.Linq;

[DefaultExecutionOrder(-999999)] // run before most scripts
public class PreviewBootstrap : MonoBehaviour
{
    // exact Invector class names + anything custom you use
    static readonly string[] TypeNamesToDisable = {
        // Invector character & inputs
        "vThirdPersonInput","vThirdPersonController","vThirdPersonMotor",
        "vMeleeCombatInput","vShooterMeleeInput","vShooterManager","vMeleeManager",
        // Invector inventory/health/camera
        "vItemManager","vInventory","vAmmoManager","vLockOnTargetControl",
        "vHealthController","vThirdPersonCamera",
        // Unity AI
        "NavMeshAgent",
        // your vehicle/hover/input scripts (add yours here)
        "ActualHoverController","NewCarHandler","NewInputHandler"
    };

    Behaviour[] _behaviours;
    Rigidbody[] _rigidbodies;
    Collider[]  _colliders;
    Animator    _anim;

    void Awake()
    {
        if (!PreviewMode.IsActive) return;

        Cache();
        ApplyDisable();      // disable BEFORE anything else runs
    }

    void OnEnable()
    {
        if (!PreviewMode.IsActive) return;
        // if something re-enables later, we’ll clamp back off in LateUpdate
    }

    void LateUpdate()
    {
        if (!PreviewMode.IsActive) return;
        // keep them disabled in case any script toggled them back on this frame
        HardClampDisabled();
    }

    void Cache()
    {
        _behaviours = GetComponentsInChildren<Behaviour>(true);
        _rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _anim = GetComponentInChildren<Animator>(true);
    }

    void ApplyDisable()
    {
        foreach (var b in _behaviours)
        {
            if (!b) continue;
            var n = b.GetType().Name;
            if (TypeNamesToDisable.Contains(n))
                b.enabled = false;
        }

        foreach (var rb in _rigidbodies)
        {
            if (!rb) continue;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var c in _colliders)
            if (c) c.enabled = false;

        if (_anim)
        {
            _anim.applyRootMotion = false;
            _anim.enabled = true;   // keep idle pose
        }

        foreach (var a in GetComponentsInChildren<AudioSource>(true))
            if (a) a.mute = true;
    }

    void HardClampDisabled()
    {
        foreach (var b in _behaviours)
        {
            if (!b) continue;
            var n = b.GetType().Name;
            if (TypeNamesToDisable.Contains(n) && b.enabled)
                b.enabled = false; // re-disable if anything toggled it
        }
    }
}
