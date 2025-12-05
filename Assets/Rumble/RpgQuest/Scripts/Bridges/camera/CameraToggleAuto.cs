using UnityEngine;
using Unity.Cinemachine;

public class CameraToggleAuto : MonoBehaviour
{
    private CinemachineCamera cinemachineCamera;
    
    [Header("Toggle Settings")]
    public KeyCode toggleKey = KeyCode.C;
    
    private void Start()
    {
        // Automatically find the Cinemachine camera
        cinemachineCamera = FindObjectOfType<CinemachineCamera>();
        
        if (cinemachineCamera == null)
        {
            Debug.LogError("No Cinemachine Camera found in the scene!");
              cinemachineCamera.enabled = false;
            cinemachineCamera.enabled = true;
        }
        else
        {
            Debug.Log($"Found Cinemachine camera: {cinemachineCamera.gameObject.name}");
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && cinemachineCamera != null)
        {
            ToggleCamera();
        }
    }
    
    public void ToggleCamera()
    {
        cinemachineCamera.gameObject.SetActive(!cinemachineCamera.gameObject.activeInHierarchy);
        Debug.Log($"Cinemachine Camera is now: {(cinemachineCamera.gameObject.activeInHierarchy ? "ENABLED" : "DISABLED")}");
    }
}