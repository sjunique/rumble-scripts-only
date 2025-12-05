using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StableRightArmIKAim : MonoBehaviour
{
    [Header("Input")]
    public KeyCode aimKey = KeyCode.Mouse1;

    [Header("Aim Targeting")]
    public float aimDistance = 18f;            // shorter distance = less over-reach
    public LayerMask aimMask = ~0;
    public float targetSmoothTime = 0.06f;
    public float weightLerp = 12f;

    [Header("Arm Bones (Humanoid)")]
    public Transform shoulder;                 // RightShoulder (optional but preferred)
    public Transform upperArm;                 // RightUpperArm
    public Transform forearm;                  // RightLowerArm (you said: RightForearm)
    public Transform hand;                     // RightHand

    [Header("Elbow Hint")]
    public Transform elbowHintPoint;           // optional; else we compute a virtual hint
    public float hintWeight = 1.0f;

    [Header("Hand Orientation")]
    public float handTwistWeight = 0.55f;      // rotate palm toward target a bit

    [Header("Upper Body Assist (LookAt)")]
    public bool useUpperBodyLookAt = true;
    public float lookAtBodyWeight = 0.35f;     // torso twist
    public float lookAtHeadWeight = 0.15f;     // slight head aim
    public float lookAtEyesWeight = 0.0f;

    [Header("Conflict Guard")]
    public MonoBehaviour handRotatorScript;    // e.g., HandAimLaserShooter

    Animator _anim;
    float _ikW;
    Vector3 _smoothedTarget, _vel;

    void Awake() { _anim = GetComponent<Animator>(); }

    Transform Cam()
    {
        var brain = FindObjectOfType<Unity.Cinemachine.CinemachineBrain>();
        if (brain && brain.OutputCamera) return brain.OutputCamera.transform;
        return Camera.main ? Camera.main.transform : null;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!_anim) return;
        var cam = Cam(); if (!cam) return;

        bool aiming = Input.GetKey(aimKey);
        _ikW = Mathf.MoveTowards(_ikW, aiming ? 1f : 0f, weightLerp * Time.deltaTime);

        // Disable any other hand rotator while IK weight is active
        if (handRotatorScript) handRotatorScript.enabled = (_ikW < 0.001f);

        // 1) Desired aim point from camera
        Vector3 desired = cam.position + cam.forward * aimDistance;
        if (Physics.Raycast(cam.position, cam.forward, out var hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
            desired = hit.point;

        // 2) Smooth target to reduce jitter
        _smoothedTarget = Vector3.SmoothDamp(_smoothedTarget == default ? desired : _smoothedTarget, desired, ref _vel, targetSmoothTime);

        // 3) Reach clamp (prevents arm from sinking into chest when over-extended)
        Vector3 ikPos = _smoothedTarget;
        if (upperArm && forearm && hand)
        {
            float bone1 = Vector3.Distance(upperArm.position, forearm.position);
            float bone2 = Vector3.Distance(forearm.position, hand.position);
            float maxReach = Mathf.Max(0.05f, bone1 + bone2 - 0.03f); // tiny padding
            Vector3 shPos = shoulder ? shoulder.position : upperArm.position;
            Vector3 v = _smoothedTarget - shPos;
            float d = v.magnitude;
            if (d > maxReach) ikPos = shPos + v.normalized * maxReach;
        }

        // 4) Optional: gently rotate torso/head toward aim to help reach
        if (useUpperBodyLookAt)
        {
            _anim.SetLookAtWeight(_ikW, lookAtBodyWeight, lookAtHeadWeight, lookAtEyesWeight, 0.5f);
            _anim.SetLookAtPosition(ikPos);
        }

        // 5) Apply hand IK
        _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _ikW);
        _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, _ikW * handTwistWeight);
        _anim.SetIKPosition(AvatarIKGoal.RightHand, ikPos);

        Vector3 handPos = _anim.GetIKPosition(AvatarIKGoal.RightHand);
        Vector3 dir = (ikPos - handPos);
        if (dir.sqrMagnitude > 1e-4f)
        {
            dir.Normalize();
            _anim.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(dir, Vector3.up));
        }

        // 6) Elbow hint (real or virtual) to stop flipping
        _anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, _ikW * hintWeight);
        if (elbowHintPoint)
        {
            _anim.SetIKHintPosition(AvatarIKHint.RightElbow, elbowHintPoint.position);
        }
        else
        {
            // Virtual hint: place it slightly to the character's right & back from upper arm
            Vector3 right = transform.right;   // adjust if your character faces -Z
            Vector3 back  = -transform.forward;
            Vector3 mid = upperArm ? upperArm.position : (shoulder ? shoulder.position : transform.position);
            Vector3 hint = mid + right * 0.18f + back * 0.12f + Vector3.up * 0.05f;
            _anim.SetIKHintPosition(AvatarIKHint.RightElbow, hint);
        }
    }
}
