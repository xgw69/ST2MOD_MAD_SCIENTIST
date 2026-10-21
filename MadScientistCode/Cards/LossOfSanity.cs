using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MadScientist.MadScientistCode.Cards;

public class LossOfSanity : MadScientistCard
{
    public LossOfSanity() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, 99m, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, 99m, Owner.Creature, this);
        await PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature, 99m, Owner.Creature, this);

        var prefs = new CardSelectorPrefs(
            new LocString("card_selection", "MADSCIENTIST-REMOVE_PROMPT"), 1, 1)
        {
            Cancelable = true
        };

        var picked = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).FirstOrDefault();
        if (picked != null)
        {
            await CardPileCmd.RemoveFromDeck(picked);
        }
    }
}
