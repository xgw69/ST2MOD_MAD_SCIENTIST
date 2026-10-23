using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>X射线：接下来若干回合无法从卡牌中获得格挡。</summary>
public class BlockLockPower : MadScientistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyBlockAdditive(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || cardSource == null)
        {
            return 0m;
        }

        return -block;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        return Amount <= 1 ? PowerCmd.Remove(this) : PowerCmd.Decrement(this);
    }
}
