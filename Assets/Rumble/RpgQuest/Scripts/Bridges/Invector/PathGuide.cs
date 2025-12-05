using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

public class PathGuide : MonoBehaviour
{
    [Header("Path Settings")]
    public float pathWidth = 2.0f; // How far player can deviate
    public float checkInterval = 0.2f;
    public GameObject visualPathPrefab; // Optional path visuals
    
    [Header("Guidance Settings")]
    public float gentleNudgeStrength = 0.5f;
    public float maxAngleDeviation = 45f;
    
    private NavMeshPath path;
    private Vector3[] pathCorners;
    private int currentSegmentIndex;
    private float nextCheckTime;
    private Transform playerTransform;
    private List<GameObject> pathVisuals = new List<GameObject>();

    void Start()
    {
        playerTransform = transform;
        path = new NavMeshPath();
    }

    public void SetPath(Vector3 destination)
    {
        ClearVisuals();
        
        if (NavMesh.CalculatePath(playerTransform.position, destination, NavMesh.AllAreas, path))
        {
            pathCorners = new Vector3[path.corners.Length];
            Array.Copy(path.corners, pathCorners, path.corners.Length);
            currentSegmentIndex = 0;
            
            CreatePathVisuals(); // Optional visual guide
        }
    }

    void Update()
    {
        if (!HasPath || Time.time < nextCheckTime) return;
        
        nextCheckTime = Time.time + checkInterval;
        GuidePlayerAlongPath();
    }

    void GuidePlayerAlongPath()
    {
        // Get current path segment (line between two corners)
        Vector3 segmentStart = pathCorners[currentSegmentIndex];
        Vector3 segmentEnd = pathCorners[Mathf.Min(currentSegmentIndex + 1, pathCorners.Length - 1)];
        Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;
        
        // Calculate player's position relative to path
        Vector3 playerToSegment = playerTransform.position - segmentStart;
        float projection = Vector3.Dot(playerToSegment, segmentDirection);
        Vector3 closestPoint = segmentStart + segmentDirection * projection;
        
        // Check if player is off-path
        float distanceFromPath = Vector3.Distance(closestPoint, playerTransform.position);
        float progressAlongSegment = projection / Vector3.Distance(segmentStart, segmentEnd);
        
        // Progress to next segment if needed
        if (progressAlongSegment >= 1.0f && currentSegmentIndex < pathCorners.Length - 2)
        {
            currentSegmentIndex++;
            return;
        }
        
        // Apply gentle guidance when needed
        if (distanceFromPath > pathWidth * 0.5f)
        {
            Vector3 nudgeDirection = (closestPoint - playerTransform.position).normalized;
            float nudgeFactor = Mathf.Clamp01((distanceFromPath - pathWidth * 0.5f) / pathWidth);
            
            // This is where you'd integrate with your movement system
            ApplyGuidance(nudgeDirection, nudgeFactor);
        }
    }

    void ApplyGuidance(Vector3 direction, float strength)
    {
        // This should interface with your existing movement system
        // Here are three common approaches:
        
        // 1. For CharacterController:
        // characterController.Move(direction * strength * gentleNudgeStrength * Time.deltaTime);
        
        // 2. For Rigidbody:
        // rigidbody.AddForce(direction * strength * gentleNudgeStrength, ForceMode.VelocityChange);
        
        // 3. For custom movement:
        // Modify your movement input vector to incorporate this nudge
        // movementInput = Vector3.Lerp(movementInput, direction, strength * gentleNudgeStrength);
        
        Debug.Log($"Applying guidance: {direction} with strength {strength}");
    }

    void CreatePathVisuals()
    {
        if (visualPathPrefab == null) return;
        
        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            GameObject segment = Instantiate(visualPathPrefab, transform.parent);
            segment.transform.position = (pathCorners[i] + pathCorners[i + 1]) * 0.5f;
            segment.transform.LookAt(pathCorners[i + 1]);
            segment.transform.localScale = new Vector3(
                pathWidth,
                segment.transform.localScale.y,
                Vector3.Distance(pathCorners[i], pathCorners[i + 1])
            );
            pathVisuals.Add(segment);
        }
    }

    void ClearVisuals()
    {
        foreach (var visual in pathVisuals)
        {
            Destroy(visual);
        }
        pathVisuals.Clear();
    }

    public bool HasPath => pathCorners != null && pathCorners.Length > 1;
    public void ClearPath() => pathCorners = null;
}