using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MadScientist.MadScientistCode.Cards;

public class StrengthResearch : MadScientistCard
{
    public StrengthResearch() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(1m);
}

public class DexterityResearch : MadScientistCard
{
    public DexterityResearch() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DexterityPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["DexterityPower"].UpgradeValueBy(1m);
}

public class ReflexResearch : MadScientistCard
{
    public ReflexResearch() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ThornsPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, DynamicVars["ThornsPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["ThornsPower"].UpgradeValueBy(1m);
}
