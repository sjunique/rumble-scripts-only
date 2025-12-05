using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PreviewSystem : MonoBehaviour
{
    [System.Serializable]
    public class PreviewSettings
    {
        public RawImage previewImage;
        public Camera previewCamera;
        public List<GameObject> prefabs = new List<GameObject>();
        public Button prevButton;
        public Button nextButton;
        
        [HideInInspector]
        public int currentIndex = 0;
        [HideInInspector]
        public GameObject currentInstance;
    }
    
    public PreviewSettings characterSettings;
    public PreviewSettings vehicleSettings;
    
    void Start()
    {
        // Initialize both preview systems
        InitializePreview(ref characterSettings);
        InitializePreview(ref vehicleSettings);
        
        // Set up button listeners
        characterSettings.prevButton.onClick.AddListener(() => CyclePreview(ref characterSettings, -1));
        characterSettings.nextButton.onClick.AddListener(() => CyclePreview(ref characterSettings, 1));
        
        vehicleSettings.prevButton.onClick.AddListener(() => CyclePreview(ref vehicleSettings, -1));
        vehicleSettings.nextButton.onClick.AddListener(() => CyclePreview(ref vehicleSettings, 1));
    }
    
    void InitializePreview(ref PreviewSettings settings)
    {
        if (settings.prefabs.Count == 0)
        {
            Debug.LogError("No prefabs assigned to preview system!");
            return;
        }
        
        // Set the render texture
        if (settings.previewCamera != null && settings.previewImage != null)
        {
            settings.previewImage.texture = settings.previewCamera.targetTexture;
        }
        
        // Instantiate the first prefab
        CyclePreview(ref settings, 0);
    }
    
    void CyclePreview(ref PreviewSettings settings, int direction)
    {
        // Remove current instance if it exists
        if (settings.currentInstance != null)
        {
            Destroy(settings.currentInstance);
        }
        
        // Calculate new index with wrap-around
        settings.currentIndex += direction;
        if (settings.currentIndex < 0) settings.currentIndex = settings.prefabs.Count - 1;
        if (settings.currentIndex >= settings.prefabs.Count) settings.currentIndex = 0;
        
        // Instantiate the new prefab
        if (settings.prefabs[settings.currentIndex] != null)
        {
            settings.currentInstance = Instantiate(
                settings.prefabs[settings.currentIndex], 
                settings.previewCamera.transform.position + settings.previewCamera.transform.forward * 2f, 
                Quaternion.identity
            );
            
            // Set up the preview instance
            SetupPreviewInstance(settings.currentInstance, settings.previewCamera);
        }
    }
    
    void SetupPreviewInstance(GameObject instance, Camera previewCamera)
    {
        // Disable unnecessary components
        foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>())
        {
            // Disable scripts that might interfere with preview
            if (component != null && 
                !(component is Transform) &&
                !(component is MeshFilter) &&
                !(component is MeshRenderer) &&
                !(component is SkinnedMeshRenderer) &&
                !(component is Animator))
            {
                component.enabled = false;
            }
        }
        
        // Set layer to preview layer if needed
        SetLayerRecursively(instance, previewCamera.gameObject.layer);
        
        // Position the object in front of the camera
        instance.transform.position = previewCamera.transform.position + previewCamera.transform.forward * 2f;
        
        // Face the object toward the camera
        instance.transform.LookAt(previewCamera.transform);
        instance.transform.Rotate(0, 180, 0); // Adjust based on your model's forward direction
    }
    
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    // Get the currently selected indices
    public int GetSelectedCharacterIndex()
    {
        return characterSettings.currentIndex;
    }
    
    public int GetSelectedVehicleIndex()
    {
        return vehicleSettings.currentIndex;
    }
    
    // Clean up when destroyed
    void OnDestroy()
    {
        if (characterSettings.currentInstance != null)
            Destroy(characterSettings.currentInstance);
        
        if (vehicleSettings.currentInstance != null)
            Destroy(vehicleSettings.currentInstance);
    }
}
