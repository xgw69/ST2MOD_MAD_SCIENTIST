using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Relics;

/// <summary>科学宝典：回合开始修复 1；回合结束获得等同机器耐久的格挡。</summary>
public class TomeOfScience : MadScientistRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
        {
            Flash();
            await Durability.Repair(choiceContext, Owner.Creature, 1, null);
        }
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }

        var durability = Durability.Get(Owner.Creature);
        if (durability > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, durability, ValueProp.Unpowered, null);
        }
    }
}

/// <summary>觉醒宝典：回合开始修复 3；回合结束获得等同耐久的格挡，并对所有敌人造成等量伤害。</summary>
public class TomeOfAwakening : MadScientistRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
        {
            Flash();
            await Durability.Repair(choiceContext, Owner.Creature, 3, null);
        }
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }

        var durability = Durability.Get(Owner.Creature);
        if (durability <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, durability, ValueProp.Unpowered, null);
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies.ToList())
        {
            await CreatureCmd.Damage(choiceContext, enemy, durability, ValueProp.Move, null, null);
        }
    }
}
