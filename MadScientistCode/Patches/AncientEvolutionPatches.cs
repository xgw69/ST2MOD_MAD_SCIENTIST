using System.Linq;
using System.Reflection;
using HarmonyLib;
using MadScientist.MadScientistCode.Cards;
using MadScientist.MadScientistCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MadScientist.MadScientistCode.Patches;

/// <summary>OROBAS 池 3：科学宝典 -> 觉醒宝典。</summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
internal static class TouchOfOrobasPatch
{
    [HarmonyPostfix]
    private static void Postfix(RelicModel starterRelic, ref RelicModel __result)
    {
        if (starterRelic is TomeOfScience)
        {
            __result = (RelicModel)ModelDb.Relic<TomeOfAwakening>().ToMutable();
        }
    }
}

/// <summary>OROBAS 池 3：科学方法 -> 伪科学方法。</summary>
[HarmonyPatch]
internal static class ArchaicToothStarterPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(ArchaicTooth), "GetTranscendenceStarterCard");

    [HarmonyPostfix]
    private static void Postfix(Player player, ref CardModel? __result)
    {
        __result ??= player.Deck.Cards.FirstOrDefault(c => c is ScientificMethod);
    }
}

[HarmonyPatch]
internal static class ArchaicToothTransformPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(ArchaicTooth), "GetTranscendenceTransformedCard");

    [HarmonyPostfix]
    private static void Postfix(CardModel starterCard, ref CardModel __result)
    {
        if (starterCard is not ScientificMethod || starterCard.Owner == null)
        {
            return;
        }

        var card = starterCard.Owner.RunState.CreateCard<PseudoscientificMethod>(starterCard.Owner);
        if (starterCard.IsUpgraded)
        {
            CardCmd.Upgrade(card);
        }

        __result = card;
    }
}
