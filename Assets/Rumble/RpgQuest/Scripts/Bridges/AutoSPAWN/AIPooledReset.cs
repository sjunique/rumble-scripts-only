 
using Invector;
using UnityEngine;
using UnityEngine.AI;
using System.Reflection;
 
public interface IAIReset
{
    void OnPooledGet();
    void OnPooledReturn();
}


public class AIPooledReset : MonoBehaviour, IAIReset
{
    public void OnPooledGet()
    {
        // Health: prefer public API if present
        var hc = GetComponent<vHealthController>();
        if (hc)
        {
            // Try ResetHealth()
            var reset = hc.GetType().GetMethod("ResetHealth", BindingFlags.Instance | BindingFlags.Public);
            if (reset != null) reset.Invoke(hc, null);
            else
            {
                // Try AddHealth(maxHealth) or ChangeHealth(maxHealth)
                var add    = hc.GetType().GetMethod("AddHealth", BindingFlags.Instance | BindingFlags.Public);
                var change = hc.GetType().GetMethod("ChangeHealth", BindingFlags.Instance | BindingFlags.Public);

                if (add != null)    add.Invoke(hc, new object[] { hc.maxHealth });
                else if (change != null) change.Invoke(hc, new object[] { hc.maxHealth });
                // As a last resort, send a message (won’t error if not present)
                else SendMessage("ResetHealth", SendMessageOptions.DontRequireReceiver);
            }
        }

        // Agent: clear motion
        var agent = GetComponent<NavMeshAgent>();
        if (agent) { agent.isStopped = false; agent.ResetPath(); }

        // HeadTrack (optional): clear look targets if any
        var ht = GetComponent<Invector.vCharacterController.vHeadTrack>();
        if (ht)
        {
            if (ht.currentLookTarget) ht.RemoveLookTarget(ht.currentLookTarget.transform);
            ht.freezeLookPoint = false;
        }

        // Motor: ensure enabled so StepOffset etc. runs while alive
        var motor = (MonoBehaviour)GetComponent("vAIMotor");
        if (motor) motor.enabled = true;

        // Animator: rebind to avoid leftover states from previous life
        var anim = GetComponent<Animator>();
        if (anim) { anim.Rebind(); anim.Update(0f); }
    }

    public void OnPooledReturn()
    {
        // Optional: clear transient state before disabling
    }
}
