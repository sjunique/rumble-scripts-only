using UnityEngine;

public class BeltLaserShooter : MonoBehaviour
{
    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public float fireRate = 6f;
    public KeyCode aimKey = KeyCode.Mouse1;  // optional hold-to-aim

    [Header("Refs")]
    public Transform aimPivot;               // rotates to face aim
    public Transform muzzle;                 // forward = shot dir
    public ParticleSystem ringLaser;         // your ring particle
    public GameObject muzzleVfx;             // optional
    public float vfxLife = 1.0f;

    [Header("Shooting")]
    public float range = 80f;
    public LayerMask hitMask = ~0;

    [Header("AI reaction")]
    public float knockback = 12f;
    public float fleeSeconds = 2.5f;
    public float initialKick = 0.8f;

    [Header("Optional: subtle spine lean")]
    public Transform spineForLean;           // e.g., SpineUpper bone
    public float leanMaxDegrees = 6f;
    public float leanLerp = 10f;

    float _nextFire, _lean;

    Transform Cam()
    {
        var brain = FindObjectOfType<Unity.Cinemachine.CinemachineBrain>();
        if (brain && brain.OutputCamera) return brain.OutputCamera.transform;
        return Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        var cam = Cam(); if (!cam) return;

        // 1) Aim pivot points where the camera looks
        Vector3 aimPoint = cam.position + cam.forward * 50f;
        if (aimPivot)
        {
            Vector3 dir = (aimPoint - aimPivot.position);
            if (dir.sqrMagnitude > 1e-4f)
                aimPivot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // 2) Optional: subtle spine lean while holding aim
        bool aiming = Input.GetKey(aimKey);
        float targetLean = aiming ? leanMaxDegrees : 0f;
        _lean = Mathf.MoveTowards(_lean, targetLean, leanLerp * Time.deltaTime);
        if (spineForLean) spineForLean.localRotation = Quaternion.Euler(_lean, 0f, 0f) * spineForLean.localRotation;

        // 3) Fire
        if (Time.time >= _nextFire && Input.GetKeyDown(fireKey))
        {
            _nextFire = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            FireOnce();
        }
    }

    void FireOnce()
    {
        if (!muzzle) return;

        if (muzzleVfx)
            Destroy(Instantiate(muzzleVfx, muzzle.position, muzzle.rotation), vfxLife);

        if (ringLaser)
        {
            ringLaser.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ringLaser.Play(true);
        }

        if (Physics.Raycast(muzzle.position, muzzle.forward, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Knockback if rigidbody
            var rb = hit.rigidbody;
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.AddForce(muzzle.forward * knockback, ForceMode.VelocityChange);
            }

            // Make Emerald AI flee if adapter present
            var flee = hit.collider.GetComponentInParent<EmeraldFleeAdapter>();
            if (flee)
            {
                Vector3 away = (hit.collider.transform.position - muzzle.position).normalized;
                flee.FleeFrom(away, fleeSeconds, initialKick);
            }

            // TODO (later): apply HP damage via Emerald health API
        }
    }
}
