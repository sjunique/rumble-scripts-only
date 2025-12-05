using UnityEngine;
 

[CreateAssetMenu(fileName = "NewDialogueCollection", menuName = "RPG/Dialogue Collection")]
public class DialogueCollection : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}
