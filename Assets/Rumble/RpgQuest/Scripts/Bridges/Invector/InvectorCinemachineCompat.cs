using UnityEngine;
 
using Invector.vCamera;
using Invector.vCharacterController;

[DefaultExecutionOrder(-50)]
public class InvectorCinemachineCompat : MonoBehaviour
{
    [SerializeField] vShooterMeleeInput shooter;
    [SerializeField] Camera mainCam;  // Camera with CinemachineBrain

    void Reset() { shooter = GetComponent<vShooterMeleeInput>(); }

    void Awake()
    {
        if (!shooter) shooter = GetComponent<vShooterMeleeInput>();
        if (!mainCam) mainCam = Camera.main;
        if (!mainCam) { Debug.LogError("[Compat] No MainCamera found."); return; }

        // Ensure a stub exists on MainCamera
        var stub = mainCam.GetComponent<vThirdPersonCamera>();
        if (!stub) stub = mainCam.gameObject.AddComponent<vThirdPersonCamera>();

        stub.enabled = false;            // keep inert; Cinemachine owns the camera
        shooter.tpCamera = stub;         // satisfy Invector

        if (mainCam.tag != "MainCamera") mainCam.tag = "MainCamera";
    }

    void Start()
    {
        // Reassert in case something overwrote it on Start
        if (shooter && shooter.tpCamera == null)
            shooter.tpCamera = Camera.main?.GetComponent<vThirdPersonCamera>();
    }
}
