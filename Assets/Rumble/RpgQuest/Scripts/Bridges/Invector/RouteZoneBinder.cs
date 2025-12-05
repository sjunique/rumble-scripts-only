 using UnityEngine;

 [RequireComponent(typeof(Collider))]
public class RouteZoneBinder : MonoBehaviour
{
    public PathFollowerNudge nudge;
    public WaypointPathVisualizer route;
    public bool enableNudge   = true;
    public bool startAutopilot= true;
    public bool fromStart     = true;

    [Header("Safety")]
    public string playerTag = "Player";
    public bool oneShot = true;
    public float cooldown = 1.0f;
    bool _busy, _used;

    void Reset(){ GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (_busy || _used) return;
        if (!other.CompareTag(playerTag)) return;
        StartCoroutine(BindRoutine());
    }

    System.Collections.IEnumerator BindRoutine()
    {
        _busy = true;

        if (route && !route.gameObject.activeSelf)
            route.gameObject.SetActive(true);

        yield return null; // let path points build this frame

        if (nudge && route)
            nudge.SetRoute(route, enableNudge, startAutopilot, fromStart);

        if (oneShot){ _used = true; gameObject.SetActive(false); }
        else        { yield return new WaitForSeconds(cooldown); _busy = false; }
    }
}
// This script binds a player to a route when they enter a trigger zone.
// It uses a PathFollowerNudge to control the player's movement along the route.    



// [RequireComponent(typeof(Collider))]
// public class RouteZoneBinder : MonoBehaviour
// {
//     public PathFollowerNudge nudge;
//     public WaypointPathVisualizer route;
//     public bool enableNudge = true;
//     public bool startAutopilot = true;
//     public bool fromStart = true;
//     public bool oneShot = true;

//     void Reset() { GetComponent<Collider>().isTrigger = true; }

//     void OnTriggerEnter(Collider other)
//     {
//         if (!other.CompareTag("Player") || !nudge || !route) return;
//         StartCoroutine(BindNextFrame());
//     }

//     System.Collections.IEnumerator BindNextFrame()
//     {
//         if (!route.gameObject.activeSelf) route.gameObject.SetActive(true);
//         yield return null; // let the visualizer build its PathPoints
//         nudge.SetRoute(route, enableNudge, startAutopilot, fromStart);
//         if (oneShot) gameObject.SetActive(false);
//     }
// }
