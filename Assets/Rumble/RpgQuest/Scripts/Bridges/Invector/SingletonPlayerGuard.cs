 
// Attach on the Player prefab
using UnityEngine;
public class SingletonPlayerGuard : MonoBehaviour
{
    void Awake()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 1) { Debug.LogWarning("[PlayerGuard] Duplicate player, destroying " + name); Destroy(gameObject); }
    }
}
