using HarmonyLib;
using MadScientist.MadScientistCode.Cards;
using MadScientist.MadScientistCode.Cards.Fusion;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

new Harmony("madscientist.regression").PatchAll();
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
Player NewPlayer() => new() { Creature = new() { CombatState = new CombatState() }, RunState = new() { CurrentRoom = new CombatRoom() } };
T Card<T>(Player p) where T : CardModel => p.Creature.CombatState!.CreateCard<T>(p);
CardPlay Play(CardModel card) => new() { Card = card, Target = new Creature() };
var choice = new PlayerChoiceContext();
var owner = NewPlayer();
var a = Card<Probe>(owner); var b = Card<AttackModule>(owner);
a.EnergyCost.UpgradeBy(-1);
var first = FusedCard.Create([a, b], owner);
Check(((CombatState)owner.Creature.CombatState!).Registered.Contains(first), "fusion registers with combat");
Check(first.FusionCost == 1, "upgraded cost plus unplayable material cannot discount the result");
Check(first.Type == CardType.Attack && first.TargetType == TargetType.AnyEnemy, "dynamic type and target");
Check(!first.Keywords.Contains(CardKeyword.Unplayable) && first.BaseReplayCount == 1, "component playable and Replay inherited");
var c = Card<AreaProbe>(owner); c.AddKeyword(CardKeyword.Exhaust);
var second = FusedCard.Create([c, Card<SkillProbe>(owner)], owner);
Check(first.Sources[0] is Probe && !first.Keywords.Contains(CardKeyword.Exhaust), "later fusion cannot mutate earlier materials/keywords");
Check(second.Keywords.Contains(CardKeyword.Exhaust), "keywords initialized after materials");
var nested = FusedCard.Create([first, second], owner);
Check(nested.Sources.Count == 4 && nested.Sources.All(s => s is not FusedCard), "nested fusion flattens in order");
Check(nested.TargetType == TargetType.AnyEnemy, "single plus area attack retains enemy selection");
var clone = (FusedCard)first.ClonePreservingMutability();
clone.Sources[0].EnergyCost.UpgradeBy(-1);
Check(first.Sources[0].EnergyCost.GetWithModifiers(CostModifiers.None) == 1, "cloned materials do not share mutable state");
var cyclic = (FusedCard)first.ClonePreservingMutability();
var sources = (List<CardModel>)typeof(FusedCard).GetField("_sources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cyclic)!;
sources.Add(cyclic);
try { FusedCard.Create([cyclic, a], owner); throw new Exception("cycle accepted"); } catch (InvalidOperationException) { Check(true, "cyclic materials rejected"); }
var x = Card<XProbe>(owner); x.EnergyCost.CapturedXValue = 99;
var xf = FusedCard.Create([x, a], owner); xf.EnergyCost.CapturedXValue = 3; xf.InPile = true;
Probe.Seen.Clear(); await xf.Play(choice, Play(xf));
Check(xf.EnergyCost.CostsX && Probe.Seen.SequenceEqual(new[] { "X:3", "Probe" }), "X captured from parent and ordinary effect executes once");
Check(ReferenceEquals(Probe.LastPlay!.Card, xf), "effects retain a single parent CardPlay");
Check(xf.Sources.All(s => s.CombatState == null), "effect combat context restored after execution");
Check(xf.Sources[0].EnergyCost.CapturedXValue == 99, "temporary X restored");
var zero = FusedCard.Create([x, Card<ForbiddenScience>(owner)], owner);
Check(!zero.EnergyCost.CostsX && zero.FusionCost == 0, "Forbidden Science takes precedence over X cost");
var otherComponents = FusedCard.Create([Card<ForbiddenScience>(owner), Card<ForbiddenScience>(owner)], owner);
Check(otherComponents.FusionCost == 0 && !otherComponents.Keywords.Contains(CardKeyword.Unplayable), "two unplayable materials remain playable");
var savedMetadata = (FusedCard)CardModel.FromSerializable(first.ToSerializable());
Check(savedMetadata.FusionCost == first.FusionCost && savedMetadata.FusionReplay == first.FusionReplay
    && savedMetadata.FusionKeywords.SequenceEqual(first.FusionKeywords), "serialized fusion preserves cost, keywords and Replay");
first.EnergyCost.SetCustomBaseCost(0);
var discounted = FusedCard.Create([first, c], owner);
Check(discounted.FusionCost == c.EnergyCost.GetWithModifiers(CostModifiers.Local), "re-fusion respects the current fused card cost");
var saved = CardModel.FromSerializable(xf.ToSerializable()); saved.Owner = owner; saved.InPile = true;
Check(saved is FusedXCard restored && restored.Sources.Count == 2 && restored.Type == CardType.Attack, "serialized fusion restores materials and X prototype");
var fail = FusedCard.Create([Card<ThrowProbe>(owner), a], owner); fail.InPile = true;
try { await fail.Play(choice, Play(fail)); } catch (InvalidOperationException) { }
Check(fail.Sources[0].CombatState == null, "combat context restored after exception");
var other = NewPlayer();
var one = FusedCard.Create([Card<AsyncProbe>(owner), a], owner); one.InPile = true;
var two = FusedCard.Create([Card<AsyncProbe>(other), Card<Probe>(other)], other); two.InPile = true;
await Task.WhenAll(one.Play(choice, Play(one)), two.Play(choice, Play(two)));
Check(AsyncProbe.ValidContexts == 2, "simultaneous players have isolated asynchronous contexts");
var legacy = Card<ScientificLegacy>(owner);
for (int i = 0; i < 9; i++) { CardSelectCmd.Selection = i; await legacy.Play(choice, Play(legacy)); }
Check(owner.Hand.Count == 9 && owner.Hand.Select(c => ((MegaCrit.Sts2.Core.Models.Cards.MadScience)c).TinkerTimeRider).Distinct().Count() == 9, "all nine choices create owned registered cards");
var refine = Card<Refine>(owner); owner.Potions.Add(new PotionModel("healing"));
await refine.Play(choice, Play(refine)); owner.Potions.Clear();
var room = (CombatRoom)owner.RunState.CurrentRoom!;
Check(room.Rewards.Count == 1 && room.Rewards[0].Reward.Potion.Id == "healing" && room.Rewards[0].Owner == owner, "reward records potion type and owner even after potion is consumed");
await refine.Play(choice, Play(refine));
Check(room.Rewards.Count == 1, "no potion produces no extra reward");
var ray = Card<XRay>(owner); ray.InPile = true; ray.Upgrade(); ray.EnergyCost.CapturedXValue = 0;
await ray.Play(choice, Play(ray));
Check(AttackCommand.LastHits == 1, "upgraded X-Ray at zero energy still hits once");
var device = Card<BlastingDevice>(owner); device.EnergyCost.CapturedXValue = 2;
await device.Play(choice, Play(device));
Check(owner.Hand.TakeLast(2).All(c => c is Blast), "Blasting Device creates registered playable cards");
Console.WriteLine($"{passed} regression checks passed (contract doubles; game integration still required).");

public class Probe() : CardModel(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public static List<string> Seen = new(); public static CardPlay? LastPlay;
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { if (CombatState == null) throw new Exception("lost combat context"); Seen.Add("Probe"); LastPlay = p; return Task.CompletedTask; }
}
public class AreaProbe() : CardModel(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
public class SkillProbe() : CardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
public class XProbe : Probe
{
    protected override bool HasEnergyCostX => true;
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { if (CombatState == null) throw new Exception("lost X combat context"); Seen.Add("X:" + ResolveEnergyXValue()); return Task.CompletedTask; }
}
public class ThrowProbe : Probe
{
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => throw new InvalidOperationException("expected effect failure");
}
public class AsyncProbe : Probe
{
    public static int ValidContexts;
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await Task.Yield(); if (CombatState != Owner.Creature.CombatState) throw new Exception("cross-player combat context"); Interlocked.Increment(ref ValidContexts); }
}
