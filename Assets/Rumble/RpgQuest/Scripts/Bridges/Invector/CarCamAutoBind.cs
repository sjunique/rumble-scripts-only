using UnityEngine;
using Unity.Cinemachine;
using Invector.vCamera;
using Invector;
[DefaultExecutionOrder(-60)]
public class CarCamAutoBind : MonoBehaviour
{
    [SerializeField] Camera mainCam;
    [SerializeField] CinemachineBrain brain;
    [SerializeField] vThirdPersonCamera invectorCamOnMain;

    void Awake()  { Rebind(); }
    void OnEnable(){ StartCoroutine(RebindNextFrame()); }

    System.Collections.IEnumerator RebindNextFrame()
    {
        yield return null; // wait until clones finish spawning this frame
        Rebind();
    }

    void Rebind()
    {
        // Pick the *single* active MainCamera
        mainCam = Camera.main;
        if (!mainCam)
        {
            Debug.LogError("[CarCamAutoBind] No Camera tagged MainCamera is active.");
            return;
        }

        // Unique Brain (must be ON that camera)
        brain = mainCam.GetComponent<CinemachineBrain>();
        if (!brain) brain = mainCam.gameObject.AddComponent<CinemachineBrain>();

        // vThirdPersonCamera lives on the same MainCamera
        invectorCamOnMain = mainCam.GetComponent<vThirdPersonCamera>();
        if (!invectorCamOnMain) invectorCamOnMain = mainCam.gameObject.AddComponent<vThirdPersonCamera>();

        // Keep the component enabled/disabled decision to CarCameraRig
        // (Don’t flip here; just ensure the reference is correct)
#if UNITY_EDITOR
//        Debug.Log($"[CarCamAutoBind] Bound Main={mainCam.name} Brain={brain != null} vTPC={invectorCamOnMain != null}");
#endif
    }

    // Expose getters so your CarCameraRig can pick them up if you don’t want to serialize
    public Camera MainCam => mainCam;
    public CinemachineBrain Brain => brain;
    public vThirdPersonCamera Vtpc => invectorCamOnMain;
}

