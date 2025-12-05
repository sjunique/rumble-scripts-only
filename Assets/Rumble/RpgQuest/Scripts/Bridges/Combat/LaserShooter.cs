using UnityEngine;

public class LaserShooter : MonoBehaviour
{
    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public float fireRate = 6f; // shots/sec

    [Header("Laser")]
    public float range = 80f;
    public float knockback = 12f;    // flee nudge
    public float fleeSeconds = 2.5f; // duration to flee
    public LayerMask hitMask = ~0;

    [Header("VFX")]
    public Transform muzzlePoint;       // where the VFX spawns (gun tip, hand, etc.)
    public GameObject muzzleVfx;
    public GameObject hitVfx;
    public float vfxLife = 1.2f;

    float _nextFireTime;

    Transform Cam()
    {
        var cmBrain = FindObjectOfType<Unity.Cinemachine.CinemachineBrain>();
        if (cmBrain && cmBrain.OutputCamera) return cmBrain.OutputCamera.transform;
        return Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        if (Time.time < _nextFireTime) return;
        if (!Input.GetKeyDown(fireKey)) return;

        _nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        FireOnce();
    }

    void FireOnce()
    {
        var cam = Cam();
        if (!cam) return;

        if (muzzleVfx && muzzlePoint)
            Destroy(Instantiate(muzzleVfx, muzzlePoint.position, muzzlePoint.rotation), vfxLife);

        if (Physics.Raycast(cam.position, cam.forward, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hitVfx)
                Destroy(Instantiate(hitVfx, hit.point, Quaternion.LookRotation(hit.normal)), vfxLife);

            // Knockback (if rigidbody)
            var rb = hit.rigidbody;
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                var away = (hit.point - cam.position).normalized;
                rb.AddForce(away * knockback, ForceMode.VelocityChange);
            }

            // Tell Emerald to Flee (via adapter, if present)
            var flee = hit.collider.GetComponentInParent<EmeraldFleeAdapter>();
            if (flee)
            {
                Vector3 awayDir = (hit.collider.transform.position - transform.position).normalized;
                flee.FleeFrom(awayDir, fleeSeconds, knockback);
            }
        }
    }
}

