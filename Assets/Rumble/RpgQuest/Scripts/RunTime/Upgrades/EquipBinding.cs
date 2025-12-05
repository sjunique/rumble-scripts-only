using UnityEngine;

public class EquipBinding : MonoBehaviour
{
    public enum Mode { ActivateExisting, AttachToBone, SpawnAtPoint }

    [Header("Binding")]
    public UpgradeId upgradeId;
    public Mode mode = Mode.ActivateExisting;

    [Header("ActivateExisting (Shield/Scuba/BeltLaser child)")]
    public GameObject existingTarget;

    [Header("AttachToBone (LaserBelt prefab)")]
    public Animator playerAnimator;                  // assign your player's Animator
    public Transform attachBone;                     // OR assign bone directly
    public HumanBodyBones preferredHumanBone = HumanBodyBones.Spine; // fallback if using humanoid
    [Tooltip("Optional: if set, we will search the player's hierarchy for a bone whose name contains this (e.g. 'Spine1', 'mixamorig:Spine1').")]
    public string searchBoneNameContains;
    public GameObject attachPrefab;
    public Vector3 localPositionOffset;
    public Vector3 localEulerOffset;
    public bool destroyOnUnequip = false;
    GameObject attachedInstance;

    [Header("SpawnAtPoint (Pet/Bodyguard)")]
    public Transform spawnPoint;
    public GameObject spawnPrefab;
    public bool despawnOnUnequip = true;
    GameObject spawnedInstance;

    bool _lastEquipped;

    void Start()
    {
        Apply(UpgradeStateManager.Instance && UpgradeStateManager.Instance.IsEquipped(upgradeId));
    }

    void Update()
    {
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        bool eq = mgr.IsEquipped(upgradeId);
        if (eq != _lastEquipped)
            Apply(eq);
    }

    public void Apply(bool equipped)
    {
        _lastEquipped = equipped;

        switch (mode)
        {
            case Mode.ActivateExisting:
                if (existingTarget) existingTarget.SetActive(equipped);
                break;

            case Mode.AttachToBone:
                HandleAttachToBone(equipped);
                break;

            case Mode.SpawnAtPoint:
                HandleSpawnAtPoint(equipped);
                break;
        }
    }

    void HandleAttachToBone(bool equipped)
    {
        var bone = ResolveAttachBone();
        if (equipped)
        {
            if (attachedInstance == null)
            {
                if (!attachPrefab || !bone)
                {
                    Debug.LogWarning($"[EquipBinding] {upgradeId}: missing attachPrefab or bone.");
                    return;
                }
                attachedInstance = Instantiate(attachPrefab, bone);
                var t = attachedInstance.transform;
                t.localPosition = localPositionOffset;
                t.localRotation = Quaternion.Euler(localEulerOffset);
            }
            else
            {
                attachedInstance.transform.SetParent(bone, false);
                attachedInstance.SetActive(true);
            }
        }
        else
        {
            if (attachedInstance)
            {
                if (destroyOnUnequip) Destroy(attachedInstance);
                else attachedInstance.SetActive(false);
                attachedInstance = null;
            }
        }
    }

    Transform ResolveAttachBone()
    {
        // explicit wins
        if (attachBone) return attachBone;

        // try animator
        var anim = playerAnimator;
        if (!anim)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) anim = player.GetComponentInChildren<Animator>();
        }

        // 1) if we have a name hint, search by partial name
        if (!string.IsNullOrEmpty(searchBoneNameContains))
        {
            var root = anim ? anim.transform : null;
            if (root)
            {
                var lower = searchBoneNameContains.ToLowerInvariant();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLowerInvariant().Contains(lower))
                        return t;
                }
            }
        }

        // 2) try humanoid bone (Spine/Chest/UpperChest/Hips)
        if (anim && anim.isHuman)
        {
            var t = anim.GetBoneTransform(preferredHumanBone);
            if (t) return t;

            // fallbacks
            t = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (t) return t;
            t = anim.GetBoneTransform(HumanBodyBones.UpperChest);
            if (t) return t;
            t = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (t) return t;
        }

        Debug.LogWarning("[EquipBinding] Could not resolve attach bone. Assign 'attachBone' or 'playerAnimator', or set 'searchBoneNameContains' (e.g., 'Spine1').");
        return null;
    }

    void HandleSpawnAtPoint(bool equipped)
    {
        if (equipped)
        {
            if (spawnedInstance == null)
            {
                if (!spawnPoint || !spawnPrefab)
                {
                    Debug.LogWarning($"[EquipBinding] {upgradeId}: missing spawnPoint or spawnPrefab.");
                    return;
                }

                spawnedInstance = Instantiate(spawnPrefab, spawnPoint.position, spawnPoint.rotation);

                // auto-wire follow
                var follow = spawnedInstance.GetComponent<CompanionFollow>();
                if (follow && !follow.player)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player) follow.player = player.transform;
                }
            }
            else spawnedInstance.SetActive(true);
        }
        else
        {
            if (spawnedInstance)
            {
                if (despawnOnUnequip) Destroy(spawnedInstance);
                else spawnedInstance.SetActive(false);
                spawnedInstance = null;
            }
        }
    }
}
