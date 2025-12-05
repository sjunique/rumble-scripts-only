using UnityEngine;

public class EmeraldFootstepReceiver : MonoBehaviour
{
    // Called by animation event "PlayStep"
    public void PlayStep()
    {
        // Optional: play a footstep SFX here.
        // Example:
        // AudioSource.PlayClipAtPoint(footstepClip, transform.position, 0.6f);
    }
}
