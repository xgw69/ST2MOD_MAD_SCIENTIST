using System.Collections.Generic;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Cards;

public class FullRepair : MadScientistCard
{
    public FullRepair() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    private int RepairAmount => DynamicVars["Repair"].IntValue;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Repair", 10m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Durability.Repair(choiceContext, Owner.Creature, RepairAmount, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Repair"].UpgradeValueBy(5m);
    }
}

public class SelfRepairPlan : MadScientistCard
{
    public SelfRepairPlan() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    private int RepairAmount => DynamicVars["Repair"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Repair", 2m), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await Durability.Repair(choiceContext, Owner.Creature, RepairAmount, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Repair"].UpgradeValueBy(1m);
    }
}

public class QuickRepair : MadScientistCard
{
    public QuickRepair() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await Durability.Repair(choiceContext, Owner.Creature, 1, this);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public class MachineDefense : MadScientistCard
{
    public MachineDefense() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(20m, ValueProp.Move)];

    protected override bool IsPlayable => Owner == null || !Durability.IsBroken(Owner.Creature);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await Durability.Consume(choiceContext, Owner.Creature, 5, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
}

public class MachineProduction : MadScientistCard
{
    public MachineProduction() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    private int Cost => DynamicVars["DurabilityCost"].IntValue;

    protected override bool IsPlayable => Owner == null || !Durability.IsBroken(Owner.Creature);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DurabilityCost", 10m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(4, Owner);
        await Durability.Consume(choiceContext, Owner.Creature, Cost, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DurabilityCost"].UpgradeValueBy(-2m);
    }
}

public class MachineControl : MadScientistCard
{
    public MachineControl() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    private int Cost => DynamicVars["DurabilityCost"].IntValue;

    protected override bool IsPlayable => Owner == null || !Durability.IsBroken(Owner.Creature);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DurabilityCost", 10m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemy, -3m, Owner.Creature, this);
        }
        await Durability.Consume(choiceContext, Owner.Creature, Cost, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DurabilityCost"].UpgradeValueBy(-2m);
    }
}

public class EnergyShield : MadScientistCard
{
    public EnergyShield() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}

public class ReactiveBoiler : MadScientistCard
{
    public ReactiveBoiler() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(23m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, 2m, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
