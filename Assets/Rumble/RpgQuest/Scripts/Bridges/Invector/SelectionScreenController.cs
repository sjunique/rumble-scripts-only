using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Invector.vCharacterController;
 
 

public class SelectionScreenController : MonoBehaviour
{
    [Serializable]
    public class Option
    {
        public string displayName;
        public GameObject prefab;
        public Vector3 localOffset;
        public float uniformScale = 1f;
        public FrameAnchor anchor = FrameAnchor.Center;
        public float extraPadding = 1.12f;
        public float verticalNudge = 0f;
    }

    //[SerializeField] Color previewBackgroundColor = new Color(0.06f, 0.02f, 0.15f); 

    [SerializeField] Color previewBackgroundColor = new Color(0.85f, 0.82f, 0.95f);
    public enum FrameAnchor { Center, Feet }

    public event Action<GameObject> OnCharacterChanged;
    public event Action<GameObject> OnVehicleChanged;
    public GameObject CurrentCharacterPrefab { get; private set; }
    public GameObject CurrentVehiclePrefab { get; private set; }

    [Header("Shared")]
    [SerializeField] UIDocument uiDocument;
    [SerializeField] string previewLayerName = "PreviewChar";
    [SerializeField] bool autoRotate = true;
    [SerializeField] float rotateSpeed = 20f;
    [SerializeField] float normalizeHeight = 1.8f;
    [SerializeField] float framePadding = 1.10f;
    [SerializeField] bool freezeAnimator = true;
    [SerializeField] bool disableBehaviours = true;
    [SerializeField] bool forceKinematic = true;
    [SerializeField] bool disableColliders = true;

    [Header("Character Stage")]
    [SerializeField] Transform charStage;
    [SerializeField] Camera charCamera;
    [SerializeField] RenderTexture charRT;
    [SerializeField] List<Option> charOptions = new();
    [SerializeField] int charStartIndex = 0;

    [Header("Vehicle Stage")]
    [SerializeField] Transform vehStage;
    [SerializeField] Camera vehCamera;
    [SerializeField] RenderTexture vehRT;
    [SerializeField] List<Option> vehOptions = new();
    [SerializeField] int vehStartIndex = 0;

    // UI
    Button btnCharPrev, btnCharNext, btnVehPrev, btnVehNext;
    Label lblCharCount, lblVehCount, lblProgress;
    VisualElement progressFill, charDisplay, vehDisplay;

    int layer;
    GameObject charInst, vehInst;
    int charIndex, vehIndex;

    void OnDestroy() { PreviewMode.IsActive = false; }

    void OnDisable()
    {
        if (charInst != null) Destroy(charInst);
        if (vehInst  != null) Destroy(vehInst);
    }

