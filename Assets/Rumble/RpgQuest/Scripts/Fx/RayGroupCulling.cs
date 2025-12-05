using UnityEngine;

 

public class RayGroupCulling : MonoBehaviour
{
    public Transform playerOrCamera;   // assign at runtime if you want
    public float enableDistance = 80f;

    Renderer[] rends;
    void Awake() => rends = GetComponentsInChildren<Renderer>(true);

    void LateUpdate()
    {
        var t = playerOrCamera ? playerOrCamera : (Camera.main ? Camera.main.transform : null);
        if (!t) return;

        float d2 = (t.position - transform.position).sqrMagnitude;
        bool visible = d2 < enableDistance * enableDistance && IsOnScreen();

        for (int i = 0; i < rends.Length; i++)
            if (rends[i]) rends[i].enabled = visible;
    }

    bool IsOnScreen()
    {
        var cam = Camera.main;
        if (!cam) return true;
        var p = cam.WorldToViewportPoint(transform.position);
        return p.z > 0 && p.x > -0.1f && p.x < 1.1f && p.y > -0.1f && p.y < 1.1f;
    }
}
