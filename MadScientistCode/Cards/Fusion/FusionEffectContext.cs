using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MadScientist.MadScientistCode.Cards.Fusion;

/// <summary>
/// Executes an effect inside the parent's play, without extra play hooks, energy payments,
/// replay or exhaust events. AsyncLocal isolates simultaneous players and nested fusions.
/// </summary>
internal sealed record FusionEffectContext(CardModel Source, ICombatState Combat)
{
    private static readonly AsyncLocal<FusionEffectContext?> Current = new();
    private static readonly MethodInfo VanillaOnPlay = AccessTools.Method(typeof(CardModel), "OnPlay");

    internal static async Task Execute(CardModel source, FusedCard parent, PlayerChoiceContext choice, CardPlay play)
    {
        var previous = Current.Value;
        var oldX = source.EnergyCost.CostsX ? source.EnergyCost.CapturedXValue : 0;
        Current.Value = new(source, parent.CombatState ?? parent.Owner.Creature.CombatState!);
        try
        {
            if (source.EnergyCost.CostsX)
                source.EnergyCost.CapturedXValue = parent.EnergyCost.CostsX ? parent.EnergyCost.CapturedXValue : 0;
            // Keep CardPlay.Card as the parent: effects belong to one logical card play.
            if (source is MadScientistCard custom) await custom.ExecuteFusionEffect(choice, play);
            else
            {
                // Vanilla and other mods do not expose an effect-only interface.
                try { await (Task)VanillaOnPlay.Invoke(source, new object[] { choice, play })!; }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    throw;
                }
            }
        }
        finally
        {
            if (source.EnergyCost.CostsX) source.EnergyCost.CapturedXValue = oldX;
            Current.Value = previous;
        }
    }

    // Materials stay out of piles and therefore do not receive independent combat hooks.
    // Only their effect's CombatState lookup is redirected while that effect is executing.
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.CombatState), MethodType.Getter)]
    private static class CombatContextPatch
    {
        private static void Postfix(CardModel __instance, ref ICombatState? __result)
        {
            if (Current.Value is { } context && ReferenceEquals(context.Source, __instance))
                __result = context.Combat;
        }
    }
}
