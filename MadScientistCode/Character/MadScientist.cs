using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MadScientist.MadScientistCode.Extensions;
using MadScientist.MadScientistCode.Visuals;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MadScientist.MadScientistCode.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MadScientist.MadScientistCode.Character;

/// <summary>
/// 疯狂科学家 / The Mad Scientist.
/// A tinkerer who experiments on cards, potions and herself.
///
/// Model id: characters.MAD_SCIENTIST (derived from the class name).
/// Loc keys: MAD_SCIENTIST.title and friends in localization/*/characters.json.
/// </summary>
public class MadScientist : PlaceholderCharacterModel
{
    public const string CharacterId = "MadScientist";

    /// <summary>Acid green - the colour of the flask on her backpack.</summary>
    public static readonly Color Color = new("a8e05f");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<WrenchStrike>()
    ];

    // TODO: replace with the character's own starter relic once it is designed.
    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BurningBlood>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<MadScientistCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MadScientistRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MadScientistPotionPool>();

    // ------------------------------------------------------------------
    // Visuals
    // ------------------------------------------------------------------

    /// <summary>
    /// Combat figure. A static image cut from the vanilla Act 3 "Tinker Time"
    /// event illustration. Can be replaced by a Spine scene later on.
    /// </summary>
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        return MadScientistVisuals.Create();
    }

    /// <summary>Character select backdrop, painted from the Tinker Time event illustration.</summary>
    public override string CustomCharacterSelectBg => "res://MadScientist/scenes/char_select_bg_mad_scientist.tscn";

    /* PlaceholderCharacterModel reuses base game assets for everything that is
       not overridden below (rest site pose, merchant pose, energy counter, card
       trail, sounds...). The 2D character art is ours. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_mad_scientist.png".CharacterUiPath();
    public override string CustomIconOutlineTexturePath => "character_icon_mad_scientist_outline.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_mad_scientist.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_mad_scientist_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_mad_scientist.png".CharacterUiPath();
}
