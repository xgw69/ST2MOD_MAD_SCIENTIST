using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>变异：下回合开始时失去敏捷，然后移除。</summary>
public class MutationPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner, -Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}

/// <summary>灼热地面：接下来若干回合开始时对随机敌人造成伤害。</summary>
public class ScorchedGroundPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        var ctx = new ThrowingPlayerChoiceContext();
        var enemies = combatState.HittableEnemies.ToList();
        if (enemies.Count > 0)
        {
            var target = enemies[combatState.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
            await CreatureCmd.Damage(ctx, target, 8m, ValueProp.Move, null, null);
        }

        if (Amount <= 1)
        {
            await PowerCmd.Remove(this);
        }
        else
        {
            await PowerCmd.Decrement(this);
        }
    }
}

/// <summary>思考：下回合开始时获得格挡和能量，然后移除。</summary>
public class ThinkPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        var ctx = new ThrowingPlayerChoiceContext();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        if (Owner.Player != null)
        {
            await PlayerCmd.GainEnergy(2, Owner.Player);
        }
        await PowerCmd.Remove(this);
    }
}
