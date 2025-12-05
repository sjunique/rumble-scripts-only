using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArrowLineScroller : MonoBehaviour
{
    public WaypointPathVisualizer path;  // assign your visualizer
    public Material materialInstance;    // assign a duplicate material for this path
    public float metersPerArrow = 2.0f;  // how dense the arrows are
    public float scrollSpeed = 0.8f;     // arrows speed along path
    public bool autoAssignBaseMap = true;

    static readonly int _BaseMap = Shader.PropertyToID("_BaseMap");
  //  static readonly int _MainTex = Shader.PropertyToID("_MainTex");

    LineRenderer _line;
    float _offset;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        if (!materialInstance && _line)
            materialInstance = Instantiate(_line.sharedMaterial);
        if (_line) _line.material = materialInstance;

        // Ensure texture repeats
        if (materialInstance)
        {
            var tex = materialInstance.HasProperty(_BaseMap)
                ? materialInstance.GetTexture(_BaseMap)
                : materialInstance.mainTexture;
            if (tex) tex.wrapMode = TextureWrapMode.Repeat;
        }
    }

    void Update()
    {
        if (!materialInstance || path == null || path.PathPoints == null || path.PathPoints.Count < 2) return;

        float length = 0f;
        var pts = path.PathPoints;
        for (int i = 0; i < pts.Count - 1; i++) length += Vector3.Distance(pts[i], pts[i + 1]);

        // Tile X by path length so arrows repeat nicely
        float tileX = Mathf.Max(1f, length / Mathf.Max(0.01f, metersPerArrow));
        SetScaleOffset(new Vector2(tileX, 1f), new Vector2(_offset, 0f));

        // Scroll
        _offset += Time.deltaTime * (scrollSpeed / Mathf.Max(0.01f, metersPerArrow));
        if (_offset > 1f) _offset -= 1f;
    }

    void SetScaleOffset(Vector2 scale, Vector2 offset)
    {
        if (materialInstance.HasProperty(_BaseMap))
        {
            materialInstance.SetTextureScale(_BaseMap, scale);
            materialInstance.SetTextureOffset(_BaseMap, offset);
        }
        else
        {
            materialInstance.mainTextureScale = scale;
            materialInstance.mainTextureOffset = offset;
        }
    }
}

