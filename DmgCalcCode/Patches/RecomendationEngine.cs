namespace DmgCalc.DmgCalcCode;

public record CardOption(int HandIndex, string Title, int Cost, decimal Damage);

public static class RecommendationEngine
{
    /// <summary>
    /// Finds the subset of playable cards that maximizes total damage
    /// without exceeding available energy. Classic 0/1 knapsack:
    /// cost = energy, value = damage.
    /// </summary>
    public static (List<CardOption> Chosen, decimal TotalDamage) RecommendBestPlay(
        List<CardOption> options, int energyAvailable)
    {
        int n = options.Count;
        var dp = new decimal[n + 1, energyAvailable + 1];

        for (int i = 1; i <= n; i++)
        {
            var opt = options[i - 1];
            for (int e = 0; e <= energyAvailable; e++)
            {
                dp[i, e] = dp[i - 1, e]; // baseline: don't take this card
                if (opt.Cost <= e)
                {
                    var take = dp[i - 1, e - opt.Cost] + opt.Damage;
                    if (take > dp[i, e]) dp[i, e] = take;
                }
            }
        }

        // Backtrack to find which cards were actually chosen.
        var chosen = new List<CardOption>();
        int energy = energyAvailable;
        for (int i = n; i > 0; i--)
        {
            if (dp[i, energy] != dp[i - 1, energy])
            {
                var opt = options[i - 1];
                chosen.Add(opt);
                energy -= opt.Cost;
            }
        }
        chosen.Reverse();

        return (chosen, dp[n, energyAvailable]);
    }

    /// <summary>
    /// Approximate lethal check: total damage vs enemy's Block + HP.
    /// Approximate because it doesn't model block-piercing effects,
    /// thorns, or other reactive triggers — good enough for a v1 flag.
    /// </summary>
    public static bool IsLethal(decimal totalDamage, decimal enemyBlock, decimal enemyHp)
        => totalDamage >= enemyBlock + enemyHp;
}