    void Awake()
    {
        PreviewMode.IsActive = true;

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        var root = uiDocument != null ? uiDocument.rootVisualElement : null;

        // Query (all explicit null checks)
        btnCharPrev  = root != null ? root.Q<Button>("character-prev") : null;
        btnCharNext  = root != null ? root.Q<Button>("character-next") : null;
        btnVehPrev   = root != null ? root.Q<Button>("vehicle-prev")   : null;
        btnVehNext   = root != null ? root.Q<Button>("vehicle-next")   : null;
        lblCharCount = root != null ? root.Q<Label>("character-count") : null;
        lblVehCount  = root != null ? root.Q<Label>("vehicle-count")   : null;
        lblProgress  = root != null ? root.Q<Label>("progress-text")   : null;
        progressFill = root != null ? root.Q<VisualElement>("progress-fill") : null;
        charDisplay  = root != null ? root.Q<VisualElement>("character-display") : null;
        vehDisplay   = root != null ? root.Q<VisualElement>("vehicle-display")   : null;

        // RT Displays (ignore clicks)
        if (charDisplay != null && charRT != null)
            charDisplay.Add(new Image { image = charRT, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore });
        if (vehDisplay != null && vehRT != null)
            vehDisplay.Add(new Image { image = vehRT, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore });

        // Loadout singleton
        var loadout = SelectedLoadout.Instance;
        if (loadout == null)
        {
            var go = new GameObject("SelectedLoadout");
            DontDestroyOnLoad(go);
            loadout = go.AddComponent<SelectedLoadout>();
        }

        // Layer + cameras
        layer = LayerMask.NameToLayer(previewLayerName);
        if (layer < 0) Debug.LogError($"Missing layer '{previewLayerName}'");

        SetupPreviewCamera(charCamera, charRT);
        SetupPreviewCamera(vehCamera,  vehRT);

        if (charStage != null && layer >= 0) SetLayerRecursive(charStage.gameObject, layer);
        if (vehStage  != null && layer >= 0) SetLayerRecursive(vehStage.gameObject,  layer);

        // Buttons
        if (btnCharPrev != null) btnCharPrev.clicked += () => SetCharIndex(charIndex - 1);
        if (btnCharNext != null) btnCharNext.clicked += () => SetCharIndex(charIndex + 1);
        if (btnVehPrev  != null) btnVehPrev .clicked += () => SetVehIndex(vehIndex - 1);
        if (btnVehNext  != null) btnVehNext .clicked += () => SetVehIndex(vehIndex + 1);

        // Start indices
        charIndex = Mathf.Clamp(charStartIndex, 0, Mathf.Max(0, charOptions.Count - 1));
        vehIndex  = Mathf.Clamp(vehStartIndex,  0, Mathf.Max(0, vehOptions.Count  - 1));

        // Enable/disable button sets
        if (btnCharPrev  != null) btnCharPrev.SetEnabled(charOptions.Count > 0);
        if (btnCharNext  != null) btnCharNext.SetEnabled(charOptions.Count > 0);
        if (btnVehPrev   != null) btnVehPrev .SetEnabled(vehOptions.Count > 0);
        if (btnVehNext   != null) btnVehNext .SetEnabled(vehOptions.Count > 0);

        RefreshProgress(0.5f);
        ShowCharacter(charIndex);
        ShowVehicle(vehIndex);
        RefreshCounts();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (autoRotate && charInst != null) charInst.transform.Rotate(Vector3.up, rotateSpeed * dt, Space.World);
        if (autoRotate && vehInst  != null) vehInst .transform.Rotate(Vector3.up, rotateSpeed * dt, Space.World);
    }

    // --------- Carousel ----------
    void SetCharIndex(int i)
    {
        if (charOptions == null || charOptions.Count == 0) return;
        charIndex = (i % charOptions.Count + charOptions.Count) % charOptions.Count;
        ShowCharacter(charIndex);
        RefreshCounts();
    }

    void ShowCharacter(int i)
    {
        if (charInst != null) Destroy(charInst);
        var opt = Get(charOptions, i);
        if (opt == null || opt.prefab == null || charStage == null) return;

        charInst = Instantiate(opt.prefab, charStage);
        SetupPreviewObject(charInst, opt);
        CurrentCharacterPrefab = opt.prefab;
        var handler = OnCharacterChanged;
        if (handler != null) handler.Invoke(CurrentCharacterPrefab);
        if (charCamera != null) FrameCameraTo(charInst, charCamera);
    }

    void SetVehIndex(int i)
    {
        if (vehOptions == null || vehOptions.Count == 0) return;
        vehIndex = (i % vehOptions.Count + vehOptions.Count) % vehOptions.Count;
        ShowVehicle(vehIndex);
        RefreshCounts();
    }

    void ShowVehicle(int i)
    {
        if (vehInst != null) Destroy(vehInst);
        var opt = Get(vehOptions, i);
        if (opt == null || opt.prefab == null || vehStage == null) return;

        vehInst = Instantiate(opt.prefab, vehStage);
        SetupPreviewObject(vehInst, opt);
        CurrentVehiclePrefab = opt.prefab;
        var handler = OnVehicleChanged;
        if (handler != null) handler.Invoke(CurrentVehiclePrefab);
        if (vehCamera != null) FrameCameraTo(vehInst, vehCamera);
    }

