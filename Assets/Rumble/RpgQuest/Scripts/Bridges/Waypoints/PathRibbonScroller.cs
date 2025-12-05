// Assets/Rumble/RpgQuest/Bridges/Waypoints/PathRibbonScroller.cs
using UnityEngine;
using SWS;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PathRibbonScroller : MonoBehaviour
{
    public PathManager path;      // drag your SWS PathManager here
    public float width = 0.6f;    // ribbon width
    public float yOffset = 0.05f; // lift off the ground slightly
    public float scrollSpeed = 0.6f;

    LineRenderer lr;
    Material runtimeMat;

    void OnEnable()
    {
        lr = GetComponent<LineRenderer>();
        Rebuild();
    }

    public void Rebuild()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        if (!path || path.waypoints == null || path.waypoints.Length == 0)
        {
            if (lr) lr.positionCount = 0;
            return;
        }

        var wp = path.waypoints;
        lr.positionCount = wp.Length;
        for (int i = 0; i < wp.Length; i++)
            lr.SetPosition(i, wp[i].position + Vector3.up * yOffset);

        lr.widthMultiplier = width;
        lr.textureMode = LineTextureMode.Tile;   // important: tiles texture along length
        lr.alignment = LineAlignment.View;       // billboard to camera

        // create a unique material instance so offset/tiling don’t affect others
        if (lr.sharedMaterial && (runtimeMat == null || Application.isEditor && !Application.isPlaying))
            runtimeMat = lr.material;

        // tile the texture roughly by segment count (assumes wrap mode = Repeat)
        if (runtimeMat)
            runtimeMat.mainTextureScale = new Vector2(wp.Length, 1f);
    }

    void Update()
    {
        if (runtimeMat)
        {
            var off = runtimeMat.mainTextureOffset;
            off.x -= scrollSpeed * Time.deltaTime;    // negative = scroll forward
            runtimeMat.mainTextureOffset = off;
        }
#if UNITY_EDITOR
        // Refresh in edit mode if waypoints moved
        if (!Application.isPlaying) Rebuild();
#endif
    }
}
