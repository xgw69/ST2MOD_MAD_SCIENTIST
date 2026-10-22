using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MadScientist.MadScientistCode;

/// <summary>
/// Startup self-check (a few lines in the log): confirms the character model is
/// registered, the packed art / scene resolve, and the generated combat
/// animations exist.  Runs on the main thread during ModelDb.Preload.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Preload))]
internal static class MadScientistDiagnostics
{
    private const string SelectBgScene = "res://MadScientist/scenes/char_select_bg_mad_scientist.tscn";

    private static readonly string[] Resources =
    [
        "res://MadScientist/images/character/combat.png",
        "res://MadScientist/images/charui/char_select_mad_scientist.png",
        "res://MadScientist/images/charui/character_icon_mad_scientist.png",
        "res://MadScientist/images/charui/map_marker_mad_scientist.png",
        "res://MadScientist/images/events/select_bg.png",
    ];

    [HarmonyPostfix]
    private static void Postfix()
    {
        var character = ModelDb.All.OfType<CharacterModel>()
            .FirstOrDefault(c => c.GetType() == typeof(Character.MadScientist));

        if (character == null)
        {
            MainFile.Logger.Error("[Diag] MadScientist character model was not registered!");
            return;
        }

        var missing = Resources.Where(p => !ResourceLoader.Exists(p)).ToList();
        var scene = ResourceLoader.Load<PackedScene>(SelectBgScene);

        MainFile.Logger.Info(
            $"[Diag] checking {character.Id} title='{character.Title.GetFormattedText()}' " +
            $"hp={character.StartingHp} missingAssets={missing.Count} " +
            $"selectBg={(scene != null ? "loaded" : "MISSING")}");

        foreach (var path in missing)
        {
            MainFile.Logger.Error($"[Diag] missing asset: {path}");
        }

        try
        {
            var visuals = ((Character.MadScientist)character).CreateCustomVisuals();
            if (visuals == null)
            {
                MainFile.Logger.Error("[Diag] CreateCustomVisuals returned null");
                return;
            }

            var sprite = visuals.GetNodeOrNull<Sprite2D>("Visuals/Artwork/Sprite");
            MainFile.Logger.Info(
                $"[Diag] combat visual scale={visuals.Scale} texture={(sprite?.Texture?.GetSize().ToString() ?? "null")}");
            var player = visuals.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            var names = player == null ? "<none>" : string.Join(",", player.GetAnimationList());
            MainFile.Logger.Info($"[Diag] combat animations: {names} autoplay={player?.Autoplay}");
            if (missing.Count == 0 && scene != null && sprite?.Texture != null && player != null)
            {
                foreach (var animationName in player.GetAnimationList())
                {
                    var animation = player.GetAnimation(animationName);
                    for (int i = 0; i < animation.GetTrackCount(); i++)
                    {
                        var path = animation.TrackGetPath(i).ToString().Split(':')[0];
                        if (!visuals.HasNode(path)) throw new System.InvalidOperationException($"Missing animation target: {path}");
                    }
                }
                MainFile.Logger.Info("[Diag] OK startup checks complete");
            }
            else MainFile.Logger.Error("[Diag] startup checks failed");
            visuals.QueueFree();
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error($"[Diag] CreateCustomVisuals failed: {e}");
        }
    }
}
