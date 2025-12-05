using UnityEngine;
using Invector.vCharacterController;

public class PlayerMoveProbe : MonoBehaviour
{
    public vThirdPersonController cc;
    public vThirdPersonInput vinput;
    public Rigidbody rb;
    public CapsuleCollider col;
    public Animator anim;

    public KeyCode dumpKey = KeyCode.F1; // print state
    public KeyCode forceUnlockKey  = KeyCode.F1;  // unlock input/physics
    public KeyCode forceBaselineKey= KeyCode.F1;  // baseline locomotion config
    public KeyCode translateTestKey= KeyCode.F1;  // move transform directly (bypass controller)

    void Reset()
    {
        if (!cc)     cc     = GetComponent<vThirdPersonController>();
        if (!vinput) vinput = GetComponent<vThirdPersonInput>();
        if (!rb)     rb     = GetComponent<Rigidbody>();
        if (!col)    col    = GetComponent<CapsuleCollider>();
        if (!anim)   anim   = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(forceUnlockKey)) ForceUnlock();
        if (Input.GetKeyDown(forceBaselineKey)) ForceBaseline();
        if (Input.GetKeyDown(translateTestKey)) TranslateTest();
        if (Input.GetKeyDown(dumpKey)) DumpOnce();
    }

    void ForceUnlock()
    {
        if (cc)
        {
            cc.enabled = true;
            cc.lockMovement = false;
            cc.lockRotation = false;
        }
        if (vinput) vinput.enabled = true;
        if (rb)     rb.isKinematic = false;
        if (col)    col.enabled = true;
        if (anim)
        {
            anim.enabled = true;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.applyRootMotion = false; // temporarily disable root motion
        }
        Debug.Log("[Probe] Forced unlock: cc.enabled=true, vInput.enabled=true, rb.isKinematic=false, anim.applyRootMotion=false");
    }

    void ForceBaseline()
    {
        if (!cc) return;
        // disable root motion & ensure speeds sane
        cc.useRootMotion = false;
        cc.freeSpeed.walkSpeed    = Mathf.Max(cc.freeSpeed.walkSpeed,    1.8f);
        cc.freeSpeed.runningSpeed = Mathf.Max(cc.freeSpeed.runningSpeed, 3.0f);
        cc.freeSpeed.sprintSpeed  = Mathf.Max(cc.freeSpeed.sprintSpeed,  5.0f);

        // free RB movement on XZ, keep rotation stabilized
        if (rb)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (col) col.enabled = true;

        Debug.Log("[Probe] Forced baseline locomotion: useRootMotion=false, speeds>=defaults, RB constraints set.");
    }

    void TranslateTest()
    {
        // Try to move 0.2m forward ignoring controller/animator
        var before = transform.position;
        transform.position += transform.forward * 0.2f;
        Debug.Log($"[Probe] TranslateTest moved from {before} to {transform.position}");
    }

    void DumpOnce()
    {
        // Read user axes via wrapper -> legacy -> keys
        float ux = (vinput && vinput.horizontalInput != null) ? vinput.horizontalInput.GetAxisRaw() : 0f;
        float uz = (vinput && vinput.verticalInput   != null) ? vinput.verticalInput.GetAxisRaw()   : 0f;
        if (Mathf.Approximately(ux,0)) ux = Input.GetAxisRaw("Horizontal");
        if (Mathf.Approximately(uz,0)) uz = Input.GetAxisRaw("Vertical");
        if (Mathf.Approximately(ux,0))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  ux -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ux += 1f;
        }
        if (Mathf.Approximately(uz,0))
        {
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  uz -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    uz += 1f;
        }

        string A = vinput ? $"vInput.enabled={vinput.enabled}" : "vInput=null";
        string B = cc     ? $"cc.enabled={cc.enabled} lockMove={cc.lockMovement} input=({cc.input.x:F2},{cc.input.z:F2}) useRootMotion={cc.useRootMotion}" : "cc=null";
#if UNITY_6000_0_OR_NEWER
        string C = rb ? $"rb.kin={rb.isKinematic} rb.vel={rb.linearVelocity} constr={rb.constraints}" : "rb=null";
#else
        string C = rb ? $"rb.kin={rb.isKinematic} rb.vel={rb.velocity}     constr={rb.constraints}" : "rb=null";
#endif
        string D = anim ? $"anim.enabled={anim.enabled} applyRootMotion={anim.applyRootMotion} ctrl={(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name :"<none>")}" : "anim=null";
        string E = col ? $"col.enabled={col.enabled}" : "col=null";

        Debug.Log($"[Probe] userAxes=({ux:F2},{uz:F2})  {A}  {B}  {C}  {D}  {E}");
    }
}
