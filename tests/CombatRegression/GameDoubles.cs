// Contract doubles for isolated regression tests, NOT a game/reference assembly.
// Model shallow-cloning, strict Owner assignment, lazy keyword/cost initialization,
// and explicit combat registration are intentional: these exposed the original bugs.
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    public class RunState { public object? CurrentRoom; }
    public class Player
    {
        public Creature Creature = new();
        public RunState RunState = new();
        public List<CardModel> Hand = new();
        public List<PotionModel> Potions = new();
    }
}
namespace MegaCrit.Sts2.Core.Entities.Creatures
{
    public class Creature
    {
        public ICombatState? CombatState;
        public bool IsDead;
        public bool IsAlive => !IsDead;
        public decimal CurrentHp = 60, MaxHp = 60;
        public List<PowerModel> Powers = new();
        public T? GetPower<T>() where T : PowerModel => Powers.OfType<T>().SingleOrDefault();
        public bool HasPower<T>() where T : PowerModel => GetPower<T>() != null;
    }
}
namespace MegaCrit.Sts2.Core.Entities.Powers
{
    public enum PowerType { Buff, Debuff }
    public enum PowerStackType { Single, Counter }
}
namespace MegaCrit.Sts2.Core.Combat
{
    public enum CombatSide { Player, Enemy }
    public interface ICombatState { T CreateCard<T>(Player owner) where T : CardModel; CardModel CreateCard(CardModel canonical, Player owner); }
    public class CombatState : ICombatState
    {
        public List<CardModel> Registered = new();
        public CardModel CreateCard(CardModel canonical, Player owner)
        { var card = canonical.ToMutable(); card.Owner = owner; Registered.Add(card); return card; }
        public T CreateCard<T>(Player owner) where T : CardModel
        {
            var card = (T)ModelDb.Card<T>().ToMutable(); card.Owner = owner; Registered.Add(card); return card;
        }
    }
}
namespace MegaCrit.Sts2.Core.Models
{
    public abstract class PowerModel
    {
        public Creature Owner = null!;
        public int Amount;
        public abstract MegaCrit.Sts2.Core.Entities.Powers.PowerType Type { get; }
        public abstract MegaCrit.Sts2.Core.Entities.Powers.PowerStackType StackType { get; }
        public virtual bool AllowNegative => true;
        public virtual MegaCrit.Sts2.Core.Localization.LocString Description => new("powers", GetType().Name);
        protected virtual string SmartDescriptionLocKey => GetType().Name + ".smartDescription";
        public MegaCrit.Sts2.Core.Localization.LocString SmartDescription => new("powers", SmartDescriptionLocKey);
        public void SetAmount(int amount) => Amount = amount;
        public virtual bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost) { modifiedCost = originalCost; return false; }
        public virtual decimal ModifyBlockAdditive(Creature target, decimal block, MegaCrit.Sts2.Core.ValueProps.ValueProp props, CardModel? cardSource, CardPlay? cardPlay) => 0;
        public virtual decimal ModifyBlockMultiplicative(Creature target, decimal block, MegaCrit.Sts2.Core.ValueProps.ValueProp props, CardModel? cardSource, CardPlay? cardPlay) => 1;
        public virtual Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) => Task.CompletedTask;
        public virtual Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) => Task.CompletedTask;
        public virtual bool ShouldDieLate(Creature creature) => true;
        public virtual Task AfterPreventingDeath(Creature creature) => Task.CompletedTask;
    }
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
        public string Id => GetType().Name;
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
        public void UpgradeInternal() => Upgrade();
        public virtual Task AfterCardDrawnEarly(PlayerChoiceContext context, CardModel card, bool fromHandDraw) => Task.CompletedTask;
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
        public static T GetById<T>(string id)
            => typeof(T) == typeof(CardModel) ? (T)(object)Cards.Values.Single(c => c.Id == id) : (T)(object)new PotionModel(id);
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
namespace MegaCrit.Sts2.Core.Models.Powers { }
namespace MegaCrit.Sts2.Core.GameActions.Multiplayer { public class PlayerChoiceContext { } public class ThrowingPlayerChoiceContext : PlayerChoiceContext { } }
namespace MegaCrit.Sts2.Core.Entities.Potions { public enum Unused { } }
namespace MegaCrit.Sts2.Core.Localization
{
    public class LocString(string table, string key) { public string Key => key; public void Add(string name, object value) { } }
}
namespace MegaCrit.Sts2.Core.CardSelection
{
    public class CardSelectorPrefs(MegaCrit.Sts2.Core.Localization.LocString loc, int min, int max) { public bool Cancelable; }
}
namespace MegaCrit.Sts2.Core.Localization.DynamicVars
{
    public class DynamicVar(string name, decimal value) { public string Name = name; public decimal BaseValue = value; public void UpgradeValueBy(decimal value) => BaseValue += value; }
    public class DamageVar(decimal value, MegaCrit.Sts2.Core.ValueProps.ValueProp prop) : DynamicVar("Damage", value) { }
    public class BlockVar(decimal value, MegaCrit.Sts2.Core.ValueProps.ValueProp prop) : DynamicVar("Block", value) { }
    public class VarSet(IEnumerable<DynamicVar> vars)
    { public DynamicVar Damage => this["Damage"]; public DynamicVar Block => this["Block"]; public DynamicVar this[string name] => vars.Single(v => v.Name == name); }
}
namespace MegaCrit.Sts2.Core.ValueProps { [Flags] public enum ValueProp { Move = 1, Unblockable = 2, Unpowered = 4 } }
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
    public static class PowerCmd
    {
        public static Task<T> Apply<T>(PlayerChoiceContext ctx, Creature target, decimal amount, Creature applier, CardModel? source) where T : PowerModel, new()
        {
            var power = target.GetPower<T>();
            if (power == null) { power = new T { Owner = target, Amount = (int)amount }; target.Powers.Add(power); }
            else if (power.StackType == MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter) power.Amount += (int)amount;
            return Task.FromResult(power);
        }
        public static Task Remove(PowerModel power) { power.Owner.Powers.Remove(power); return Task.CompletedTask; }
        public static Task Decrement(PowerModel power) { power.Amount--; return Task.CompletedTask; }
    }
    public static class CreatureCmd
    {
        public static Task Damage(PlayerChoiceContext ctx, Creature target, decimal damage, MegaCrit.Sts2.Core.ValueProps.ValueProp props, CardModel? source, Creature? dealer)
        { target.CurrentHp -= damage; return Task.CompletedTask; }
        public static Task LoseMaxHp(PlayerChoiceContext ctx, Creature target, decimal amount, bool unpowered)
        { target.MaxHp -= amount; target.CurrentHp = Math.Min(target.CurrentHp, target.MaxHp); return Task.CompletedTask; }
        public static Task Heal(Creature target, decimal amount)
        { target.CurrentHp = Math.Min(target.MaxHp, target.CurrentHp + amount); return Task.CompletedTask; }
        public static decimal LastBlock;
        public static Task GainBlock(Creature target, MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar amount, CardPlay play)
        { LastBlock = amount.BaseValue; return Task.CompletedTask; }
    }
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
namespace MadScientist.MadScientistCode.Powers { public abstract class MadScientistPower : PowerModel { } }
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
