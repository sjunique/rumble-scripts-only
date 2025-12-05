using UnityEngine;

public class FootstepEventReceiver : MonoBehaviour
{
    [Header("Optional quick SFX hookup")]
    public AudioSource source;
    public AudioClip[] footstepClips;

    // Animation Event receiver
    public void PlayStep()
    {
        if (source != null && footstepClips != null && footstepClips.Length > 0)
        {
            var clip = footstepClips[Random.Range(0, footstepClips.Length)];
            source.PlayOneShot(clip);
        }
        // TODO: trigger dust VFX here if you have one.
    }
}
