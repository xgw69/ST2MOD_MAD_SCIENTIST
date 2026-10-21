using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>库存：回合结束时保留最多 N 张手牌。</summary>
public class InventoryPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player == null)
        {
            return Task.CompletedTask;
        }

        var hand = Owner.Player.PlayerCombatState?.Hand.Cards;
        if (hand == null)
        {
            return Task.CompletedTask;
        }

        foreach (var card in hand.Take(Amount))
        {
            CardCmd.ApplySingleTurnRetain(card);
        }

        return Task.CompletedTask;
    }
}

/// <summary>疯狂科研：每回合开始时获得一张随机罕见能力牌。</summary>
public class MadResearchPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player == null)
        {
            return;
        }

        var pool = new CardModel[]
        {
            ModelDb.Card<StrengthResearch>(),
            ModelDb.Card<DexterityResearch>(),
            ModelDb.Card<ReflexResearch>(),
            ModelDb.Card<SelfRepairResearch>(),
        };

        var pick = pool[System.Random.Shared.Next(pool.Length)].CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(pick, PileType.Hand, Owner.Player);
    }
}

/// <summary>灵魂研究：下一次致死时免死，回复到最大生命一半（并消耗自身）。</summary>
public class SoulResearchPower : MadScientistPower
{
    private bool _used;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner || _used)
        {
            return true;
        }

        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
        {
            return;
        }

        _used = true;
        var heal = System.Math.Max(1, (int)(creature.MaxHp / 2m));
        await CreatureCmd.Heal(creature, heal);
        await PowerCmd.Remove(this);
    }
}
