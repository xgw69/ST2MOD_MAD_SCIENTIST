using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MadScientist.MadScientistCode.Cards;

/// <summary>炼制：战斗奖励中追加一瓶当前最左侧的药水。</summary>
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

        if (Owner.RunState.CurrentRoom is MegaCrit.Sts2.Core.Rooms.CombatRoom room)
        {
            // Capture the model now; drinking/discarding it later does not change the reward.
            var clone = ModelDb.GetById<PotionModel>(potion.Id).ToMutable();
            room.AddExtraReward(Owner, new MegaCrit.Sts2.Core.Rewards.PotionReward(clone, Owner));
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
