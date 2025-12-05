using UnityEngine;

public class RewardOnComplete : MonoBehaviour
{
    [SerializeField] private int pointsAward = 25;

    // Call this when your quest/level is completed
    public void Grant()
    {
        if (UpgradeStateManager.Instance == null) return;
        UpgradeStateManager.Instance.AddPoints(pointsAward);
        Debug.Log($"[Reward] Granted {pointsAward} points.");
    }
}
