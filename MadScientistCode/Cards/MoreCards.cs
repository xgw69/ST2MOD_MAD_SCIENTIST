using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Cards.Fusion;
using MadScientist.MadScientistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Cards;

public class Mutation : MadScientistCard
{
    public Mutation() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Loss", 2m), new BlockVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        await PowerCmd.Apply<MutationPower>(choiceContext, Owner.Creature, DynamicVars["Loss"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Loss"].UpgradeValueBy(-1m);
    }
}

public class ScorchedGround : MadScientistCard
{
    public ScorchedGround() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ScorchedGroundPower>(choiceContext, Owner.Creature, DynamicVars["Turns"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Turns"].UpgradeValueBy(1m);
    }
}

public class Think : MadScientistCard
{
    public Think() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ThinkPower>(choiceContext, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public class MechanicalAscension : MadScientistCard
{
    public MechanicalAscension() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Artifact", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ArtifactPower>(choiceContext, Owner.Creature, DynamicVars["Artifact"].BaseValue, Owner.Creature, this);
        await ExhaustAllPowers(choiceContext);
    }

    private async Task ExhaustAllPowers(PlayerChoiceContext choiceContext)
    {
        var state = Owner.PlayerCombatState;
        var powers = state.Hand.Cards.Concat(state.DrawPile.Cards).Concat(state.DiscardPile.Cards)
            .Where(c => c.Type == CardType.Power && c != this)
            .Distinct()
            .ToList();

        foreach (var card in powers)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Artifact"].UpgradeValueBy(1m);
    }
}

public class ReptilianEvolutionProject : MadScientistCard
{
    public ReptilianEvolutionProject() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Intangible", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature, DynamicVars["Intangible"].BaseValue, Owner.Creature, this);

        var state = Owner.PlayerCombatState;
        var powers = state.Hand.Cards.Concat(state.DrawPile.Cards).Concat(state.DiscardPile.Cards)
            .Where(c => c.Type == CardType.Power && c != this)
            .Distinct()
            .ToList();

        foreach (var card in powers)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Intangible"].UpgradeValueBy(1m);
    }
}

public class ForbiddenScience : MadScientistCard
{
    public ForbiddenScience() : base(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override int MaxUpgradeLevel => 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(15m)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}

public class ScientificResearch : MadScientistCard
{
    public ScientificResearch() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override async Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this || fromHandDraw)
        {
            return;
        }

        var copy = CombatState!.CreateCard(ModelDb.GetById<CardModel>(Id), Owner);
        if (IsUpgraded)
        {
            copy.UpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);

    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pool = MegaCrit.Sts2.Core.Models.ModelDb
            .CardPool<MadScientist.MadScientistCode.Character.MadScientistCardPool>()
            .AllCards
            .Where(c => c.Rarity == CardRarity.Common && c.Type == CardType.Attack)
            .OrderBy(c => c.Id.Entry)
            .ToList();
        if (pool.Count == 0)
        {
            return;
        }

        var canonical = pool[Owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Count)];
        var bonus = CombatState!.CreateCard(canonical, Owner);
        if (IsUpgraded)
        {
            bonus.UpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(bonus, PileType.Hand, Owner);
    }
}

public class Process : MadScientistCard
{
    public Process() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var prefs = new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(
            new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "MADSCIENTIST-EXHAUST_PROMPT"), 1, 1)
        {
            Cancelable = true
        };

        var picked = (await MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromHand(choiceContext, Owner, prefs, c => c != this, this)).FirstOrDefault();
        if (picked == null)
        {
            return;
        }

        await CardCmd.Exhaust(choiceContext, picked);
        var drawn = (await CardPileCmd.Draw(choiceContext, 1, Owner)).FirstOrDefault();
        if (drawn != null)
        {
            await FusionHelper.Fuse(choiceContext, Owner, new CardModel[] { cardPlay.Card, drawn });
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
