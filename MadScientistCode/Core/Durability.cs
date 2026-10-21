using System.Threading.Tasks;
using MadScientist.MadScientistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MadScientist.MadScientistCode.Core;

/// <summary>
/// Machine Durability / Repair.
///
/// Durability is a combat buff (0..20).  Cards may "consume" durability; if the
/// machine cannot pay, it explodes: 2 x overdraft damage, durability reset to 0,
/// and a MachineBrokenPower that disables durability cards for the rest of combat.
/// </summary>
public static class Durability
{
    public const int Max = 20;

    public static int Get(Creature creature) =>
        creature.GetPower<MachineDurabilityPower>()?.Amount ?? 0;

    public static bool IsBroken(Creature creature) => creature.HasPower<MachineBrokenPower>();

    public static async Task Repair(PlayerChoiceContext ctx, Creature creature, int amount, CardModel? source)
    {
        if (amount <= 0 || IsBroken(creature))
        {
            return;
        }

        var power = creature.GetPower<MachineDurabilityPower>();
        if (power == null)
        {
            await PowerCmd.Apply<MachineDurabilityPower>(ctx, creature, amount, creature, source);
        }
        else
        {
            power.SetAmount(System.Math.Min(Max, power.Amount + amount));
        }
    }

    /// <summary>Consume durability.  Returns false if the machine exploded.</summary>
    public static async Task<bool> Consume(PlayerChoiceContext ctx, Creature creature, int amount, CardModel? source)
    {
        if (IsBroken(creature))
        {
            return false;
        }

        var power = creature.GetPower<MachineDurabilityPower>();
        var current = power?.Amount ?? 0;

        if (current >= amount)
        {
            power!.SetAmount(current - amount);
            return true;
        }

        var overdraft = amount - current;
        if (power != null && current > 0)
        {
            power.SetAmount(0);
        }

        await PowerCmd.Apply<MachineBrokenPower>(ctx, creature, 1, creature, source);
        await CreatureCmd.Damage(
            ctx,
            creature,
            overdraft * 2,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            source!,
            null);
        return false;
    }
}
