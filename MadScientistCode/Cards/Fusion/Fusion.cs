using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MadScientist.MadScientistCode.Cards.Fusion;

public static class FusionHelper
{
    public static async Task<FusedCard?> Fuse(PlayerChoiceContext ctx, Player player, IReadOnlyList<CardModel> sources)
    {
        // Curses and statuses can never be fused (also guarded in the selection filter).
        sources = sources.Where(c => c.Type is not (CardType.Curse or CardType.Status)).ToList();
        if (sources.Count < 2)
        {
            return null;
        }

        var fused = FusedCard.Create(sources, player);
        foreach (var source in sources)
        {
            await CardPileCmd.RemoveFromCombat(source, true);
        }

        await CardPileCmd.AddGeneratedCardToCombat(fused, PileType.Hand, player);
        return fused;
    }

    /// <summary>Let the player pick cards from their hand and fuse them.</summary>
    public static Task<FusedCard?> SelectAndFuse(PlayerChoiceContext ctx, Player player, CardModel source, int count)
        => SelectAndFuse(ctx, player, source, count, count);

    public static async Task<FusedCard?> SelectAndFuse(PlayerChoiceContext ctx, Player player, CardModel source, int min, int max)
    {
        var prefs = new CardSelectorPrefs(new LocString("card_selection", "MADSCIENTIST-FUSE_PROMPT"), min, max)
        {
            Cancelable = true
        };

        var selected = (await MultiPileCardSelect.Select(ctx, player, prefs, c => c != source && c.Type is not (CardType.Curse or CardType.Status), PileType.Hand)).ToList();
        if (selected.Count < min)
        {
            return null;
        }

        return await Fuse(ctx, player, selected);
    }
}
