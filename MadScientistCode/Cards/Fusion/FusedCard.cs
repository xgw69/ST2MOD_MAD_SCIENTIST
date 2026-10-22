using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MadScientist.MadScientistCode.Cards.Fusion;

/// <summary>A single play, with independent snapshots of its component effects.</summary>
public class FusedCard : CardModel
{
    private List<CardModel> _sources = new();

    public FusedCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public static FusedCard Create(IReadOnlyList<CardModel> sources, Player owner)
    {
        var leaves = Flatten(sources, new HashSet<CardModel>()).ToList();
        if (leaves.Count < 2 || leaves.Any(c => c.Type is CardType.Curse or CardType.Status))
            throw new ArgumentException("Fusion requires at least two non-curse, non-status cards.", nameof(sources));

        bool free = leaves.Any(c => c is ForbiddenScience);
        var combat = owner.Creature.CombatState ?? throw new InvalidOperationException("Fusion requires a combat.");
        FusedCard fused = !free && leaves.Any(c => c.EnergyCost.CostsX)
            ? combat.CreateCard<FusedXCard>(owner)
            : combat.CreateCard<FusedCard>(owner);
        fused._sources = leaves.Select(Snapshot).ToList();
        fused.FusionCost = free ? 0 : sources.Where(c => !c.EnergyCost.CostsX)
            .Sum(c => Math.Max(0, c.EnergyCost.GetWithModifiers(CostModifiers.Local)));
        fused.FusionKeywords = sources.SelectMany(c => c.Keywords)
            .Where(k => k != CardKeyword.Unplayable).Distinct().ToArray();
        // Existing Replay belongs to the entire result, not to each effect invocation.
        fused.FusionReplay = sources.Sum(c => c.BaseReplayCount);
        if (leaves.Count == 2 && leaves.Count(c => c is AttackModule) == 1
            && leaves.All(c => c.Type == CardType.Attack) && !sources.OfType<FusedCard>().Any())
            fused.FusionReplay++;
        return fused;
    }

    private static IEnumerable<CardModel> Flatten(IEnumerable<CardModel> sources, HashSet<CardModel> path)
    {
        foreach (var source in sources)
        {
            if (!path.Add(source)) throw new InvalidOperationException("Cyclic fusion materials.");
            if (source is FusedCard fused)
            {
                foreach (var leaf in Flatten(fused._sources, path)) yield return leaf;
            }
            else yield return source;
            path.Remove(source);
        }
    }

    private static CardModel Snapshot(CardModel card) => (CardModel)card.ClonePreservingMutability();

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _sources = _sources.Select(Snapshot).ToList();
    }

    // Include materials and derived metadata in saves and multiplayer serialization.
    [SavedProperty]
    public List<SerializableCard> FusionMaterials
    {
        get => _sources.Select(c => c.ToSerializable()).ToList();
        set => _sources = value.Select(CardModel.FromSerializable).ToList();
    }

    [SavedProperty]
    public int FusionCost
    {
        get => EnergyCost.GetWithModifiers(CostModifiers.None);
        set => EnergyCost.SetCustomBaseCost(value);
    }

    [SavedProperty]
    public CardKeyword[] FusionKeywords
    {
        get => Keywords.ToArray();
        set
        {
            foreach (var keyword in Keywords.ToArray()) RemoveKeyword(keyword);
            foreach (var keyword in value.Where(k => k != CardKeyword.Unplayable)) AddKeyword(keyword);
        }
    }

    [SavedProperty]
    public int FusionReplay { get => BaseReplayCount; set => BaseReplayCount = value; }

    public IReadOnlyList<CardModel> Sources => _sources.AsReadOnly();
    public override bool CanBeGeneratedByModifiers => false;
    public override CardPoolModel Pool => ModelDb.CardPool<MadScientist.MadScientistCode.Character.MadScientistCardPool>();
    public override string Title => _sources.Count == 0 ? base.Title : string.Join("+", _sources.Select(c => c.Title));
    public override string PortraitPath => _sources.Count > 0 ? _sources[0].PortraitPath : base.PortraitPath;
    public override CardType Type => _sources.Any(c => c.Type == CardType.Attack) ? CardType.Attack
        : _sources.Any(c => c.Type == CardType.Power) ? CardType.Power : CardType.Skill;
    // A single-target effect must still receive a target when mixed with an AoE.
    public override TargetType TargetType => _sources.Any(c => c.TargetType == TargetType.AnyEnemy) ? TargetType.AnyEnemy
        : _sources.Any(c => c.TargetType == TargetType.AllEnemies) ? TargetType.AllEnemies : TargetType.Self;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Effects", string.Join("\n", _sources.Select(c => c.Title + ": " + c.GetDescriptionForPile(PileType.None))));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var source in _sources)
        {
            if (Owner.Creature.IsDead) break;
            if (source.TargetType == TargetType.AnyEnemy && (cardPlay.Target == null || !cardPlay.Target.IsAlive)) continue;
            // Deserialized material snapshots are unowned; normal snapshots already belong to us.
            if (source.Owner == null) source.Owner = Owner;
            await FusionEffectContext.Execute(source, this, choiceContext, cardPlay);
        }
    }
}

/// <summary>X is fixed on the prototype because CardEnergyCost caches CostsX when cloned.</summary>
public sealed class FusedXCard : FusedCard
{
    protected override bool HasEnergyCostX => true;
}
