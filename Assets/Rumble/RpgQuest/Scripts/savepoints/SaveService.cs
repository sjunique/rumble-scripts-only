 // Assets/Rumble/RpgQuest/savepoints/SaveService.cs
using System.IO;
using UnityEngine;

public static class SaveService
{


  public static string GetProfilePath() => Prof;

    public static string GetScenePath(string scene) => ScenePath(scene);


    static ProfileSave _profile;
public static bool Verbose = true;

    static string Root   => Application.persistentDataPath;
    static string Prof   => Path.Combine(Root, "profile.json");
    static string Scenes => Path.Combine(Root, "scenes");

    static void EnsureDirs() { if (!Directory.Exists(Scenes)) Directory.CreateDirectory(Scenes); }









    public static ProfileSave LoadProfile()
    {
        if (Verbose) Debug.Log($"[SaveService] Profile path: {Prof}");
        if (_profile != null) return _profile;
        if (!File.Exists(Prof)) { _profile = new ProfileSave(); return _profile; }
        _profile = JsonUtility.FromJson<ProfileSave>(File.ReadAllText(Prof)) ?? new ProfileSave();
        return _profile;
    }

    public static void SaveProfile(ProfileSave p = null) {
        EnsureDirs();
        var data = p ?? _profile ?? new ProfileSave();
        File.WriteAllText(Prof, JsonUtility.ToJson(data, true));
    }
static string ScenePath(string scene)
{
    EnsureDirs();
    var path = Path.Combine(Scenes, $"scene_{scene}.json");

    // OK to log the *string* we just built — do not call ScenePath(...) here
#if UNITY_EDITOR
    if (Verbose) Debug.Log($"[SaveService] Scene path -> {path}");
#endif

    return path;
}

    public static bool TryLoadSceneCheckpoint(string scene, out SceneCheckpointFile cp) {

        var path = ScenePath(scene);
        bool exists = File.Exists(path);
        if (Verbose) Debug.Log($"[SaveService] TryLoadSceneCheckpoint({scene}) -> {(exists ? "found" : "none")}");
        if (exists) {
            cp = JsonUtility.FromJson<SceneCheckpointFile>(File.ReadAllText(path)) ?? new SceneCheckpointFile();
            return cp.has;
        }
        cp = null;
        return false;
    }

    public static void SaveSceneCheckpoint(string scene, Vector3 pos, Quaternion rot, bool has = true) {

        if (Verbose) Debug.Log($"[SaveService] SaveSceneCheckpoint -> {ScenePath(scene)}");
        var cp = new SceneCheckpointFile { has = has, pos = pos, rot = rot };
        File.WriteAllText(ScenePath(scene), JsonUtility.ToJson(cp, true));
        var prof = LoadProfile(); prof.lastPlayedScene = scene; SaveProfile(prof);
    }

    public static void ClearSceneCheckpoint(string scene)
    {
        var path = ScenePath(scene);
        if (File.Exists(path))
        {
            var cp = JsonUtility.FromJson<SceneCheckpointFile>(File.ReadAllText(path)) ?? new SceneCheckpointFile();
            cp.has = false;
            File.WriteAllText(path, JsonUtility.ToJson(cp, true));
        }
    }
    
// SaveService.cs
public static void EnsureSceneFile(string scene, Vector3 defaultPos, Quaternion defaultRot)
{
    var path = ScenePath(scene);
    if (File.Exists(path)) return;  // don't overwrite

    var cp = new SceneCheckpointFile { has = false, pos = defaultPos, rot = defaultRot };
    File.WriteAllText(path, JsonUtility.ToJson(cp, true));
    if (Verbose) Debug.Log($"[SaveService] EnsureSceneFile -> {path}");
}

  public static void DeleteProfile()
    {
        string path = GetProfilePath(); // whatever you already use internally
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveService] Deleted profile at {path}");
        }
    }
    public static string GetScenesFolderPath()
    {
        // derive folder from a canonical scene path
        var scenePath = GetScenePath("Level_0_Main");
        var dir = Path.GetDirectoryName(scenePath);
        return string.IsNullOrEmpty(dir) ? Scenes : dir;
    }




   public static void DeleteSceneCheckpoint(string sceneName)
    {
        string path = GetScenePath(sceneName); // same helper you use for TryLoadSceneCheckpoint
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveService] Deleted scene checkpoint for '{sceneName}' at {path}");
        }
    }


    public static void DeleteAllSceneCheckpoints()
    {
        // If you store them all in the same folder, you can iterate
        string dir = GetScenesFolderPath(); // or Path.GetDirectoryName(GetScenePath("Level_0_Main"));

        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "scene_*.json"))
        {
            File.Delete(file);
            Debug.Log($"[SaveService] Deleted checkpoint file {file}");
        }
    }

}
