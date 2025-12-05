using UnityEngine;

 

public class CollectibleReward : MonoBehaviour
{
    [SerializeField] private int points = 10;  // set 10 or 5

    public void Grant()
    {
        UpgradeStateManager.Instance?.AddPoints(points);
        // play VFX/SFX etc., then disable
        gameObject.SetActive(false);
    }
}
