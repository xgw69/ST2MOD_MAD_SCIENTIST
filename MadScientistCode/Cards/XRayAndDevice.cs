using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Cards;

public class XRay : MadScientistCard
{
    public XRay() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        var hits = IsUpgraded ? x + 1 : x;
        if (hits <= 0)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        var lockTurns = IsUpgraded ? x - 1 : x;
        if (lockTurns > 0)
        {
            await PowerCmd.Apply<MadScientist.MadScientistCode.Powers.BlockLockPower>(
                choiceContext, Owner.Creature, lockTurns + 1, Owner.Creature, this);
        }
    }
}

public class BlastingDevice : MadScientistCard
{
    public BlastingDevice() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        var count = IsUpgraded ? x + 1 : x;
        if (count > 0)
        {
            var cards = Enumerable.Range(0, count)
                .Select(_ => Owner.Creature.CombatState!.CreateCard<Blast>(Owner))
                .ToList();
            await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
        }

        // The discount is a separate effect, including when X creates no cards.
        await PowerCmd.Apply<MadScientist.MadScientistCode.Powers.BlastFreePower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
