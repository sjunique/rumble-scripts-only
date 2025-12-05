using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SimpleRightArmIKAim : MonoBehaviour
{
    [Header("Input")]
    public KeyCode aimKey = KeyCode.Mouse1;   // hold to aim

    [Header("Aim")]
    public float aimDistance = 30f;           // how far ahead to aim
    public LayerMask aimMask = ~0;            // raycast against level for nicer aim
    public float weightLerp = 12f;            // how fast arm blends in/out

    [Header("Hand Orientation")]
    public Vector3 palmForwardLocal = Vector3.forward; // axis that should face the target
    public float twistBlend = 0.6f;           // 0..1: how much to rotate hand to face target

    private Animator _anim;
    private float _w;

    void Awake(){ _anim = GetComponent<Animator>(); }

    Transform GetCam()
    {
        var brain = FindObjectOfType<Unity.Cinemachine.CinemachineBrain>();
        if (brain && brain.OutputCamera) return brain.OutputCamera.transform;
        return Camera.main ? Camera.main.transform : null;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (_anim == null) return;
        var cam = GetCam(); if (!cam) return;

        bool aiming = Input.GetKey(aimKey);
        float targetW = aiming ? 1f : 0f;
        _w = Mathf.MoveTowards(_w, targetW, weightLerp * Time.deltaTime);

        // Find an aim point from the camera center
        Vector3 dst = cam.position + cam.forward * aimDistance;
        if (Physics.Raycast(cam.position, cam.forward, out var hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
            dst = hit.point;

        // Position the right hand toward the point
        _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _w);
        _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, _w * twistBlend);
        _anim.SetIKPosition(AvatarIKGoal.RightHand, dst);

        // Optional: make the palm face the target a bit (helps laser muzzle alignment)
        Vector3 handPos = _anim.GetIKPosition(AvatarIKGoal.RightHand);
        Vector3 dir = (dst - handPos); if (dir.sqrMagnitude > 1e-4f) dir.Normalize();
        Quaternion face = Quaternion.LookRotation(dir, Vector3.up);
        _anim.SetIKRotation(AvatarIKGoal.RightHand, face);
    }
}
