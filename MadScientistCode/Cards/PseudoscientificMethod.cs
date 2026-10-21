using System.Collections.Generic;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Cards.Fusion;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MadScientist.MadScientistCode.Cards;

/// <summary>伪科学方法：科学方法进化后的先古版本。</summary>
public class PseudoscientificMethod : MadScientistCard
{
    public PseudoscientificMethod() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await FusionHelper.SelectAndFuse(choiceContext, Owner, this, 2, 4);
    }
}
