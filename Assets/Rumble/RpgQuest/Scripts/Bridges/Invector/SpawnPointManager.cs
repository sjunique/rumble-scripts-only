
using UnityEngine;
public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }

    [SerializeField] private Transform[] spawnPoints;

    void Awake()
    {
        Instance = this;
    }

    public Transform GetSpawnPoint(int index)
    {
        if (index < 0 || index >= spawnPoints.Length) return null;
        return spawnPoints[index];
    }

 

}
