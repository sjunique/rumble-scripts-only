using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ForceFieldVisualBinder : MonoBehaviour
{
    [Header("Links")]
    public ShieldRepelField repelField;          // assign your ShieldRepelField (can be on a sibling)
    public SphereCollider referenceCollider;     // the collider that defines the radius (usually on repelField)
    public Renderer rend;                        // the shield mesh renderer

    [Header("Visual Tweaks")]
    public float radiusPadding = 0.0f;           // extra scale if your mesh isn’t unit sphere
    public bool followPlayer = true;             // re-parent under player if not already

    [Header("Hit Ripple VFX (optional)")]
    public GameObject hitRipplePrefab;
    public float rippleLife = 0.75f;

    Transform _player;

    void Reset()
    {
        rend = GetComponent<Renderer>();
        if (!repelField) repelField = GetComponentInParent<ShieldRepelField>();
        if (!referenceCollider && repelField) referenceCollider = repelField.GetComponent<SphereCollider>();
    }

    void Start()
    {
        if (!rend) rend = GetComponent<Renderer>();
        if (!referenceCollider && repelField) referenceCollider = repelField.GetComponent<SphereCollider>();
        var link = PlayerCarLinker.Instance;
        _player = link && link.player ? link.player.transform : transform.root;

        if (followPlayer && _player && transform.parent != _player)
            transform.SetParent(_player, true);

        SyncScale();
        SyncVisibility();
    }

    void LateUpdate()
    {
        // keep scale & visibility in sync at runtime (radius or shieldActive may change)
        SyncScale();
        SyncVisibility();
    }

    void SyncScale()
    {
        if (!referenceCollider) return;

        // assume your shield mesh is a unit sphere (diameter 1). Scale = radius * 2
        float r = Mathf.Max(0.001f, referenceCollider.radius + radiusPadding);
        var s = Vector3.one * (r * 2f);
        if (transform.localScale != s) transform.localScale = s;

        // if the collider uses a center offset, follow it too
        transform.localPosition = referenceCollider.center;
    }

    void SyncVisibility()
    {
        if (!rend) return;
        bool on = repelField ? repelField.shieldActive : true;
        if (rend.enabled != on) rend.enabled = on;
    }

    // Allow gameplay to ping a ripple VFX on the surface
    public void SpawnHitRipple(Vector3 worldPoint, Vector3 normal)
    {
        if (!hitRipplePrefab) return;
        var v = Instantiate(hitRipplePrefab, worldPoint, Quaternion.LookRotation(normal));
        Destroy(v, rippleLife);
    }
}
