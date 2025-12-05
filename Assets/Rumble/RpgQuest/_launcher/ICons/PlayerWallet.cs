// Assets/_Shop/PlayerWallet.cs
public static class PlayerWallet
{
    static UpgradeStateManager M => UpgradeStateManager.Instance;

    public static int Coins => M ? M.Points : 0;                 // points == coins
    public static bool CanAfford(int cost) => Coins >= cost;

    public static bool TrySpend(int cost)
    {
        if (!M || Coins < cost) return false;
        M.AddPoints(-cost);                                      // spend
        return true;
    }

    public static void Add(int amount) { if (M) M.AddPoints(amount); }
}

