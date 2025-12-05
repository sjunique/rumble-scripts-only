 

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class SpawnPortalOnce : MonoBehaviour
{
    [Header("Portal Source (pick ONE)")]
    public GameObject portalPrefab;                 // drag your portal prefab here (non-addressable)

#if ENABLE_ADDRESSABLES
    public AssetReferenceGameObject portalAddressable; // or assign an Addressable
#endif

    [Header("Placement")]
    public Transform followTarget;                  // usually your Player root or spawn point
    public Vector3 offset = Vector3.zero;
    public bool parentToTarget = false;

    [Header("Timing")]
    [Tooltip("Seconds to keep the portal alive before auto-destroy")]
    public float lifetime = 3f;
    public float startDelay = 0f;                   // small delay if you need Player to settle first

    [Header("Uniqueness")]
    [Tooltip("Unique ID per spawn point (e.g., Spawn_A, Spawn_MainGate)")]
    public string uniqueSpawnId = "Spawn_Main";
    [Tooltip("Include scene name in the key so it only runs once per scene")]
    public bool perScene = true;

    // key -> PlayerPrefs
    string Key => (perScene ? SceneManager.GetActiveScene().name + ":" : "GLOBAL:") + uniqueSpawnId + ":PortalShown";

    void Start()
    {
         
    }





    IEnumerator PlayPortalOnceCo()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        var spawnPos = (followTarget ? followTarget.position : transform.position) + offset;
        var spawnRot = followTarget ? followTarget.rotation : transform.rotation;

#if ENABLE_ADDRESSABLES
        if (portalAddressable != null && portalAddressable.RuntimeKeyIsValid())
        {
            AsyncOperationHandle<GameObject> handle = portalAddressable.InstantiateAsync(spawnPos, spawnRot);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var go = handle.Result;
                if (parentToTarget && followTarget) go.transform.SetParent(followTarget, true);
                TryPlayParticleOrVFX(go);
                yield return new WaitForSeconds(lifetime);
                Addressables.ReleaseInstance(go);
            }
        }
        else
#endif
        if (portalPrefab != null)
        {
            var go = Instantiate(portalPrefab, spawnPos, spawnRot);
            if (parentToTarget && followTarget) go.transform.SetParent(followTarget, true);
            TryPlayParticleOrVFX(go);
            yield return new WaitForSeconds(lifetime);
            Destroy(go);
        }

        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();

        // this helper can go away
        Destroy(gameObject);
    }

public void TriggerPortal()
{
    if (PlayerPrefs.GetInt(Key, 0) == 1)
    {
        Debug.Log("Portal already played once. Ignored.");
        return;
    }

    StartCoroutine(PlayPortalOnceCo());
}




    void TryPlayParticleOrVFX(GameObject go)
    {
        // Auto-play common FX components if they exist
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps) ps.Play(true);

#if UNITY_2021_2_OR_NEWER
        // If using VFX Graph (optional)
        var vfx = go.GetComponentInChildren<UnityEngine.VFX.VisualEffect>();
        if (vfx) vfx.Play();
#endif
    }

    // Utility: call this to reset (e.g., from a debug menu)
    public void ResetShownFlag()
    {
        PlayerPrefs.DeleteKey(Key);
    }
}
