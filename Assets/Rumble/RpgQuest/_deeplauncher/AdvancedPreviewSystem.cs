using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AdvancedPreviewSystem : MonoBehaviour
{
    [System.Serializable]
    public class PreviewSettings
    {
        public RawImage previewImage;
        public Camera previewCamera;
        public List<GameObject> prefabs = new List<GameObject>();
        public Button prevButton;
        public Button nextButton;
        public Button rotateButton;
        public AnimatorOverrideController previewAnimation;
        
        [HideInInspector]
        public int currentIndex = 0;
        [HideInInspector]
        public GameObject currentInstance;
        [HideInInspector]
        public Animator currentAnimator;
        [HideInInspector]
        public PreviewModelRotator rotator;
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
        characterSettings.rotateButton.onClick.AddListener(() => ToggleRotation(ref characterSettings));
        
        vehicleSettings.prevButton.onClick.AddListener(() => CyclePreview(ref vehicleSettings, -1));
        vehicleSettings.nextButton.onClick.AddListener(() => CyclePreview(ref vehicleSettings, 1));
        vehicleSettings.rotateButton.onClick.AddListener(() => ToggleRotation(ref vehicleSettings));
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
                Vector3.zero, 
                Quaternion.identity
            );
            
            // Set up the preview instance
            SetupPreviewInstance(settings.currentInstance, ref settings);
        }
    }
    
    void SetupPreviewInstance(GameObject instance, ref PreviewSettings settings)
    {
        // Disable unnecessary components
        foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>())
        {
            // Keep only essential components for preview
            if (component != null && 
                component.GetType() != typeof(Transform) &&
                component.GetType() != typeof(MeshFilter) &&
                component.GetType() != typeof(MeshRenderer) &&
                component.GetType() != typeof(SkinnedMeshRenderer) &&
                component.GetType() != typeof(Animator))
            {
                component.enabled = false;
            }
        }
        
        // Set layer to preview camera's layer
        SetLayerRecursively(instance, settings.previewCamera.gameObject.layer);
        
        // Position the object in front of the camera
        instance.transform.position = settings.previewCamera.transform.position + 
                                     settings.previewCamera.transform.forward * 2f;
        
        // Face the object toward the camera
        instance.transform.LookAt(settings.previewCamera.transform);
        instance.transform.Rotate(0, 180, 0);
        
        // Add rotation script
        settings.rotator = instance.AddComponent<PreviewModelRotator>();
        
        // Set up animator if exists
        settings.currentAnimator = instance.GetComponent<Animator>();
        if (settings.currentAnimator != null && settings.previewAnimation != null)
        {
            settings.currentAnimator.runtimeAnimatorController = settings.previewAnimation;
            settings.currentAnimator.applyRootMotion = false;
        }
    }
    
    void ToggleRotation(ref PreviewSettings settings)
    {
        if (settings.rotator != null)
        {
            settings.rotator.ToggleRotation();
        }
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
