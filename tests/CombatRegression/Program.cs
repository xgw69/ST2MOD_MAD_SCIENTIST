using HarmonyLib;
using MadScientist.MadScientistCode.Cards;
using MadScientist.MadScientistCode.Cards.Fusion;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MadScientist.MadScientistCode.Core;
using MadScientist.MadScientistCode.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

new Harmony("madscientist.regression").PatchAll();
int passed = 0, failed = 0;
void Check(bool condition, string name) { if (!condition) { failed++; Console.WriteLine("FAIL " + name); return; } passed++; Console.WriteLine("PASS " + name); }
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
var fresh = NewPlayer();
await Durability.Repair(choice, fresh.Creature, 25, null);
Check(Durability.Get(fresh.Creature) == 20, "initial repair is capped at 20");
fresh.Creature.GetPower<MachineDurabilityPower>()!.SetAmount(19);
await Durability.Repair(choice, fresh.Creature, 5, null);
Check(Durability.Get(fresh.Creature) == 20, "subsequent repair is capped at 20");
await Durability.Repair(choice, fresh.Creature, 0, null);
Check(Durability.Get(fresh.Creature) == 20, "zero repair leaves full durability unchanged");
Check(await Durability.Consume(choice, fresh.Creature, 20, null) && Durability.Get(fresh.Creature) == 0, "exact consumption does not break the machine");
var hp = fresh.Creature.CurrentHp;
Check(!await Durability.Consume(choice, fresh.Creature, 3, null) && fresh.Creature.CurrentHp == hp - 6 && Durability.IsBroken(fresh.Creature), "overdraft deals double damage and breaks the machine");
await Durability.Repair(choice, fresh.Creature, 20, null);
Check(Durability.Get(fresh.Creature) == 0 && !await Durability.Consume(choice, fresh.Creature, 1, null) && fresh.Creature.CurrentHp == hp - 6, "broken machine cannot repair or explode twice");
try { Check(await Durability.Consume(choice, NewPlayer().Creature, 0, null), "zero consumption without a durability power is safe"); }
catch (NullReferenceException) { Check(false, "zero consumption without a durability power is safe"); }
var freePower = await PowerCmd.Apply<BlastFreePower>(choice, owner.Creature, 1, owner.Creature, null);
Check(freePower.TryModifyEnergyCostInCombat(Card<Blast>(owner), 3, out var ownCost) && ownCost == 0, "Blast discount applies to its owner");
Check(!freePower.TryModifyEnergyCostInCombat(Card<Blast>(other), 3, out var teamCost) && teamCost == 3, "Blast discount does not change teammate costs");
await freePower.BeforeSideTurnEnd(choice, CombatSide.Enemy, [other.Creature]);
Check(owner.Creature.HasPower<BlastFreePower>(), "other side does not expire Blast discount");
await freePower.BeforeSideTurnEnd(choice, CombatSide.Player, [owner.Creature]);
Check(!owner.Creature.HasPower<BlastFreePower>(), "owner turn end expires Blast discount without mutating cards");
var blockLock = await PowerCmd.Apply<BlockLockPower>(choice, owner.Creature, 2, owner.Creature, null);
decimal BlockAfterHooks(CardModel? source, Creature target, bool dexFirst)
{
    decimal block = 6;
    if (dexFirst) block += 3;
    block += blockLock.ModifyBlockAdditive(target, block, ValueProp.Move, source, null);
    if (!dexFirst) block += 3;
    return block * blockLock.ModifyBlockMultiplicative(target, block, ValueProp.Move, source, null);
}
Check(BlockAfterHooks(a, owner.Creature, false) == 0 && BlockAfterHooks(a, owner.Creature, true) == 0, "block lock cancels card block regardless of additive modifier order");
Check(BlockAfterHooks(null, owner.Creature, false) == 9 && BlockAfterHooks(a, other.Creature, false) == 9, "block lock preserves non-card and teammate block");
await blockLock.BeforeSideTurnStart(choice, CombatSide.Enemy, [other.Creature], (CombatState)owner.Creature.CombatState!);
Check(blockLock.Amount == 2, "block lock does not tick on enemy turn");
await blockLock.BeforeSideTurnStart(choice, CombatSide.Player, [owner.Creature], (CombatState)owner.Creature.CombatState!);
Check(blockLock.Amount == 1, "block lock decrements once at owner turn start");
await blockLock.BeforeSideTurnStart(choice, CombatSide.Player, [owner.Creature], (CombatState)owner.Creature.CombatState!);
Check(!owner.Creature.HasPower<BlockLockPower>(), "block lock expires at the following owner turn start");
foreach (var loss in new[] { 5, 3 })
{
    var soulPlayer = NewPlayer();
    var soul = await PowerCmd.Apply<SoulResearchPower>(choice, soulPlayer.Creature, loss, soulPlayer.Creature, null);
    soulPlayer.Creature.CurrentHp = 0;
    Check(!soul.ShouldDieLate(soulPlayer.Creature), $"Soul Research prevents first death (loss {loss})");
    Check(soul.SmartDescription.Key.EndsWith(".smartDescription"), "unused Soul Research selects its active smart description");
    await soul.AfterPreventingDeath(soulPlayer.Creature);
    Check(soulPlayer.Creature.MaxHp == 60 - loss && soulPlayer.Creature.CurrentHp == (int)((60 - loss) / 2m), $"Soul Research reduces max HP and heals to half (loss {loss})");
    var reapplied = await PowerCmd.Apply<SoulResearchPower>(choice, soulPlayer.Creature, loss, soulPlayer.Creature, null);
    Check(reapplied.ShouldDieLate(soulPlayer.Creature), $"Soul Research cannot revive again after reapplication (loss {loss})");
    Check(reapplied.SmartDescription.Key == "MADSCIENTIST-SOUL_RESEARCH_POWER.spent"
        && reapplied.Description.Key == reapplied.SmartDescription.Key, "spent Soul Research keeps both tooltip paths spent after reapplication");
    Check(soul.ShouldDieLate(other.Creature), "Soul Research does not prevent teammate death");
}
var researchPlayer = NewPlayer();
var research = Card<ScientificResearch>(researchPlayer); research.InPile = true;
await research.AfterCardDrawnEarly(choice, research, true);
Check(researchPlayer.Hand.Count == 1, "Scientific Research copies on turn-start hand draw");
researchPlayer.Hand.Clear();
await research.AfterCardDrawnEarly(choice, research, false);
Check(researchPlayer.Hand.Count == 1, "Scientific Research copies on effect draw");
researchPlayer.Hand.Clear();
await research.AfterCardDrawnEarly(choice, a, false);
Check(researchPlayer.Hand.Count == 0, "Scientific Research ignores another card being drawn");
await research.Play(choice, Play(research));
Check(CreatureCmd.LastBlock == 6 && Durability.Get(researchPlayer.Creature) == 1, "Scientific Research plays for 6 block and 1 repair");
research.Upgrade();
await research.Play(choice, Play(research));
Check(CreatureCmd.LastBlock == 7 && Durability.Get(researchPlayer.Creature) == 3, "upgraded Scientific Research plays for 7 block and 2 repair");
await research.AfterCardDrawnEarly(choice, research, false);
Check(researchPlayer.Hand.Single().IsUpgraded, "Scientific Research copy preserves upgrade");
foreach (var upgrade in new[] { false, true })
foreach (var energy in new[] { 0, 1, 3 })
{
    var xp = NewPlayer();
    var xr = Card<XRay>(xp); xr.InPile = true; xr.EnergyCost.CapturedXValue = energy;
    if (upgrade) xr.Upgrade();
    AttackCommand.LastHits = -1;
    await xr.Play(choice, Play(xr));
    var expectedHits = energy + (upgrade ? 1 : 0);
    Check(AttackCommand.LastHits == (expectedHits == 0 ? -1 : expectedHits), $"X-Ray X={energy}, upgrade={upgrade}: hit count");
    var lockedTurns = upgrade ? energy - 1 : energy;
    Check((xp.Creature.GetPower<BlockLockPower>()?.Amount ?? 0) == (lockedTurns > 0 ? lockedTurns + 1 : 0), $"X-Ray X={energy}, upgrade={upgrade}: block-lock duration");
    var bd = Card<BlastingDevice>(xp); bd.EnergyCost.CapturedXValue = energy;
    if (upgrade) bd.Upgrade();
    await bd.Play(choice, Play(bd));
    Check(xp.Hand.Count == expectedHits && xp.Hand.All(c => c is Blast && c.Owner == xp), $"Blasting Device X={energy}, upgrade={upgrade}: generated cards");
    Check(xp.Creature.HasPower<BlastFreePower>(), $"Blasting Device X={energy}, upgrade={upgrade}: discount also applies to existing Blasts");
}
Console.WriteLine($"{passed} passed, {failed} failed (contract doubles; game integration still required).");
Environment.ExitCode = failed == 0 ? 0 : 1;

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
