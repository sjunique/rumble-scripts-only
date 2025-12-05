// Assets/Rumble/RpgQuest/savepoints/SceneCheckpoint.cs
using System;
using UnityEngine;

[Serializable]
public class SceneCheckpointFile {
    public bool has = false;
    public Vector3 pos;
    public Quaternion rot = Quaternion.identity;
}

