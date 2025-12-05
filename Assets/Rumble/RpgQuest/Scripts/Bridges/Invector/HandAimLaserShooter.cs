using UnityEngine;

public class HandAimLaserShooter : MonoBehaviour
{
    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public float fireRate = 6f;

    [Header("Aiming")]
    public Transform handRotator;    // the transform you want to rotate (hand or forearm)
    public Vector3 handForwardAxis = Vector3.forward; // which local axis points out of the palm
    public float aimLerp = 20f;      // rotation speed

    [Header("Shot")]
    public Transform muzzle;         // tip point for VFX and ray origin
    public float range = 80f;
    public LayerMask hitMask = ~0;

    [Header("Effects")]
    public GameObject muzzleVfx;
    public GameObject hitVfx;
    public float vfxLife = 1.2f;

    [Header("AI Reaction")]
    public float knockback = 12f;
    public float fleeSeconds = 2.5f;
    public float initialKick = 0.8f;

    float _cooldown;

    Transform Cam()
    {
        var brain = FindObjectOfType<Unity.Cinemachine.CinemachineBrain>();
        if (brain && brain.OutputCamera) return brain.OutputCamera.transform;
        return Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        var cam = Cam();
        if (!cam) return;

        // 1) Aim the hand toward a point far along the camera forward
        Vector3 aimPoint = cam.position + cam.forward * 100f;
        AimHandTowards(aimPoint, Time.deltaTime);

        // 2) Fire
        if (Time.time >= _cooldown && Input.GetKeyDown(fireKey))
        {
            _cooldown = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            Fire(cam);
        }
    }

    void AimHandTowards(Vector3 worldPoint, float dt)
    {
        if (!handRotator) return;

        Vector3 toTarget = (worldPoint - handRotator.position);
        if (toTarget.sqrMagnitude < 0.0001f) return;

        // figure out which world direction corresponds to the hand's forward axis
        Vector3 currentFwd = handRotator.TransformDirection(handForwardAxis);
        Quaternion targetRot = Quaternion.FromToRotation(currentFwd, toTarget.normalized) * handRotator.rotation;
        handRotator.rotation = Quaternion.Slerp(handRotator.rotation, targetRot, 1f - Mathf.Exp(-aimLerp * Time.deltaTime));
    }

    void Fire(Transform cam)
    {
        if (muzzleVfx && muzzle)
            Destroy(Instantiate(muzzleVfx, muzzle.position, muzzle.rotation), vfxLife);

        Vector3 origin = muzzle ? muzzle.position : cam.position;
        Vector3 dir = muzzle ? (muzzle.forward) : cam.forward;

        if (Physics.Raycast(origin, dir, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hitVfx)
                Destroy(Instantiate(hitVfx, hit.point, Quaternion.LookRotation(hit.normal)), vfxLife);

            // Rigidbody nudge
            if (hit.rigidbody)
            {
#if UNITY_6000_0_OR_NEWER
                hit.rigidbody.linearVelocity = Vector3.zero;
#else
                hit.rigidbody.velocity = Vector3.zero;
#endif
                hit.rigidbody.AddForce(dir * knockback, ForceMode.VelocityChange);
            }

            // Emerald flee
            var flee = hit.collider.GetComponentInParent<EmeraldFleeAdapter>();
            if (flee)
            {
                Vector3 awayDir = (hit.collider.transform.position - transform.position).normalized;
                flee.FleeFrom(awayDir, fleeSeconds, initialKick);
            }

            // (Optional) Damage gateway if you want to reduce HP too:
            // var hp = hit.collider.GetComponentInParent<EmeraldHealthAdapter>();
            // if (hp) hp.ApplyDamage( laserDamage );
        }
    }
}
