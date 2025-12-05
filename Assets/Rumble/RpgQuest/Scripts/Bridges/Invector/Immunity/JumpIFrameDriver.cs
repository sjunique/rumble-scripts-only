using UnityEngine;
using Invector.vCharacterController;
// Assets/Rumble/RpgQuest/Combat/JumpIFrameDriver.cs
 

[DisallowMultipleComponent]
public class JumpIFrameDriver : MonoBehaviour
{
    public bool ownsJumpImmunity;   // set via binder (or inspector)
    public bool enabledForJump = true;
    public float airSafeWindow = 0.6f;

    ImmunityController imm; vThirdPersonMotor motor; bool wasGrounded = true;
    void Awake(){ imm = GetComponent<ImmunityController>(); motor = GetComponent<vThirdPersonMotor>(); }

    void Update()
    {
        if (!motor) return;
        bool grounded = motor.isGrounded;
        if (wasGrounded && !grounded && ownsJumpImmunity && enabledForJump)
            imm?.Add(DamageCategory.Fall, "JumpIFrame", airSafeWindow);
        wasGrounded = grounded;
    }
}

