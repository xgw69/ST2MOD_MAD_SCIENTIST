using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MadScientist.MadScientistCode.Cards;

public class Inventory : MadScientistCard
{
    public Inventory() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Retain", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InventoryPower>(choiceContext, Owner.Creature, DynamicVars["Retain"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Retain"].UpgradeValueBy(1m);
    }
}

public class MadResearch : MadScientistCard
{
    public MadResearch() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MadResearchPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}

public class SoulResearch : MadScientistCard
{
    public SoulResearch() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SoulResearchPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
