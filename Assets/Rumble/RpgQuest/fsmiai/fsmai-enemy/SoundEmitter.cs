using UnityEngine;

using System;
using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    public static event Action<Vector3, float> OnSoundEmitted;

    /// <param name="worldPos">Sound position</param>
    /// <param name="radius">Hearing radius in meters (before occlusion)</param>
    public static void Emit(Vector3 worldPos, float radius)
    {
        OnSoundEmitted?.Invoke(worldPos, radius);
    }
}
