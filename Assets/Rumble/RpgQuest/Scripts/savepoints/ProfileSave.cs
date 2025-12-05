// Assets/Rumble/RpgQuest/savepoints/ProfileSave.cs
using System;
using UnityEngine;

[Serializable]
public class ProfileSave {
    public int version = 1;
    public string profileId = "default";
    public string lastPlayedScene = "";
    public int maxUnlockedIndex = 0;
    public string selectedCharacterId = "";
    public string selectedCarId = "";
}

