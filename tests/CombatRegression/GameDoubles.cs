// Contract doubles for isolated regression tests, NOT a game/reference assembly.
// Model shallow-cloning, strict Owner assignment, lazy keyword/cost initialization,
// and explicit combat registration are intentional: these exposed the original bugs.
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Entities.Cards
{
    public enum CardType { Skill, Attack, Power, Curse, Status }
    public enum CardRarity { Common, Uncommon, Rare, Basic }
    public enum TargetType { Self, AnyEnemy, AllEnemies }
    public enum CardKeyword { Unplayable, Exhaust, Retain }
    public enum PileType { None, Hand, Play, Draw }
    public enum CostModifiers { None, Local }
    public class CardPlay
    {
        public required CardModel Card { get; init; }
        public Creature? Target { get; init; }
    }
    public class CardEnergyCost(CardModel card, int cost, bool x)
    {
        public int Cost = cost;
        public int Canonical => cost;
        public bool CostsX => x;
        private int _captured;
        public int CapturedXValue { get => x ? _captured : throw new Exception("Not X"); set { if (!x) throw new Exception("Not X"); _captured = value; } }
        public int GetWithModifiers(CostModifiers modifiers) => Cost;
        public void SetCustomBaseCost(int value) => Cost = value;
        public void UpgradeBy(int value) => Cost += value;
        public CardEnergyCost Clone(CardModel owner) => new(owner, cost, x) { Cost = Cost, _captured = _captured };
    }
}
namespace MegaCrit.Sts2.Core.Entities.Players
{
    public class Creature { public ICombatState? CombatState; public bool IsDead; public bool IsAlive => !IsDead; }
    public class RunState { public object? CurrentRoom; }
    public class Player
    {
        public Creature Creature = new();
        public RunState RunState = new();
        public List<CardModel> Hand = new();
        public List<PotionModel> Potions = new();
    }
}
namespace MegaCrit.Sts2.Core.Combat
{
    public interface ICombatState { T CreateCard<T>(Player owner) where T : CardModel; }
    public class CombatState : ICombatState
    {
        public List<CardModel> Registered = new();
        public T CreateCard<T>(Player owner) where T : CardModel
        {
            var card = (T)ModelDb.Card<T>().ToMutable(); card.Owner = owner; Registered.Add(card); return card;
        }
    }
}
namespace MegaCrit.Sts2.Core.Models
{
    public class CardPoolModel { }
    public class CardModel(int cost, CardType type, CardRarity rarity, TargetType target)
    {
        private Player? _owner;
        public Player Owner { get => _owner!; set { if (_owner != null && value != null) throw new Exception("already has an owner"); _owner = value; } }
        public bool InPile;
        public ICombatState? CombatState { [MethodImpl(MethodImplOptions.NoInlining)] get => InPile ? _owner?.Creature.CombatState : null; }
        public bool IsUpgraded;
        public bool IsCanonical = true;
        public virtual CardType Type => type;
        public virtual TargetType TargetType => target;
        public virtual string Title => GetType().Name;
        public virtual string PortraitPath => Title;
        public virtual bool CanBeGeneratedByModifiers => true;
        public virtual CardPoolModel Pool => new();
        public int BaseReplayCount { get; set; }
        protected virtual bool HasEnergyCostX => false;
        protected virtual IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars => [];
        private MegaCrit.Sts2.Core.Localization.DynamicVars.VarSet? _vars;
        public MegaCrit.Sts2.Core.Localization.DynamicVars.VarSet DynamicVars => _vars ??= new(CanonicalVars);
        private CardEnergyCost? _energy;
        public CardEnergyCost EnergyCost => _energy ??= new(this, cost, HasEnergyCostX);
        private HashSet<CardKeyword>? _keywords;
        public virtual IEnumerable<CardKeyword> CanonicalKeywords => [];
        public IReadOnlySet<CardKeyword> Keywords => _keywords ??= CanonicalKeywords.ToHashSet();
        public void AddKeyword(CardKeyword keyword) { _ = Keywords; _keywords!.Add(keyword); }
        public void RemoveKeyword(CardKeyword keyword) { _ = Keywords; _keywords!.Remove(keyword); }
        protected virtual void DeepCloneFields() { _keywords = Keywords.ToHashSet(); _energy = _energy?.Clone(this); _vars = null; }
        public object ClonePreservingMutability() { var copy = (CardModel)MemberwiseClone(); copy.DeepCloneFields(); return copy; }
        public CardModel ToMutable() { var copy = (CardModel)ClonePreservingMutability(); copy.IsCanonical = false; return copy; }
        public SerializableCard ToSerializable() => new(GetType(), GetType().GetProperties().Where(p => p.IsDefined(typeof(SavedPropertyAttribute))).ToDictionary(p => p.Name, p => p.GetValue(this)!));
        public static CardModel FromSerializable(SerializableCard saved)
        {
            var card = (CardModel)Activator.CreateInstance(saved.Type)!; card.IsCanonical = false;
            foreach (var pair in saved.Values) saved.Type.GetProperty(pair.Key)!.SetValue(card, pair.Value);
            return card;
        }
        protected virtual void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description) { }
        public string GetDescriptionForPile(PileType pile) => Title;
        protected virtual Task OnPlay(PlayerChoiceContext choice, CardPlay play) => Task.CompletedTask;
        public Task Play(PlayerChoiceContext choice, CardPlay play) => OnPlay(choice, play);
        protected virtual void OnUpgrade() { }
        public void Upgrade() { IsUpgraded = true; OnUpgrade(); }
        public int ResolveEnergyXValue() => EnergyCost.CapturedXValue;
    }
    public class PotionModel(string id) { public string Id = id; public PotionModel ToMutable() => new(Id); }
    public static class ModelDb
    {
        private static readonly Dictionary<Type, CardModel> Cards = new();
        public static T Card<T>() where T : CardModel
        {
            if (!Cards.TryGetValue(typeof(T), out var card)) Cards[typeof(T)] = card = (CardModel)Activator.CreateInstance(typeof(T))!;
            return (T)card;
        }
        public static T CardPool<T>() where T : CardPoolModel, new() => new();
        public static T GetById<T>(string id) where T : PotionModel => (T)new PotionModel(id);
    }
}
namespace MegaCrit.Sts2.Core.Saves.Runs
{
    [AttributeUsage(AttributeTargets.Property)] public class SavedPropertyAttribute : Attribute { }
    public record SerializableCard(Type Type, Dictionary<string, object> Values);
}
namespace MegaCrit.Sts2.Core.Models.Events
{
    public static class TinkerTime { public enum RiderEffect { Violence, Sapping, Choking, Energized, Wisdom, Chaos, Expertise, Curious, Improvement } }
}
namespace MegaCrit.Sts2.Core.Models.Cards
{
    public class MadScience() : CardModel(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        [SavedProperty] public CardType TinkerTimeType { get; set; }
        [SavedProperty] public Events.TinkerTime.RiderEffect TinkerTimeRider { get; set; }
        public override CardType Type => TinkerTimeType;
    }
}
namespace MegaCrit.Sts2.Core.GameActions.Multiplayer { public class PlayerChoiceContext { } }
namespace MegaCrit.Sts2.Core.Entities.Potions { public enum Unused { } }
namespace MegaCrit.Sts2.Core.Localization
{
    public class LocString(string table, string key) { public void Add(string name, object value) { } }
}
namespace MegaCrit.Sts2.Core.CardSelection
{
    public class CardSelectorPrefs(MegaCrit.Sts2.Core.Localization.LocString loc, int min, int max) { public bool Cancelable; }
}
namespace MegaCrit.Sts2.Core.Localization.DynamicVars
{
    public class DynamicVar(string name, decimal value) { public string Name = name; public decimal BaseValue = value; }
    public class DamageVar(decimal value, MegaCrit.Sts2.Core.ValueProps.ValueProp prop) : DynamicVar("Damage", value) { }
    public class VarSet(IEnumerable<DynamicVar> vars) { public DynamicVar Damage => vars.Single(v => v.Name == "Damage"); }
}
namespace MegaCrit.Sts2.Core.ValueProps { public enum ValueProp { Move } }
namespace MegaCrit.Sts2.Core.Rooms
{
    public class CombatRoom
    {
        public List<(Player Owner, MegaCrit.Sts2.Core.Rewards.PotionReward Reward)> Rewards = new();
        public void AddExtraReward(Player player, MegaCrit.Sts2.Core.Rewards.PotionReward reward) => Rewards.Add((player, reward));
    }
}
namespace MegaCrit.Sts2.Core.Rewards { public record PotionReward(PotionModel Potion, Player Owner); }
namespace MegaCrit.Sts2.Core.Commands
{
    public static class CardSelectCmd
    {
        public static int Selection;
        public static Task<IEnumerable<CardModel>> FromSimpleGrid(PlayerChoiceContext c, List<CardModel> options, Player p, MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs prefs)
            => Task.FromResult<IEnumerable<CardModel>>(new[] { options[Selection] });
    }
    public static class CardPileCmd
    {
        public static Task AddGeneratedCardToCombat(CardModel card, PileType pile, Player owner)
        {
            if (!((CombatState)owner.Creature.CombatState!).Registered.Contains(card)) throw new Exception("not registered");
            owner.Hand.Add(card); card.InPile = true; return Task.CompletedTask;
        }
        public static async Task AddGeneratedCardsToCombat<T>(IEnumerable<T> cards, PileType pile, Player owner) where T : CardModel
        { foreach (var card in cards) await AddGeneratedCardToCombat(card, pile, owner); }
    }
    public static class DamageCmd { public static AttackCommand Attack(decimal damage) => new(); }
    public class AttackCommand
    {
        public static int LastHits;
        private int _hits = 1;
        public AttackCommand WithHitCount(int count) { _hits = count; return this; }
        public AttackCommand FromCard(CardModel card, CardPlay play) => this;
        public AttackCommand TargetingAllOpponents(ICombatState? state) { if (state == null) throw new Exception("missing combat"); return this; }
        public Task Execute(PlayerChoiceContext context) { LastHits = _hits; return Task.CompletedTask; }
    }
}
namespace MadScientist.MadScientistCode.Character { public class MadScientistCardPool : CardPoolModel { } }
namespace MadScientist.MadScientistCode.Cards
{
    public class MadScientistCard(int cost, CardType type, CardRarity rarity, TargetType target) : CardModel(cost, type, rarity, target)
    { internal Task ExecuteFusionEffect(PlayerChoiceContext c, CardPlay p) => OnPlay(c, p); }
    public class AttackModule() : MadScientistCard(-1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable]; }
    public class ForbiddenScience() : MadScientistCard(-1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable]; }
    public class Blast() : MadScientistCard(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
}
