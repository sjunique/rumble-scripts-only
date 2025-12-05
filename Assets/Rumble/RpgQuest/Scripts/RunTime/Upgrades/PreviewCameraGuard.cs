using UnityEngine;

[DisallowMultipleComponent]
public class PreviewCameraGuard : MonoBehaviour
{
    public RenderTexture targetRT;
    public string layerName = "PreviewChar";
    public bool forceEveryFrame = true;

    int _mask;
    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        int layer = LayerMask.NameToLayer(layerName);
        _mask = (layer >= 0) ? (1 << layer) : 0;

        Apply();
    }

    void LateUpdate()
    {
        if (forceEveryFrame) Apply();
    }

    void Apply()
    {
        if (!_cam) return;
        _cam.enabled = true;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Color.black;
        _cam.nearClipPlane = 0.05f;
        _cam.farClipPlane = 1000f;
        if (_mask != 0) _cam.cullingMask = _mask;
        if (targetRT) _cam.targetTexture = targetRT;
    }
}
