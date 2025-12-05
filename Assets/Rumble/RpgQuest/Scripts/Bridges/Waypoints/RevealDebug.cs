using UnityEngine;

// This script is used to reveal the route and collectibles in the game when a player interacts with a specific trigger.
// It is designed to be used in conjunction with the RouteStageBinder component to manage quest stages
// and collectibles visibility. The script also provides a debug method to log the reveal action in the
 
    [RequireComponent(typeof(Collider))]
    public class RevealDebug : MonoBehaviour
    {
        public GameObject routeRoot, collectiblesRoot;

        void OnEnable()
        {
            var b = GetComponent<RouteStageBinder>();
            b.controller = b.controller; // no-op, keeps Inspector visible
        }

        public void LogReveal()
        {
            Debug.Log($"[Binder] Enabling routeRoot={routeRoot?.name} activeNow={routeRoot?.activeSelf}");
            Debug.Log($"[Binder] Enabling collectiblesRoot={collectiblesRoot?.name} activeNow={collectiblesRoot?.activeSelf}");
        }
    }
 