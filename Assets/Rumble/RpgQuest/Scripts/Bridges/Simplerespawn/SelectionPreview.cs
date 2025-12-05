
using UnityEngine;
public class SelectionPreview : MonoBehaviour
{
    [Header("Where to spawn the preview")]
    [SerializeField] Transform previewParent;   // <- drag a GameObject here

    [SerializeField] SelectionScreenController selection;

    GameObject currentPreview;

    void Awake()
    {
        if (!selection) selection = FindObjectOfType<SelectionScreenController>(true);
        // Fallback: auto-create a parent if you forgot to assign one
        if (!previewParent) previewParent = EnsureParent("CharacterPreviewRoot");
    }

    public void ShowCharacter(GameObject prefab)
    {
        if (!prefab) return;
        if (currentPreview) Destroy(currentPreview);

        currentPreview = Instantiate(prefab, previewParent, false);
        ForcePreviewMode(currentPreview);
        ZeroLocal(currentPreview.transform);
    }

    Transform EnsureParent(string name)
    {
        var go = new GameObject(name);
        go.transform.position = Vector3.zero;   // move it in the Scene view as you like
        go.transform.rotation = Quaternion.identity;
        return go.transform;
    }

    void ForcePreviewMode(GameObject go)
    {
        var d = go.GetComponent<DisableForPreview>();
        if (!d) d = go.AddComponent<DisableForPreview>();
        d.autoApplyOnAwake = true;
        d.Apply();
    }

    void ZeroLocal(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }
}
