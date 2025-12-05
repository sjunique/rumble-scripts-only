using UnityEngine;
using System.Collections.Generic;

public class RandomCollectibleSpawner : MonoBehaviour
{
    [Header("What to Place")]
    public GameObject collectiblePrefab;
    public int count = 10;

    [Header("Where to Place")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(30, 0, 30); // X=width, Y=unused, Z=depth

    [Header("How High Above Ground")]
    public float spawnHeight = 50f; // start raycast from above terrain
    public LayerMask groundLayer;









    public BoxCollider spawnArea; // Assign in inspector

    public float raycastHeight = 100f; // Start well above terrain
    [ContextMenu("Spawn Collectibles Now")]

    public void SpawnCollectibles()
    {
        if (!collectiblePrefab || !spawnArea) return;

        for (int i = 0; i < count; i++)
        {
            // Random XZ within area
            Vector3 local = new Vector3(
                Random.Range(-0.5f, 0.5f) * spawnArea.size.x,
                0,
                Random.Range(-0.5f, 0.5f) * spawnArea.size.z
            );
            Vector3 world = spawnArea.transform.TransformPoint(local + spawnArea.center);

            // Start HIGH above terrain, raycast DOWN
            Vector3 rayOrigin = new Vector3(world.x, spawnArea.transform.position.y + raycastHeight, world.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2, groundLayer))
            {
                // Snap to hit point (exact ground)
                Vector3 spawnPos = hit.point;

                // Optionally, raise slightly above surface to avoid z-fighting
                spawnPos.y += 0.05f;

                Instantiate(collectiblePrefab, spawnPos, Quaternion.identity, transform);
            }
            else
            {
                Debug.LogWarning("No ground hit at " + rayOrigin);
            }
        }
    }


    void OnDrawGizmosSelected()
    {
        if (!spawnArea) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnArea.transform.position + spawnArea.center, spawnArea.size);

        // Show random spawn samples (editor only)
        for (int i = 0; i < 10; i++)
        {
            Vector3 local = new Vector3(
                Random.Range(-0.5f, 0.5f) * spawnArea.size.x,
                0,
                Random.Range(-0.5f, 0.5f) * spawnArea.size.z
            );
            Vector3 world = spawnArea.transform.TransformPoint(local + spawnArea.center);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(world + Vector3.up * 2f, 0.3f);
        }
    }

}