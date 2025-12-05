// Assets/Scripts/Systems/SaveSystem.cs
using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData {
    public string sceneName;
    public int lives;
    public Vector3 lastCheckpointPos;
    public Quaternion lastCheckpointRot;
    public bool hasCheckpoint;


    // NEW — level progression + selection screen choices
    public int currentLevelIndex;     // which level is currently loaded
    public int maxUnlockedLevelIndex; // highest level unlocked so far (for Level Select gating)
    public string selectedCharacterId; // optional: from your selection screen
    public string selectedCarId;       // optional: from your selection screen 

    public string lastCharacterId;
     public string       lastCarId      ; 

}














public static class SaveSystem
{
    static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        var json = JsonUtility.ToJson(data);
        File.WriteAllText(Path, json);
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(Path)) return false;
        var json = File.ReadAllText(Path);
        data = JsonUtility.FromJson<SaveData>(json);
        return data != null;
    }

    public static void Delete()
    {
        if (File.Exists(Path)) File.Delete(Path);
    }
}

