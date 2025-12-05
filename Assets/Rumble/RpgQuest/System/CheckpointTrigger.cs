 

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
        gameObject.tag = "RespawnPoint";
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var rm = FindObjectOfType<PlayerRespawnManager>(true);
        if (rm) rm.SetCheckpoint(transform);

         //   Debug.Log($"[Checkpoint] setcheck for '{scene}' at {t.position}");
    }
}

