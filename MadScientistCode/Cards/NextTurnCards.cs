using System.Collections.Generic;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Core;
using MadScientist.MadScientistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Cards;

public class SpareSpikes : MadScientistCard
{
    public SpareSpikes() : base(1, CardType.Attack, CardRarity.Common, TargetType.Self)
    {
    }

    private int Damage => IsUpgraded ? 10 : 9;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SpareSpikesPower>(choiceContext, Owner.Creature, Damage, Owner.Creature, this);
    }
}

public class SpareReagent : MadScientistCard
{
    public SpareReagent() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self)
    {
    }

    private int Damage => IsUpgraded ? 10 : 9;

    private int Weak => IsUpgraded ? 2 : 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<SpareReagentPower>(choiceContext, Owner.Creature, Damage, Owner.Creature, this);
        if (power != null)
        {
            power.WeakAmount = Weak;
        }
    }
}

public class Plan : MadScientistCard
{
    public Plan() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<PlanPower>(choiceContext, Owner.Creature, 3m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class SelfRepairResearch : MadScientistCard
{
    public SelfRepairResearch() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Repair", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SelfRepairResearchPower>(choiceContext, Owner.Creature, DynamicVars["Repair"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Repair"].UpgradeValueBy(1m);
}
