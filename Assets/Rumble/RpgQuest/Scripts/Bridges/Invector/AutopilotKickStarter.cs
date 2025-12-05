// AutopilotKickStarter.cs  (attach to PLAYER)
using UnityEngine;
using Invector.vCharacterController;

[DefaultExecutionOrder(20000)]
public class AutopilotKickStarter : MonoBehaviour
{
    public vThirdPersonController cc;
    public PathFollowerNudge nudge;
    float until;

    void Reset(){ cc = GetComponent<vThirdPersonController>(); nudge = GetComponent<PathFollowerNudge>(); }

    public void Kick(float seconds = 0.25f)
    {
        until = Time.time + Mathf.Max(0.05f, seconds);
        enabled = true;
    }

    void FixedUpdate()
    {if (Time.frameCount % 20 == 0)
    Debug.Log($"hello[Mixer] ap={nudge && nudge.autopilot} ap02={ (nudge? nudge.AutoPilotInput02 : Vector2.zero) } cc=({cc.input.x:F2},{cc.input.z:F2})");
 


        if (Time.time >= until) { enabled = false; return; }
        if (!cc || !nudge) return;

        var ap = nudge.AutoPilotInput02;
        if (ap.sqrMagnitude < 1e-4f) ap = new Vector2(0f, 1f); // tiny forward
        var inp = cc.input; inp.x = ap.x; inp.z = ap.y; cc.input = inp;
    }
}
