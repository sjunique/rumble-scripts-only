using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TrailEffectFX : MonoBehaviour
{
    [Header("Trail Source")]
    [SerializeField] private Transform target;              // your player transform
    [SerializeField] private Mesh trailMesh;                // simple quad/mesh for segments
    [SerializeField] private int layer = 0;
    [SerializeField] private int subMeshMask = ~0;          // which submeshes to draw

    [Header("Trail Settings")]
    [SerializeField] private int maxTrailPoints = 100;      // maximum trail points to maintain
    [SerializeField] private float spawnInterval = 0.1f;    // time between trail point spawns

    [Header("Material")]
    [SerializeField] private Material baseTrailMaterial;    // assign in Inspector (URP/Unlit recommended)
    [SerializeField] private Color trailColor = new Color(0, 1, 1, 0.6f);

    // runtime
    private Material _trailMat;             // instancing-enabled clone
    private bool _canInstance;
    private bool _isInitialized = false;
    private readonly List<Matrix4x4> _matrices = new();
    private readonly List<Vector4> _colors = new();        // per-instance colors (RGBA)
       private MaterialPropertyBlock _mpb;
    private float _timeSinceLastSpawn;

    // batching constant
    const int MAX_BATCH = 1023;

    void Awake()
    {
        Initialize();
           _mpb = new MaterialPropertyBlock();
    }

    void Initialize()
    {
        // Create a default quad mesh if none is assigned
        if (trailMesh == null)
        {
            trailMesh = CreateUnitQuadMesh();
        }

        InitializeMaterial();
        _isInitialized = (_trailMat != null && trailMesh != null);
    }

    void InitializeMaterial()
    {
        // If you didn't assign a material, make a URP Unlit one that supports instancing
        if (baseTrailMaterial == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit"); // fallback
            if (sh == null) sh = Shader.Find("Standard"); // final fallback
            if (sh != null)
            {
                baseTrailMaterial = new Material(sh);
            }
            else
            {
                Debug.LogError("No suitable shader found for trail material!");
                return;
            }
        }

        // Always CLONE. Never modify shared asset at runtime.
        _trailMat = new Material(baseTrailMaterial);
        _trailMat.enableInstancing = true;

        // Best effort: set base color if the property exists
        if (_trailMat.HasProperty("_BaseColor")) 
            _trailMat.SetColor("_BaseColor", trailColor);
        else if (_trailMat.HasProperty("_Color")) 
            _trailMat.SetColor("_Color", trailColor);

        // Heuristic — if Unity refuses the flag for this shader, we'll fallback
        _canInstance = _trailMat.enableInstancing;
    }

    void Update()
    {
        if (!_isInitialized || target == null) return;
        
        // Update timer for spawning trail points
        _timeSinceLastSpawn += Time.deltaTime;
        
        // Add new trail point at intervals
        if (_timeSinceLastSpawn >= spawnInterval)
        {
            AppendTrailPoint(target.position, target.rotation, target.lossyScale);
            _timeSinceLastSpawn = 0f;
        }
    }

    void LateUpdate()
    {
        if (!_isInitialized || target == null || _matrices.Count == 0) return;
        
        // Draw the trail
        DrawBatched();
    }

    static Mesh CreateUnitQuadMesh()
    {
        var m = new Mesh { name = "UnitQuad" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
        };
        m.uv = new[]
        {
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(1,1), new Vector2(0,1),
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    void AppendTrailPoint(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        // Add the current pose to the trail
        _matrices.Add(Matrix4x4.TRS(pos, rot, scale));
        _colors.Add((Vector4)trailColor);

        // Cap list sizes (avoid unbounded growth)
        if (_matrices.Count > maxTrailPoints)
        {
            int remove = _matrices.Count - maxTrailPoints;
            _matrices.RemoveRange(0, remove);
            _colors.RemoveRange(0, remove);
        }
    }

    void DrawBatched()
    {
        if (_matrices.Count == 0 || _trailMat == null || trailMesh == null) return;

        // For each submesh we want to render
        int subMeshCount = trailMesh.subMeshCount;
        for (int s = 0; s < subMeshCount; s++)
        {
            if (((1 << s) & subMeshMask) == 0) continue;

            int drawn = 0;
            int total = _matrices.Count;

            while (drawn < total)
            {
                int count = Mathf.Min(MAX_BATCH, total - drawn);

                // Slice a batch
                var matricesArray = ListSliceToArray(_matrices, drawn, count);

                // Fill MPB with per-instance colors if the shader supports it
                bool hasBase = _trailMat.HasProperty("_BaseColor");
                bool hasColor = _trailMat.HasProperty("_Color");
                
                if (hasBase || hasColor)
                {
                    var colorsArray = ListSliceToArray(_colors, drawn, count);
                    if (hasBase) 
                        _mpb.SetVectorArray("_BaseColor", colorsArray);
                    else if (hasColor)
                        _mpb.SetVectorArray("_Color", colorsArray);
                }
                else
                {
                    _mpb.Clear();
                }

                if (_canInstance)
                {
                    // Instanced path (fast)
                    Graphics.DrawMeshInstanced(
                        trailMesh, s, _trailMat, matricesArray, count, _mpb,
                        ShadowCastingMode.Off, false, layer
                    );
                }
                else
                {
                    // Fallback path (never errors, just slower)
                    for (int i = 0; i < count; i++)
                    {
                        Graphics.DrawMesh(
                            trailMesh, matricesArray[i], _trailMat,
                            layer, null, s, _mpb, ShadowCastingMode.Off, false
                        );
                    }
                }

                drawn += count;
            }
        }
    }

    // Helper methods
    static Matrix4x4[] ListSliceToArray(List<Matrix4x4> list, int start, int count)
    {
        var arr = new Matrix4x4[count];
        for (int i = 0; i < count; i++) 
            arr[i] = list[start + i];
        return arr;
    }

    static Vector4[] ListSliceToArray(List<Vector4> list, int start, int count)
    {
        var arr = new Vector4[count];
        for (int i = 0; i < count; i++) 
            arr[i] = list[start + i];
        return arr;
    }

    // Cleanup
    void OnDestroy()
    {
        if (_trailMat != null)
        {
            if (Application.isPlaying)
                Destroy(_trailMat);
            else
                DestroyImmediate(_trailMat);
        }
    }

    // Public method to check if initialized
    public bool IsInitialized()
    {
        return _isInitialized;
    }
}