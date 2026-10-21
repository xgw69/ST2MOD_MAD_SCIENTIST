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
        if (sources.Count < 2)
        {
            return null;
        }

        var fused = new FusedCard(sources);
        foreach (var source in sources)
        {
            await CardPileCmd.RemoveFromCombat(source, true);
        }

        await CardPileCmd.AddGeneratedCardToCombat(fused, PileType.Hand, player);
        return fused;
    }

    /// <summary>Let the player pick cards from their hand and fuse them.</summary>
    public static async Task<FusedCard?> SelectAndFuse(PlayerChoiceContext ctx, Player player, CardModel source, int count)
    {
        var prefs = new CardSelectorPrefs(new LocString("card_selection", "MADSCIENTIST-FUSE_PROMPT"), count, count)
        {
            Cancelable = true
        };

        var selected = (await MultiPileCardSelect.Select(ctx, player, prefs, c => c != source, PileType.Hand)).ToList();
        if (selected.Count < count)
        {
            return null;
        }

        return await Fuse(ctx, player, selected);
    }
}
