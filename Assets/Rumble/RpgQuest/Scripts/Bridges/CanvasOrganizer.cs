using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CanvasOrganizer : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas purchaseCanvas;
    public Canvas questHUDCanvas;
    public Canvas questPointUICanvas;
    
    [Header("Layout Settings")]
    public bool enableVisualDebugging = true;
    public Color canvasBoundsColor = new Color(1, 0.5f, 0, 0.3f);
    public float updateInterval = 1f;
    
    private Dictionary<Canvas, RectTransform> canvasRects = new Dictionary<Canvas, RectTransform>();
    private Dictionary<Canvas, List<RectTransform>> childElements = new Dictionary<Canvas, List<RectTransform>>();
    private float updateTimer = 0f;
    
    void Start()
    {
        InitializeCanvasData();
        LogCanvasInfo();
    }
    
    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateCanvasData();
            updateTimer = 0f;
        }
    }
    
    void InitializeCanvasData()
    {
        RegisterCanvas(purchaseCanvas);
        RegisterCanvas(questHUDCanvas);
        RegisterCanvas(questPointUICanvas);
    }
    
    void RegisterCanvas(Canvas canvas)
    {
        if (canvas != null)
        {
            canvasRects[canvas] = canvas.GetComponent<RectTransform>();
            childElements[canvas] = new List<RectTransform>();
            
            // Get all child UI elements
            foreach (RectTransform child in canvas.GetComponent<RectTransform>())
            {
                childElements[canvas].Add(child);
            }
        }
    }
    
    void UpdateCanvasData()
    {
        foreach (var canvas in canvasRects.Keys)
        {
            if (canvas != null)
            {
                childElements[canvas].Clear();
                foreach (RectTransform child in canvas.GetComponent<RectTransform>())
                {
                    childElements[canvas].Add(child);
                }
            }
        }
    }
    
    void LogCanvasInfo()
    {
        Debug.Log("=== Canvas Layout Information ===");
        
        foreach (var pair in canvasRects)
        {
            Canvas canvas = pair.Key;
            RectTransform rect = pair.Value;
            
            if (canvas == null) continue;
            
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  - Render Mode: {canvas.renderMode}");
            Debug.Log($"  - Size: {rect.rect.width}x{rect.rect.height}");
            Debug.Log($"  - Position: {rect.anchoredPosition}");
            Debug.Log($"  - Child Elements: {childElements[canvas].Count}");
            
            foreach (RectTransform child in childElements[canvas])
            {
                Debug.Log($"    -> {child.name}: {child.anchoredPosition}, Size: {child.rect.width}x{child.rect.height}");
            }
        }
    }
    
    // Helper method to align canvases relative to each other
    public void AlignCanvases(Canvas primary, Canvas secondary, CanvasAlignment alignment, Vector2 offset)
    {
        if (primary == null || secondary == null) return;
        
        RectTransform primaryRect = primary.GetComponent<RectTransform>();
        RectTransform secondaryRect = secondary.GetComponent<RectTransform>();
        
        Vector2 newPosition = secondaryRect.anchoredPosition;
        
        switch (alignment)
        {
            case CanvasAlignment.Top:
                newPosition.y = primaryRect.anchoredPosition.y + primaryRect.rect.height + offset.y;
                break;
            case CanvasAlignment.Bottom:
                newPosition.y = primaryRect.anchoredPosition.y - secondaryRect.rect.height - offset.y;
                break;
            case CanvasAlignment.Left:
                newPosition.x = primaryRect.anchoredPosition.x - secondaryRect.rect.width - offset.x;
                break;
            case CanvasAlignment.Right:
                newPosition.x = primaryRect.anchoredPosition.x + primaryRect.rect.width + offset.x;
                break;
            case CanvasAlignment.Center:
                newPosition.x = primaryRect.anchoredPosition.x + (primaryRect.rect.width - secondaryRect.rect.width) / 2;
                newPosition.y = primaryRect.anchoredPosition.y + (primaryRect.rect.height - secondaryRect.rect.height) / 2;
                break;
        }
        
        secondaryRect.anchoredPosition = newPosition;
    }
    
    // Visual debugging in the Scene view
    void OnDrawGizmos()
    {
        if (!enableVisualDebugging || !Application.isPlaying) return;
        
        foreach (var pair in canvasRects)
        {
            if (pair.Key == null) continue;
            
            RectTransform rect = pair.Value;
            Vector3[] worldCorners = new Vector3[4];
            rect.GetWorldCorners(worldCorners);
            
            // Draw canvas bounds
            Debug.DrawLine(worldCorners[0], worldCorners[1], canvasBoundsColor);
            Debug.DrawLine(worldCorners[1], worldCorners[2], canvasBoundsColor);
            Debug.DrawLine(worldCorners[2], worldCorners[3], canvasBoundsColor);
            Debug.DrawLine(worldCorners[3], worldCorners[0], canvasBoundsColor);
            
            // Draw child elements
            foreach (RectTransform child in childElements[pair.Key])
            {
                if (child == null) continue;
                
                Vector3[] childCorners = new Vector3[4];
                child.GetWorldCorners(childCorners);
                
                Debug.DrawLine(childCorners[0], childCorners[1], Color.green);
                Debug.DrawLine(childCorners[1], childCorners[2], Color.green);
                Debug.DrawLine(childCorners[2], childCorners[3], Color.green);
                Debug.DrawLine(childCorners[3], childCorners[0], Color.green);
            }
        }
    }
    
    public enum CanvasAlignment
    {
        Top,
        Bottom,
        Left,
        Right,
        Center
    }
}
