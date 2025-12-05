// CameraRole.cs
using UnityEngine;

public enum CameraRole { Gameplay, Menu, Location, Cutscene }

[DisallowMultipleComponent]
public sealed class CameraRoleTag : MonoBehaviour
{
    public CameraRole role = CameraRole.Gameplay;
    [Tooltip("If true, this camera is the scene's primary pick when this scene is active.")]
    public bool preferThisCamera = true;
}

