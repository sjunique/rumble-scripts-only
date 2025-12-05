using UnityEngine;

public class PlayerAttackBridge : MonoBehaviour
{
    Animator anim;
    public KeyCode attackButton = KeyCode.Mouse0;   // Left Mouse button
    public KeyCode dashAttackButton = KeyCode.Mouse1; // Right Mouse button

    void Awake() => anim = GetComponent<Animator>();

    void Update()
    {
        if (Input.GetKeyDown(attackButton))
        {
            anim.SetInteger("AttackID", 0);   // change later for variety
            anim.SetTrigger("Attack");
        }

        if (Input.GetKeyDown(dashAttackButton))
        {
            anim.SetTrigger("DashAttack");
        }
    }
}