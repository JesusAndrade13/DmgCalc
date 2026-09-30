using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards; // for CardPreviewMode
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using DmgCalc.DmgCalcCode;

namespace DmgCalc.DmgCalcCode.Patches;

[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.RecalculateCardValues))]
public static class CombatStateCapturePatch
{
    // Dedupe: RecalculateCardValues fires multiple times for the same
    // logical hand state (draw animation, UI settle passes, etc.), not
    // once per actual change. Skip re-logging/re-rendering identical output.
    private static string? _lastOutput;

    static void Postfix(PlayerCombatState __instance)
    {
        var hand = __instance.Hand.Cards;
        int energy = __instance.Energy;

        var plainLines = new List<string>();
        var bbcodeLines = new List<string>();

        // Damage options grouped per enemy, since damage is target-dependent
        // and each enemy gets its own "best play" recommendation for now.
        var optionsByEnemy = new Dictionary<Creature, List<CardOption>>();

        for (int i = 0; i < hand.Count; i++)
        {
            var card = hand[i];

            // Skills/Powers (Defend, etc.) have no Damage var at all — skip them.
            if (!card.DynamicVars.ContainsKey("Damage")) continue;

            var enemies = card.CombatState?.Enemies;
            if (enemies is null) continue;

            var cost = card.EnergyCost.GetAmountToSpend();

            foreach (var enemy in enemies)
            {
                card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.None, enemy, runGlobalHooks: true);
                var damage = card.DynamicVars.Damage.PreviewValue;

                plainLines.Add($"[{i}] {card.Title} -> {enemy.Name}: {damage} dmg (cost {cost})");
                bbcodeLines.Add($"[b]{card.Title}[/b] -> {enemy.Name}: [color=orange]{damage} dmg[/color]");

                if (!optionsByEnemy.TryGetValue(enemy, out var list))
                {
                    list = new List<CardOption>();
                    optionsByEnemy[enemy] = list;
                }
                list.Add(new CardOption(i, card.Title, cost, damage));
            }
        }

        var plainOutput = string.Join("\n", plainLines);
        if (plainOutput == _lastOutput) return; // identical to last fire — skip
        _lastOutput = plainOutput;

        foreach (var line in plainLines) MainFile.Logger.Info(line);

        // Best-play recommendation per enemy, via the knapsack engine.
        var recBlocks = new List<string>();
        foreach (var (enemy, options) in optionsByEnemy)
        {
            var (chosen, total) = RecommendationEngine.RecommendBestPlay(options, energy);
            var names = chosen.Count > 0
                ? string.Join(", ", chosen.Select(c => $"[{c.HandIndex}] {c.Title}"))
                : "(nothing affordable)";
            recBlocks.Add($"[b]Best vs {enemy.Name}:[/b] {names} = [color=lime]{total} dmg[/color]");
            MainFile.Logger.Info($"Best vs {enemy.Name}: {names} = {total} dmg");
        }

        var bbcode = "[b]Hand Preview[/b]\n" + string.Join("\n", bbcodeLines);
        if (recBlocks.Count > 0)
        {
            bbcode += "\n\n" + string.Join("\n", recBlocks);
        }
        DamageOverlay.Instance?.SetText(bbcode);
    }
}