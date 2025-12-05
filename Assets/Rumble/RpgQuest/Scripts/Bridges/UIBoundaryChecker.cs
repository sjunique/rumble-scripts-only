using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIBoundaryChecker : MonoBehaviour
{
    [Header("Settings")]
    public bool checkOnStart = true;
    public bool enableVisualization = true;
    public Color safeColor = Color.green;
    public Color outOfBoundsColor = Color.red;
    
    [Header("Debug Tools")]
    public bool showBoundsInEditor = true;
    
    private Canvas canvas;
    private RectTransform canvasRect;
    private Dictionary<RectTransform, Color> originalColors = new Dictionary<RectTransform, Color>();
    
    void Start()
    {
        // Get reference to the Canvas on this same GameObject
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("UIBoundaryChecker must be attached to a GameObject with a Canvas component!");
            return;
        }
        
        canvasRect = canvas.GetComponent<RectTransform>();
        
        if (checkOnStart)
        {
            CheckAllUIElements();
        }
    }
    
    void Update()
    {
        // Visual debugging in Play mode
        if (enableVisualization && Application.isPlaying)
        {
            DrawDebugBounds();
        }
    }
    
    public void CheckAllUIElements()
    {
        // Clear previous colors
        originalColors.Clear();
        
        // Get all UI elements in the canvas
        RectTransform[] allUIElements = GetComponentsInChildren<RectTransform>(true);
        
        foreach (RectTransform element in allUIElements)
        {
            // Skip the canvas itself
            if (element == canvasRect) continue;
            
            CheckElementBounds(element);
        }
    }
    
    private void CheckElementBounds(RectTransform element)
    {
        // Store original color if we haven't already
        if (!originalColors.ContainsKey(element))
        {
            Image img = element.GetComponent<Image>();
            if (img != null)
            {
                originalColors[element] = img.color;
            }
        }
        
        // Check if element is within canvas bounds
        bool isOutOfBounds = IsElementOutOfBounds(element);
        
        if (isOutOfBounds)
        {
            Debug.LogWarning($"UI element '{element.name}' is out of bounds!", element);
            
            // Visual indication
            if (enableVisualization)
            {
                Image img = element.GetComponent<Image>();
                if (img != null)
                {
                    img.color = outOfBoundsColor;
                }
            }
            
            // Auto-fix the position
            AutoFixElementPosition(element);
        }
        else if (enableVisualization)
        {
            // Restore original color if within bounds
            Image img = element.GetComponent<Image>();
            if (img != null && originalColors.ContainsKey(element))
            {
                img.color = originalColors[element];
            }
        }
    }
    
    private bool IsElementOutOfBounds(RectTransform element)
    {
        // Get the corners of the UI element in canvas space
        Vector3[] elementCorners = new Vector3[4];
        element.GetWorldCorners(elementCorners);
        
        // Convert to canvas space
        for (int i = 0; i < 4; i++)
        {
            elementCorners[i] = canvas.transform.InverseTransformPoint(elementCorners[i]);
        }
        
        // Get canvas bounds
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 canvasCenter = canvasRect.rect.center;
        Rect canvasBounds = new Rect(
            canvasCenter.x - canvasSize.x / 2,
            canvasCenter.y - canvasSize.y / 2,
            canvasSize.x,
            canvasSize.y
        );
        
        // Check if any corner is outside canvas bounds
        foreach (Vector3 corner in elementCorners)
        {
            if (!canvasBounds.Contains(corner))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void AutoFixElementPosition(RectTransform element)
    {
        // Get element dimensions
        float elementWidth = element.rect.width;
        float elementHeight = element.rect.height;
        
        // Get current anchored position
        Vector2 anchoredPosition = element.anchoredPosition;
        
        // Get canvas dimensions
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        
        // Calculate maximum allowed positions
        float maxX = canvasWidth / 2 - elementWidth * (1 - element.pivot.x);
        float minX = -canvasWidth / 2 + elementWidth * element.pivot.x;
        float maxY = canvasHeight / 2 - elementHeight * (1 - element.pivot.y);
        float minY = -canvasHeight / 2 + elementHeight * element.pivot.y;
        
        // Clamp position to stay within bounds
        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
        
        // Apply the corrected position
        element.anchoredPosition = anchoredPosition;
        
        Debug.Log($"Auto-adjusted position of '{element.name}' to {anchoredPosition}");
    }
    
    private void DrawDebugBounds()
    {
        if (!showBoundsInEditor) return;
        
        // Draw canvas bounds
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(canvasCorners[i], canvasCorners[(i + 1) % 4], Color.blue);
        }
        
        // Draw bounds for all UI elements
        RectTransform[] allUIElements = GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform element in allUIElements)
        {
            if (element == canvasRect) continue;
            
            Vector3[] corners = new Vector3[4];
            element.GetWorldCorners(corners);
            
            Color drawColor = IsElementOutOfBounds(element) ? Color.red : Color.green;
            
            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(corners[i], corners[(i + 1) % 4], drawColor);
            }
        }
    }
    
    // Method to call from editor button or manually
    [ContextMenu("Validate UI Boundaries")]
    public void ValidateNow()
    {
        CheckAllUIElements();
    }
    
    // Editor-only visualization
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showBoundsInEditor || !enabled) return;
        
        if (canvasRect == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>();
        }
        
        if (canvasRect != null)
        {
            // Draw canvas bounds in editor
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            
            for (int i = 0; i < 4; i++)
            {
                UnityEditor.Handles.color = Color.blue;
                UnityEditor.Handles.DrawDottedLine(canvasCorners[i], canvasCorners[(i + 1) % 4], 2f);
            }
            
            // Draw UI element bounds in editor
            if (Application.isPlaying) return; // Let Update handle this in Play mode
            
            RectTransform[] allUIElements = GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform element in allUIElements)
            {
                if (element == canvasRect) continue;
                
                Vector3[] corners = new Vector3[4];
                element.GetWorldCorners(corners);
                
                bool outOfBounds = IsElementOutOfBounds(element);
                UnityEditor.Handles.color = outOfBounds ? Color.red : Color.green;
                
                for (int i = 0; i < 4; i++)
                {
                    UnityEditor.Handles.DrawDottedLine(corners[i], corners[(i + 1) % 4], 2f);
                }
                
                if (outOfBounds)
                {
                    UnityEditor.Handles.Label(corners[1], element.name, UnityEditor.EditorStyles.boldLabel);
                }
            }
        }
    }
    #endif
}