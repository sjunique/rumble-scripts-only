using UnityEngine;

public interface ICameraReset
{
    // Basic reset
    void ResetCamera();

    // Reset using a specific target (usually the player)
    void ResetCamera(UnityEngine.Transform target);
}
