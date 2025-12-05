 

 

// CarSummonGuard.cs
using UnityEngine;

public class CarSummonGuard : MonoBehaviour
{
    static float blockUntil = 0f;
    public static bool IsBlocked => Time.time < blockUntil;
    public static void BeginBlock(float seconds = 0.9f) => blockUntil = Time.time + seconds;
}