    // --------- Helpers ----------
    Option Get(List<Option> list, int i)
    {
        if (list == null || list.Count == 0) return null;
        return (i >= 0 && i < list.Count) ? list[i] : null;
    }

    void RefreshCounts()
    {
        if (lblCharCount != null && charOptions != null && charOptions.Count > 0)
            lblCharCount.text = $"{charIndex + 1}/{charOptions.Count}";
        if (lblVehCount != null && vehOptions != null && vehOptions.Count > 0)
            lblVehCount.text = $"{vehIndex + 1}/{vehOptions.Count}";
    }

    void RefreshProgress(float t01)
    {
        if (progressFill != null)
            progressFill.style.width = new StyleLength(new Length(Mathf.Clamp01(t01) * 100f, LengthUnit.Percent));
        if (lblProgress != null)
            lblProgress.text = (t01 < 1f) ? "Step 1 of 2: Select Character & Vehicle" : "Step 2 of 2: Ready!";
    }

 void SetupPreviewCamera(Camera cam, RenderTexture rt)
{
    if (cam == null) return;
    cam.gameObject.tag = "Untagged";
    cam.enabled = true;
    cam.clearFlags = CameraClearFlags.SolidColor;
    cam.backgroundColor = previewBackgroundColor;  // 🎨 pastel-ish instead of black
    cam.nearClipPlane = 0.05f;
    cam.farClipPlane   = 1000f;
    if (rt != null) cam.targetTexture = rt;

    var l = LayerMask.NameToLayer(previewLayerName);
    if (l >= 0) cam.cullingMask = 1 << l;
}

    void SetupPreviewObject(GameObject go, Option opt)
    {
        go.transform.localPosition = opt.localOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * Mathf.Max(0.0001f, opt.uniformScale);

        var l = LayerMask.NameToLayer(previewLayerName);
        if (l >= 0) SetLayerRecursive(go, l);

        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null && freezeAnimator) anim.speed = 0f;

        if (forceKinematic)
        {
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            {
#if UNITY_6000_0_OR_NEWER
                rb.isKinematic = true; rb.linearVelocity = Vector3.zero;
#else
                rb.isKinematic = true; rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
            }
        }

        bool isInvector = go.GetComponentInChildren<vThirdPersonController>(true) != null;
        if (!isInvector)
        {
            if (disableBehaviours)
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && !(mb is Animator)) mb.enabled = false;

            if (disableColliders)
                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                    col.enabled = false;
        }

        NormalizeToHeight(go, normalizeHeight);
    }

    void NormalizeToHeight(GameObject go, float desiredHeight)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        var b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);
        float h = Mathf.Max(0.0001f, b.size.y);
        float s = desiredHeight / h;
        go.transform.localScale *= s;

        // re-ground
        b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);
        var local = go.transform.localPosition;
        float bottomWorldY = b.min.y;
        float modelRootWorldY = go.transform.position.y;
        float bottomLocalOffset = bottomWorldY - modelRootWorldY + local.y;
        go.transform.localPosition = new Vector3(local.x, local.y - bottomLocalOffset, local.z);
    }

    void FrameCameraTo(GameObject go, Camera cam)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0 || cam == null) return;

        var b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);

        float vFov = cam.fieldOfView * Mathf.Deg2Rad;
        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * cam.aspect);
        float distV = b.extents.y / Mathf.Tan(vFov * 0.5f);
        float distH = b.extents.x / Mathf.Tan(hFov * 0.5f);
        float dist  = Mathf.Max(distV, distH) * Mathf.Max(1f, framePadding);

        Vector3 center = b.center;
        Vector3 forward = cam.transform.forward;
        cam.transform.position = center - forward * dist + Vector3.up * (b.extents.y * 0.15f);
        cam.transform.LookAt(center, Vector3.up);
    }

    void SetLayerRecursive(GameObject go, int l)
    {
        var stack = new Stack<Transform>();
        stack.Push(go.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            t.gameObject.layer = l;
            for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
        }
    }
}
