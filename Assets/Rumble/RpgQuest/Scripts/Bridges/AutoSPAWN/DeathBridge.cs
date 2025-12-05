using UnityEngine;
using System;

public class DeathBridge : MonoBehaviour
{
    public event Action onDied;

    // Call these from your health script events:
    public void RaiseDied() => onDied?.Invoke();

    // If using Invector vHealthController:
#if INVECTOR_HEALTH_PRESENT
    Invector.vCharacterController.vHealthController _hc;
    void Awake()
    {
        _hc = GetComponent<Invector.vCharacterController.vHealthController>();
        if (_hc) _hc.onDead.AddListener(() => onDied?.Invoke());
    }
#endif
}

