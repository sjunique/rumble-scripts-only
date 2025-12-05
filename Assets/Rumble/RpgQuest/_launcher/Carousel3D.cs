// Assets/_Launcher/Scripts/Carousel3D.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// at top
using System;


public class Carousel3D : MonoBehaviour
{
    //new
    // --- add inside Carousel3D ---
[SerializeField] float targetHeightMeters = 1.8f;   // desired visual height
[SerializeField] bool freezeAnimator = true;
    [SerializeField] bool forceKinematic = true;
//
    [System.Serializable]
    public class Option
    {
        public string displayName;
        public GameObject prefab;
        public Vector3 localOffset;
        public float uniformScale = 1f;
    }

    [Header("Stage & Camera")]
    public Transform stage;             // unique per carousel
    public Camera previewCamera;        // unique per carousel
    [Tooltip("Layer name this carousel renders (e.g. PreviewChar or PreviewVeh)")]
    public string renderLayerName = "PreviewChar";

    [Header("UI (optional but recommended)")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text label;              // shows selected name
    public RawImage previewRawImage;    // the RawImage in the Canvas
    public RenderTexture renderTexture; // the RT assigned to the camera & RawImage

    [Header("Behaviour")]
    public List<Option> options = new();
    public int startIndex = 0;
    public bool autoRotate = true;
    public float rotateSpeed = 15f;
    public float framePadding = 1.15f;    // how much we zoom out when framing
    public bool autoFrameOnSwitch = true;

    [Header("Safety")]
    public bool forceExclusiveCulling = true; // set camera cull mask to ONLY this layer
    public bool setStageLayerToo = true;      // put stage (and its children) on this layer

    GameObject _current;
    int _index;
    int _layer = -1;
  
  
    void Awake()
    {
        // Clean button listeners so we don't drive *both* carousels by accident
        if (prevButton) { prevButton.onClick.RemoveAllListeners(); prevButton.onClick.AddListener(Prev); }
        if (nextButton) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(Next); }

        // Resolve layer
        _layer = LayerMask.NameToLayer(renderLayerName);
        if (_layer < 0) Debug.LogError($"[Carousel:{name}] Layer '{renderLayerName}' does not exist. Create it in Project Settings → Tags & Layers.");

        // Force camera config to be exclusive to this layer
        if (previewCamera)
        {
            if (forceExclusiveCulling && _layer >= 0)
                previewCamera.cullingMask = 1 << _layer;

            if (renderTexture) previewCamera.targetTexture = renderTexture;
        }

        // Ensure RawImage shows the same RT (optional)
        if (previewRawImage && renderTexture) previewRawImage.texture = renderTexture;

        // Put stage (& its children) on this layer so only our cam sees it
        if (setStageLayerToo && stage && _layer >= 0)
            SetLayerRecursive(stage.gameObject, _layer);

        // Start
        _index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, options.Count - 1));
        Show(_index);
        LogStatus("Awake");
    }

    void OnDisable()
    {
        // If you disable this carousel, remove its spawned preview so it can’t bleed into the other RT
        if (_current) { DestroyImmediate(_current); _current = null; }
    }

    void Update()
    {
        if (autoRotate && _current)
            _current.transform.Rotate(Vector3.up, rotateSpeed * Time.unscaledDeltaTime, Space.World);
    }

    public void Next()
    {
        if (options == null || options.Count == 0) return;
        _index = (_index + 1) % options.Count;
        Show(_index);
    }

    public void Prev()
    {
        if (options == null || options.Count == 0) return;
        _index = (_index - 1 + options.Count) % options.Count;
        Show(_index);
    }

    public int GetIndex() => _index;
    public GameObject GetCurrentPrefab() => (options != null && _index >= 0 && _index < options.Count) ? options[_index].prefab : null;

    void Show(int i)
    {
        if (_current) { DestroyImmediate(_current); _current = null; }

        var opt = (options != null && i >= 0 && i < options.Count) ? options[i] : null;
        if (opt == null || !opt.prefab || !stage) { LogStatus("Show(SKIP)"); return; }

        _current = Instantiate(opt.prefab, stage);
        _current.transform.localPosition = opt.localOffset;
        _current.transform.localRotation = Quaternion.identity;
        _current.transform.localScale = Vector3.one * Mathf.Max(0.0001f, opt.uniformScale);

        // Make sure the preview object is on the preview layer
        if (_layer >= 0) SetLayerRecursive(_current, _layer);

// NEW:
PreparePreviewInstance(_current);
        if (label) label.text = string.IsNullOrEmpty(opt.displayName) ? opt.prefab.name : opt.displayName;

        if (autoFrameOnSwitch && previewCamera) FrameCameraToCurrent();
        LogStatus($"Show({i})");
    }

    void SetLayerRecursive(GameObject go, int layer)
    {
        var stack = new Stack<Transform>();
        stack.Push(go.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            t.gameObject.layer = layer;
            for (int c = 0; c < t.childCount; c++) stack.Push(t.GetChild(c));
        }
    }

    void FrameCameraToCurrent()
    {
        var rends = _current ? _current.GetComponentsInChildren<Renderer>() : null;
        if (rends == null || rends.Length == 0 || !previewCamera) return;

        var b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);

        var cam = previewCamera;
        float vFov = cam.fieldOfView * Mathf.Deg2Rad;
        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * cam.aspect);

        float distV = b.extents.y / Mathf.Tan(vFov * 0.5f);
        float distH = b.extents.x / Mathf.Tan(hFov * 0.5f);
        float dist = Mathf.Max(distV, distH) * Mathf.Max(1f, framePadding);

        Vector3 center = b.center;
        var forward = cam.transform.forward;
        cam.transform.position = center - forward * dist + Vector3.up * (b.extents.y * 0.15f);
        cam.transform.LookAt(center, Vector3.up);
    }

    void LogStatus(string where)
    {
        var cam = previewCamera;
        string rt = cam && cam.targetTexture ? cam.targetTexture.name : "(null)";
        string layerName = (_layer >= 0) ? LayerMask.LayerToName(_layer) : "(invalid)";
        int mask = cam ? cam.cullingMask : 0;
        Debug.Log($"[Carousel:{name}/{where}] Stage={stage?.name}  Cam={cam?.name}  Layer={layerName}({_layer})  CullMask=0x{mask:X}  RT={rt}");
    }




    //new
    
    void PreparePreviewInstance(GameObject go)
{
    if (!go) return;

    // Optional: freeze animator so it doesn't play cycles in the launcher
    var anim = go.GetComponentInChildren<Animator>();
    if (anim && freezeAnimator) anim.speed = 0f;

    // Optional: force kinematic so it won't fall through your stage
    foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
        if (forceKinematic) rb.isKinematic = true;

    // Normalize scale to a target height and sit on the ground
    NormalizeToHeight(go, targetHeightMeters);
}

void NormalizeToHeight(GameObject go, float desiredHeight)
{
    var rends = go.GetComponentsInChildren<Renderer>();
    if (rends.Length == 0) return;

    var b = new Bounds(rends[0].bounds.center, Vector3.zero);
    foreach (var r in rends) b.Encapsulate(r.bounds);

    float h = Mathf.Max(0.0001f, b.size.y);
    float s = desiredHeight / h;

    // Scale uniformly around model root
    go.transform.localScale = Vector3.one * s;

    // Recompute bounds after scaling to place it nicely on the ground
    b = new Bounds(rends[0].bounds.center, Vector3.zero);
    foreach (var r in rends) b.Encapsulate(r.bounds);

    // Move so feet touch y=0 of the stage
    var local = go.transform.localPosition;
    float bottom = b.min.y - go.transform.position.y + local.y;
    go.transform.localPosition = new Vector3(local.x, local.y - bottom, local.z);
}
}
