using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MadScientist.MadScientistCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MadScientist.MadScientistCode.Powers;

public class SoulResearchPower : MadScientistPower
{
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public bool Used { get; set; }

    public override MegaCrit.Sts2.Core.Localization.LocString Description => Used
        ? new("powers", "MADSCIENTIST-SOUL_RESEARCH_POWER.spent")
        : base.Description;

    // Mutable powers use SmartDescription in combat. Keep its spent state in
    // sync with Description now that the localized smart description exists.
    protected override string SmartDescriptionLocKey => Used
        ? "MADSCIENTIST-SOUL_RESEARCH_POWER.spent"
        : base.SmartDescriptionLocKey;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner || Used)
        {
            return true;
        }

        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner || Used)
        {
            return;
        }

        Used = true;
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, Amount, true);
        var heal = System.Math.Max(1, (int)(creature.MaxHp / 2m));
        await CreatureCmd.Heal(creature, heal);
        // Keep the spent, single-stack power until combat cleanup. Removing it lets
        // another copy of Soul Research install a fresh power and revive again.
    }
}
