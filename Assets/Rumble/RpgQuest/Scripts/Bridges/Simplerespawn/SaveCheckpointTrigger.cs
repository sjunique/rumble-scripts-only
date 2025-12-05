// Assets/Rumble/RpgQuest/savepoints/CheckpointTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveCheckpointTrigger : MonoBehaviour
{
    [Tooltip("Only save when this tag enters (your player).")]
    public string requiredTag = "Player";

    [Tooltip("Offset up from the player feet if needed.")]
    public float yOffset = 0.0f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        var t     = other.transform;
        var scene = SceneManager.GetActiveScene().name;

        // <- THIS is exactly where it goes:
        SaveService.SaveSceneCheckpoint(scene,
            t.position + Vector3.up * yOffset,
            t.rotation,
            has: true);

        Debug.Log($"[Checkpoint] Saved for '{scene}' at {t.position}");
    }
}
