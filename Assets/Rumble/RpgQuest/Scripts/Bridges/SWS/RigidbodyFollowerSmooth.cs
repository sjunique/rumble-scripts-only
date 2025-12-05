// Assets/Rumble/RpgQuest/Bridges/SWS/RigidbodyFollowerSmooth.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyFollowerSmooth : MonoBehaviour
{
    public Transform target;
    public float posLerp = 12f;
    public float rotLerp = 10f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // MovePosition/MoveRotation driven
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (!target) return;

        float a = 1f - Mathf.Exp(-posLerp * Time.fixedDeltaTime);
        float b = 1f - Mathf.Exp(-rotLerp * Time.fixedDeltaTime);

        Vector3 nextPos = Vector3.Lerp(rb.position, target.position, a);
        Quaternion nextRot = Quaternion.Slerp(rb.rotation, target.rotation, b);

        rb.MovePosition(nextPos);
        rb.MoveRotation(nextRot);
    }
}
