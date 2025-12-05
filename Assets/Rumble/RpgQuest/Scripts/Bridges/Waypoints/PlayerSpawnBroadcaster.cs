using UnityEngine;
using System;

public class PlayerSpawnBroadcaster : MonoBehaviour
{
    public static event Action<Transform> OnPlayerSpawned;
    public static Transform Last { get; private set; }

    void Awake()
    {
        Last = transform;
        OnPlayerSpawned?.Invoke(transform);
    }
}
