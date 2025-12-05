using UnityEngine;

public class QuestRewardHooks : MonoBehaviour
{
    public void GrantShieldNow()
    {
        var link = PlayerCarLinker.Instance;
        var inv = link && link.player ? link.player.GetComponent<PlayerInventoryFlags>() : null;
        if (inv) inv.GrantShield(true);
    }

    public void GrantScubaNow()
    {
        var link = PlayerCarLinker.Instance;
        var inv = link && link.player ? link.player.GetComponent<PlayerInventoryFlags>() : null;
        if (inv) inv.GrantScuba();
    }
}

