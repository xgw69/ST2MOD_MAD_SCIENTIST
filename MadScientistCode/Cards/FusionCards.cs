using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Core;
using MadScientist.MadScientistCode.Cards.Fusion;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Cards;

public class GreatFusion : MadScientistCard
{
    public GreatFusion() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = Owner.PlayerCombatState.Hand.Cards
            .Where(c => c != this && c.Type is not (CardType.Curse or CardType.Status))
            .ToList();

        if (hand.Count == 0)
        {
            return;
        }

        var fused = await FusionHelper.Fuse(choiceContext, Owner, hand);
        if (fused != null)
        {
            await CardCmd.AutoPlay(choiceContext, fused, null);
        }
    }
}

public class Assemble : MadScientistCard
{
    public Assemble() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var state = Owner.PlayerCombatState;
        var cards = state.Hand.Cards.Concat(state.DrawPile.Cards)
            .Where(c => c != this && c.Keywords.Contains(CardKeyword.Unplayable))
            .Distinct()
            .ToList();

        if (cards.Count > 0)
        {
            await FusionHelper.Fuse(choiceContext, Owner, cards);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class FullyAutomatic : MadScientistCard
{
    public FullyAutomatic() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    private int RepairPerEnemy => IsUpgraded ? 2 : 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var durability = Durability.Get(Owner.Creature);
        if (durability <= 0)
        {
            return;
        }

        await Durability.Consume(choiceContext, Owner.Creature, durability, this);
        await DamageCmd.Attack(durability)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        var enemies = CombatState.HittableEnemies.Count();
        await Durability.Repair(choiceContext, Owner.Creature, enemies * RepairPerEnemy, this);
    }

    public override async Task AfterAutoPrePlayPhaseEnteredEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (Pile?.Type == PileType.Exhaust && player == Owner)
        {
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
    }
}
