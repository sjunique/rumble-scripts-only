using UnityEngine;

// SimpleWaypointNodeMeta.cs
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SimpleWaypointNodeMeta : MonoBehaviour
{
    [Header("Beacon Stop")]
    public bool beaconStop = false;
   // public AssetReferenceGameObject beaconPrefab; // optional; if null use default
    //     public Vector3 beaconOffset = new Vector3(0, 0, 0);
 public GameObject beaconPrefab;
    [Header("Parking/Hover")]
    public float hoverEnterRadius = 10f;
    public float hoverEnterHeight = 6f;   // Above beacon Y
    public float hoverExitRadius  = 12f;  // hysteresis
    public float hoverExitHeight  = 4f;
    public float dwellSeconds = 2.0f;     // how long to hold before continuing (or until player exits)
}
