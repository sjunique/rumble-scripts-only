// Assets/Scripts/AI/Core/PlayerStatus.cs
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Tooltip("If true, enemies will flee instead of chasing")]
    public bool hasBodyguard = false;

    [Tooltip("Optional: bodyguard object; if assigned, auto-toggle based on distance")]
    public Transform bodyguard;
    public float bodyguardActiveRadius = 10f;

    void Update()
    {
        if (!bodyguard) return;
        hasBodyguard = Vector3.Distance(transform.position, bodyguard.position) <= bodyguardActiveRadius;
    }
}
