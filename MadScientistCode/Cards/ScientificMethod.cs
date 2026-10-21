using System.Collections.Generic;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Cards.Fusion;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MadScientist.MadScientistCode.Cards;

public class ScientificMethod : MadScientistCard
{
    public ScientificMethod() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await FusionHelper.SelectAndFuse(choiceContext, Owner, this, 2);
    }

    protected override void OnUpgrade()
    {
        // TODO: 升级去掉消耗（关键词是静态的，需要单独的升级后关键词处理）
    }
}
