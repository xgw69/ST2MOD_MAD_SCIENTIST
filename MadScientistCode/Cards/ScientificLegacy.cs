using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;

namespace MadScientist.MadScientistCode.Cards;

/// <summary>九种疯狂科学（三类型 x 三词条）。</summary>
public static class MadScienceFactory
{
    public static List<CardModel> CreateAll()
    {
        var list = new List<CardModel>();
        Add(list, CardType.Attack, TinkerTime.RiderEffect.Violence);
        Add(list, CardType.Attack, TinkerTime.RiderEffect.Sapping);
        Add(list, CardType.Attack, TinkerTime.RiderEffect.Choking);
        Add(list, CardType.Skill, TinkerTime.RiderEffect.Energized);
        Add(list, CardType.Skill, TinkerTime.RiderEffect.Wisdom);
        Add(list, CardType.Skill, TinkerTime.RiderEffect.Chaos);
        Add(list, CardType.Power, TinkerTime.RiderEffect.Expertise);
        Add(list, CardType.Power, TinkerTime.RiderEffect.Curious);
        Add(list, CardType.Power, TinkerTime.RiderEffect.Improvement);
        return list;
    }

    private static void Add(List<CardModel> list, CardType type, TinkerTime.RiderEffect rider)
    {
        var card = (MadScience)ModelDb.Card<MadScience>().ToMutable();
        card.TinkerTimeType = type;
        card.TinkerTimeRider = rider;
        list.Add(card);
    }
}

public class ScientificLegacy : MadScientistCard
{
    public ScientificLegacy() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var options = MadScienceFactory.CreateAll();
        var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner, true);
        if (chosen != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
