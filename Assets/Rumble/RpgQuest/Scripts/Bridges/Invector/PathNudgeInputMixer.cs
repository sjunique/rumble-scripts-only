// PathNudgeInputMixer.cs  (attach to PLAYER)
using UnityEngine;
using Invector.vCharacterController;
using RpgQuest.Utilities;
[DefaultExecutionOrder(10000)]
public class PathNudgeInputMixer : MonoBehaviour
{
    [Mandatory]   public vThirdPersonController cc;
    [Mandatory]   public vThirdPersonInput inv;
     [Mandatory]  public PathFollowerNudge nudge;
    [Mandatory]   public KidModeAssist kidAssist;

    Vector2 user, combined;

    void Reset(){
        cc = GetComponent<vThirdPersonController>();
        inv = GetComponent<vThirdPersonInput>();
        nudge = GetComponent<PathFollowerNudge>();
        kidAssist = GetComponent<KidModeAssist>();
    }

    void Update()
    {
        if (!cc || !inv) return;

        var inRef = cc.input;
        user = new Vector2(inRef.x, inRef.z);

        if (nudge) nudge.SetUserInput02(user);

        combined = user;
        if (nudge)
        {
            if (nudge.autopilot)
            {
                var ap = nudge.AutoPilotInput02;
                if (ap.sqrMagnitude > 1e-3f) combined = Vector2.ClampMagnitude(ap, 1f);
            }
            else if (nudge.nudgeEnabled)
            {
                bool userHas = user.sqrMagnitude > 1e-4f;
                var assist = (nudge.nudgeOnlyWhenPlayerMoves && !userHas) ? Vector2.zero : nudge.NudgeInput02;
                combined = Vector2.ClampMagnitude(user + assist, 1f);
            }
        }

        if (kidAssist && kidAssist.enabled)
            combined *= Mathf.Clamp(kidAssist.CurrentSpeedMult, 0.1f, 1.5f);
    }

    void FixedUpdate()
    {
        if (!cc) return;
        var inp = cc.input; inp.x = combined.x; inp.z = combined.y; cc.input = inp;
    }

    void LateUpdate()
    {
        if (!cc) return;
        var inp = cc.input; inp.x = combined.x; inp.z = combined.y; cc.input = inp;
    }
}
// PathNudgeInputMixer.cs  (attach to PLAYER)