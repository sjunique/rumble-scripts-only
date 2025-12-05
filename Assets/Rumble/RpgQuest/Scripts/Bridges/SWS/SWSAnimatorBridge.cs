// SWSAnimatorBridge.cs  (put on the player)
using UnityEngine;

public class SWSAnimatorBridge : MonoBehaviour
{
    [Header("Animator & Param Names (match your controller)")]
    public Animator anim;
    public string horizParam = "InputHori";
    public string vertParam  = "InputVerti";
    public string magParam   = "InputMag";
    public string groundedParam = "IsGrounded";

    [Header("Tuning")]
    public float fullSpeed = 3.5f;      // set to your SWS moveSpeed so InputMag hits ~1
    public float lerp = 12f;            // smoothing for params

    Vector3 lastPos;
    Vector2 curHV;

    void Awake()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        lastPos = transform.position;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        Vector3 vWorld = (transform.position - lastPos) / dt;
        Vector3 vLocal = transform.InverseTransformDirection(vWorld);

        // Normalize to your expected full speed
        Vector2 hv = new Vector2(vLocal.x, vLocal.z) / Mathf.Max(fullSpeed, 0.01f);
        hv = Vector2.ClampMagnitude(hv, 1f);

        // Smooth
        curHV = Vector2.Lerp(curHV, hv, 1f - Mathf.Exp(-lerp * dt));

        if (anim)
        {
            if (!string.IsNullOrEmpty(horizParam)) anim.SetFloat(horizParam, curHV.x);
            if (!string.IsNullOrEmpty(vertParam))  anim.SetFloat(vertParam,  curHV.y);
            if (!string.IsNullOrEmpty(magParam))   anim.SetFloat(magParam,   curHV.magnitude);
            if (!string.IsNullOrEmpty(groundedParam)) anim.SetBool(groundedParam, true);
        }

        lastPos = transform.position;
    }
}
