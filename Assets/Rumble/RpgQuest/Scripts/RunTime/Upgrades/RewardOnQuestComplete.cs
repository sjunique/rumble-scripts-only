using UnityEngine;
using System;
using System.Reflection;

public class RewardOnQuestComplete : MonoBehaviour
{
    [Header("Quest")]
    public Quest quest;

    [Header("Rewards")]
    [Tooltip("Points to add when the quest completes.")]
    public int pointsToAward = 25;

    [Tooltip("Optional: for each UpgradeDef listed here, attempt to buy ONE level on completion.")]
    public UpgradeDef[] upgradesToGrant;

    private bool _granted;

    // Reflection wiring to QuestManager.OnQuestCompleted(Quest)
    private object _qmInstance;
    private EventInfo _eventInfo;
    private Delegate _handler;

    void OnEnable()
    {
        TrySubscribeToQuestManagerEvent();
        // immediate refresh in case quest already completed before this enabled
        if (!_granted && quest && quest.isCompleted)
            TryGrantOnce();
    }

    void OnDisable()
    {
        TryUnsubscribeFromQuestManagerEvent();
    }

    void Update()
    {
        // Fallback polling only if not yet granted and we have a quest ref
        if (!_granted && quest && quest.isCompleted)
            TryGrantOnce();
    }

    // --- Grant logic ---
    private void TryGrantOnce()
    {
        if (_granted) return;
        var mgr = UpgradeStateManager.Instance;
        if (mgr == null) return;

        // Points
        if (pointsToAward != 0)
            mgr.AddPoints(pointsToAward);

        // Optional: auto-purchase one level for each listed upgrade (if affordable & not maxed)
        if (upgradesToGrant != null)
        {
            foreach (var def in upgradesToGrant)
            {
                if (!def) continue;
                var id = def.id;
                if (!mgr.IsMaxed(id) && mgr.CanAffordNext(id))
                    mgr.TryPurchase(id);
            }
        }

        _granted = true;
        Debug.Log($"[Reward] Granted quest rewards for '{(quest ? quest.name : "Unknown")}'. " +
                  $"Points +{pointsToAward}, Upgrades: {(upgradesToGrant != null ? upgradesToGrant.Length : 0)} (attempted).");
    }

    // --- QuestManager event wiring (reflection) ---
    private void TrySubscribeToQuestManagerEvent()
    {
        var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>(true);
        if (!qm) return;

        _qmInstance = qm;
        var type = qm.GetType();

        // Look for OnQuestCompleted event (public or non-public)
        _eventInfo = type.GetEvent("OnQuestCompleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (_eventInfo == null) return;

        try
        {
            // Expecting a handler compatible with: void Handler(Quest q)
            var handlerType = _eventInfo.EventHandlerType;
            var method = GetType().GetMethod(nameof(OnQuestCompleted), BindingFlags.Instance | BindingFlags.NonPublic);
            _handler = Delegate.CreateDelegate(handlerType, this, method, throwOnBindFailure: false);

            if (_handler != null)
                _eventInfo.AddEventHandler(qm, _handler);
            else
                Debug.LogWarning("[RewardOnQuestComplete] Found OnQuestCompleted but couldn't bind handler (signature mismatch?).");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RewardOnQuestComplete] Failed to subscribe to OnQuestCompleted: {ex.Message}");
            _handler = null;
            _eventInfo = null;
            _qmInstance = null;
        }
    }

    private void TryUnsubscribeFromQuestManagerEvent()
    {
        if (_qmInstance == null || _eventInfo == null || _handler == null) return;
        try
        {
            _eventInfo.RemoveEventHandler(_qmInstance, _handler);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RewardOnQuestComplete] Failed to unsubscribe: {ex.Message}");
        }
        finally
        {
            _qmInstance = null; _eventInfo = null; _handler = null;
        }
    }

    // Must match the expected event: OnQuestCompleted(Quest q)
    private void OnQuestCompleted(Quest q)
    {
        if (_granted) return;
        if (!quest || q == quest) // grant if this is the specific quest, or if no specific quest assigned
            TryGrantOnce();
    }
}
