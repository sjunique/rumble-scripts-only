using UnityEngine;
using System;

public class DialogueSpeaker : MonoBehaviour
{
    public DialogueCollection dialogueCollection;

    public void PlayDialogue(int lineIndex, params object[] args)
    {
        if (dialogueCollection == null ||
            dialogueCollection.dialogueLines == null ||
            lineIndex < 0 ||
            lineIndex >= dialogueCollection.dialogueLines.Length)
        {
            Debug.LogWarning($"[DialogueSpeaker] Invalid line index {lineIndex}", this);
            return;
        }

        DialogueLine line = dialogueCollection.dialogueLines[lineIndex];
        string raw = line.subtitleTemplate ?? string.Empty;
        string subtitle = raw;

        try
        {
            // Only bother formatting if there are args AND we see a '{'
            if (args != null && args.Length > 0 && raw.Contains("{"))
                subtitle = string.Format(raw, args);
        }
        catch (FormatException ex)
        {
            Debug.LogError($"[DialogueSpeaker] Format error on line {lineIndex}: \"{raw}\" " +
                           $"with {args?.Length ?? 0} args. Using raw text.\n{ex}", this);
            subtitle = raw;
        }

        if (line.audioClip != null)
            AudioSource.PlayClipAtPoint(line.audioClip, transform.position);

        SubtitleUI.Show(subtitle);
    }
}
