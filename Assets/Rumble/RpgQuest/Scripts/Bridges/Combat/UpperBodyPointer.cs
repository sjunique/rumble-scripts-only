using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UpperBodyPointer : MonoBehaviour
{
    [Header("Rig References")]
    public Transform rightHand;       // mixamorig:RightHand
    public Transform muzzle;          // child on the hand; its Z+ should be forward
    public Transform aimTarget;       // IK_AimTarget (empty)
    public Transform elbowHint;       // IK_ElbowHint (empty)

    [Header("Aiming")]
    public KeyCode aimKey = KeyCode.Mouse1;
    public bool holdToAim = true;
    public float maxDistance = 100f;
    public LayerMask aimMask = ~0;
    public float ikLerpSpeed = 8f;    // blend speed for smooth enter/exit

    [Header("Upper Body Animator Layer")]
    public int upperBodyLayerIndex = 1;  // set to your “UpperBodyAim” layer index

    private Animator anim;
    private Camera cam;
    private float ikWeight;            // 0..1
    private LineRenderer lr;

    void Awake()
    {
        anim = GetComponent<Animator>();
        cam = Camera.main;
        lr  = muzzle ? muzzle.GetComponent<LineRenderer>() : null;
    }

    void Update()
    {
        // Decide whether we are aiming
        bool aiming = holdToAim ? Input.GetKey(aimKey)
                                : Input.GetKeyDown(aimKey) ? !(ikWeight > 0.5f) : (ikWeight > 0.5f);

        float target = aiming ? 1f : 0f;
        ikWeight = Mathf.MoveTowards(ikWeight, target, ikLerpSpeed * Time.deltaTime);

        // Drive the upper-body layer weight (if you added one)
        if (upperBodyLayerIndex >= 0 && upperBodyLayerIndex < anim.layerCount)
            anim.SetLayerWeight(upperBodyLayerIndex, ikWeight);

        // Ray from camera center to world to place the aim target
        if (cam && aimTarget)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 end = cam.transform.position + cam.transform.forward * maxDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, aimMask, QueryTriggerInteraction.Ignore))
                end = hit.point;

            aimTarget.position = end;

            // Laser line (optional)
            if (lr && muzzle)
            {
                lr.enabled = ikWeight > 0.05f;
                if (lr.enabled)
                {
                    lr.positionCount = 2;
                    lr.SetPosition(0, muzzle.position);
                    lr.SetPosition(1, end);
                }
            }
        }

        // Keep a reasonable elbow hint to the right of the upper arm
        if (elbowHint && rightHand)
        {
            Transform shoulder = rightHand.parent?.parent; // RightHand -> RightForeArm -> RightUpperArm
            if (shoulder)
                elbowHint.position = shoulder.position + transform.right * 0.25f + transform.up * -0.1f;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!anim || ikWeight <= 0.001f || rightHand == null || aimTarget == null) return;

        // Position the hand at current hand position (we mainly care about rotation)
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight * 0.25f); // tiny pos influence to keep stability
        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);

        // Rotate hand to look at the aim target using the hand's local forward (Z+) as the "muzzle"
        Vector3 toTarget = (aimTarget.position - rightHand.position).normalized;
        Quaternion look = Quaternion.LookRotation(toTarget, transform.up);
        anim.SetIKPosition(AvatarIKGoal.RightHand, rightHand.position);
        anim.SetIKRotation(AvatarIKGoal.RightHand, look);

        // Elbow hint for nicer bend
        if (elbowHint)
        {
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, ikWeight);
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, elbowHint.position);
        }
    }
}
