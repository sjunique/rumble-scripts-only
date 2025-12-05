using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // Instance-scoped event (works when subscribed to the live Instance)
    public event Action<Quest> QuestCompleted;

    // Global static mirror (always fires, even if Instance swaps)
    public static event Action<Quest> QuestCompletedGlobal;
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();
    //  public List<Quest> completedQuests = new();
    [Header("All quest assets (for reset)")]
    public Quest[] allQuests;  



    [System.Serializable]
    public class QuestStateSnapshot
    {
        public string[] activeQuests;
        public string[] completedQuests;
    }
    public QuestStateSnapshot GetSnapshot()
    {
        return new QuestStateSnapshot
        {
            activeQuests = activeQuests.Select(q => q.questName).ToArray(),
            completedQuests = completedQuests.Select(q => q.questName).ToArray()
        };
    }

    public void ApplySnapshot(QuestStateSnapshot snap, Quest[] allQuests)
    {
        activeQuests.Clear();
        completedQuests.Clear();

        foreach (var name in snap.activeQuests)
            activeQuests.Add(allQuests.FirstOrDefault(q => q.questName == name));
        foreach (var name in snap.completedQuests)
        {
            var q = allQuests.FirstOrDefault(qq => qq.questName == name);
            if (q != null)
            {
                q.isCompleted = true;
                completedQuests.Add(q);
            }
        }
    }

    /// <summary>
    /// Reset all quest state: flags on assets + runtime lists.
    /// Call this from Reset Game / New Game.
    /// </summary>
    public void ResetAllQuestsss()
    {
        // 1) Reset all quest assets we know about
        if (allQuests != null)
        {
            foreach (var q in allQuests)
            {
                if (q == null) continue;
                q.isCompleted = false;
            }
        }

        // 2) Also reset anything currently tracked in lists
        foreach (var q in activeQuests)
        {
            if (q == null) continue;
            q.isCompleted = false;
        }

        foreach (var q in completedQuests)
        {
            if (q == null) continue;
            q.isCompleted = false;
        }

        // 3) Clear runtime lists
        activeQuests.Clear();
        completedQuests.Clear();

        Debug.Log("[QM] All quest state reset (assets + lists).");
    }




 public void ResetAllQuests()
{
    // 1) Reset all quest assets listed in the inspector
    if (allQuests != null)
    {
        foreach (var q in allQuests)
        {
            if (q == null) continue;

            q.isCompleted = false;

            if (q.objectives != null)
            {
                foreach (var obj in q.objectives)
                {
                    if (obj == null) continue;
                    obj.currentAmount = 0;      // 👈 reset progress
                }
            }
        }
    }

    // 2) Also reset anything currently tracked in the runtime lists
    foreach (var q in activeQuests)
    {
        if (q == null) continue;

        q.isCompleted = false;
        if (q.objectives != null)
        {
            foreach (var obj in q.objectives)
            {
                if (obj == null) continue;
                obj.currentAmount = 0;
            }
        }
    }

    foreach (var q in completedQuests)
    {
        if (q == null) continue;

        q.isCompleted = false;
        if (q.objectives != null)
        {
            foreach (var obj in q.objectives)
            {
                if (obj == null) continue;
                obj.currentAmount = 0;
            }
        }
    }

    activeQuests.Clear();
    completedQuests.Clear();

    Debug.Log("[QM] All quest state reset (assets + objectives + lists).");
}


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[QM] Duplicate QuestManager found; destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasQuest(Quest quest) => activeQuests.Contains(quest);

    public void AddQuest(Quest quest)
    {
        if (!quest) return;
        if (!HasQuest(quest) && !quest.isCompleted)
        {
            activeQuests.Add(quest);
            Debug.Log($"[QM] Quest added: {quest.name}");
        }
        else Debug.Log($"[QM] Quest already active/completed: {quest.name}");
    }

    public void CompleteQuest(Quest quest)
    {
        if (!quest || quest.isCompleted) return;

        quest.isCompleted = true;
        activeQuests.Remove(quest);
        if (!completedQuests.Contains(quest)) completedQuests.Add(quest);

        Debug.Log($"Quest completed: {quest.name}");

        // fire both
        QuestCompleted?.Invoke(quest);
        QuestCompletedGlobal?.Invoke(quest);
    }
}
// Note: This script is designed to be used in a Unity project and should be placed in the appropriate folder structure as indicated in the context.
// It manages quests, allowing for adding and completing quests, and provides events for when a quest is completed.
// Ensure that the Quest class is defined elsewhere in your project for this to work correctly.