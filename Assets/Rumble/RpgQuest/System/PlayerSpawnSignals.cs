using System;
using UnityEngine;

public static class PlayerSpawnSignals
{
    public static Transform CurrentPlayer { get; private set; }
    public static event Action<Transform> OnPlayerReady;

    public static void Announce(Transform player)
    {
        if (player == null) return;
        CurrentPlayer = player;
        OnPlayerReady?.Invoke(player);
    }
}

