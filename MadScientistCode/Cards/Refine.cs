using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MadScientist.MadScientistCode.Cards;

/// <summary>炼制：复制一瓶已有药水。</summary>
public class Refine : MadScientistCard
{
    public Refine() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var potion = Owner.Potions.FirstOrDefault();
        if (potion == null)
        {
            return Task.CompletedTask;
        }

        var canonical = ModelDb.GetById<PotionModel>(potion.Id);
        var clone = (PotionModel)canonical.ToMutable();
        typeof(Player)
            .GetMethod("AddPotionInternal", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(Owner, [clone]);

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
