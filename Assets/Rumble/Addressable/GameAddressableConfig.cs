using UnityEngine;

[CreateAssetMenu(menuName = "Rumble/Config/Addressable Keys", fileName = "GameAddressableConfig")]
public class GameAddressableConfig : ScriptableObject
{
    [Header("Addressable scene keys (match Addressables keys)")]
    public string mainMenuKey = "MainMenu";          // Addressable key for MainMenu scene
    public string pauseUIKey  = "PauseUI";           // Addressable key for PauseUI scene (additive)
    
    [Header("Optional: you can keep keys equal to scene names")]
    public string[] gameplayLevelKeys = new string[] { "Level_0_Main", "Level_1_Enchanted" };
}