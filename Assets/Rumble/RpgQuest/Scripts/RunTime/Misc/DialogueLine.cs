using UnityEngine;

 

[CreateAssetMenu(fileName = "NewDialogueLine", menuName = "RPG/Dialogue Line")]
public class DialogueLine : ScriptableObject
{
    public AudioClip audioClip;
    [TextArea(2, 5)]
    public string subtitleTemplate; // Use {0}, {1}, etc. for dynamic values

    // Optional: type or tag if you want to categorize (e.g., "Offer", "Progress", etc.)
    public string dialogueType; // e.g. "Offer", "Progress", "Complete"
}
