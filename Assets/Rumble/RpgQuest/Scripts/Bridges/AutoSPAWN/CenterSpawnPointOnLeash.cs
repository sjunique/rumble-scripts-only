using UnityEngine;

public class CenterSpawnPointOnLeash : MonoBehaviour
{
    public BoxCollider leashCollider;
    public Transform spawnPoint;   // your AISpawnPoint

    void Reset()
    {
        leashCollider = GetComponent<BoxCollider>();
    }

    void Start()
    {
        if (!leashCollider || !spawnPoint) return;

        // compute world-space center of the box
        Vector3 worldCenter = leashCollider.transform.TransformPoint(leashCollider.center);
        spawnPoint.position = worldCenter;
    }
}
