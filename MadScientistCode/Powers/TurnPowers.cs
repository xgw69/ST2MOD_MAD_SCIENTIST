using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>自修复研究：每回合开始时修复。</summary>
public class SelfRepairResearchPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        await Durability.Repair(new ThrowingPlayerChoiceContext(), Owner, Amount, null);
    }
}

/// <summary>备用尖刺：下回合开始时对所有敌人造成伤害，然后移除。</summary>
public class SpareSpikesPower : MadScientistPower
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
        foreach (var enemy in combatState.HittableEnemies.ToList())
        {
            await CreatureCmd.Damage(ctx, enemy, Amount, ValueProp.Move, null, null);
        }

        await PowerCmd.Remove(this);
    }
}

/// <summary>备用药剂：下回合开始时对所有敌人造成伤害并给予虚弱，然后移除。</summary>
public class SpareReagentPower : MadScientistPower
{
    public int WeakAmount { get; set; } = 1;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        var ctx = new ThrowingPlayerChoiceContext();
        foreach (var enemy in combatState.HittableEnemies.ToList())
        {
            await CreatureCmd.Damage(ctx, enemy, Amount, ValueProp.Move, null, null);
            await PowerCmd.Apply<WeakPower>(ctx, enemy, WeakAmount, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}

/// <summary>计划：下回合开始时修复，然后移除。</summary>
public class PlanPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        await Durability.Repair(new ThrowingPlayerChoiceContext(), Owner, Amount, null);
        await PowerCmd.Remove(this);
    }
}
