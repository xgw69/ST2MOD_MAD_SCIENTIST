using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MadScientist.MadScientistCode.Visuals;

/// <summary>
/// Builds the combat creature out of a static texture and a code generated
/// AnimationPlayer, so the character does not need a Spine rig:
///
///   Idle   - gentle breathing / sway (loops)
///   Attack - quick lunge forward with a white flash
///   Cast   - small hop + green glow
///   Hurt   - shake + red tint
///   Die    - falls over and fades out
///
/// BaseLib forwards the game's animation triggers (Attack / Cast / Hit / Dead)
/// to the AnimationPlayer by name, so the names below matter.
/// </summary>
public static class MadScientistVisuals
{
    public const string TexturePath = "res://MadScientist/images/character/combat.png";

    /// <summary>In-combat display scale relative to the texture (1.0 = 700 px tall).</summary>
    public const float VisualScale = 0.4f;

    public static NCreatureVisuals Create()
    {
        var texture = ResourceLoader.Load<Texture2D>(TexturePath);
        var root = NodeFactory<NCreatureVisuals>.CreateFromResource(texture);

        var sprite = root.GetNode<Sprite2D>("Visuals");
        var height = texture.GetHeight();

        // Re-parent the sprite under a pivot at the feet so rotations (attack
        // lean, death fall) swing around the ground instead of the waist.
        root.RemoveChild(sprite);
        var pivot = new Node2D { Name = "Visuals" };
        root.AddChild(pivot);
        pivot.Owner = root;
        pivot.UniqueNameInOwner = true;
        // Base display scale lives on the pivot, not on the root: the game's
        // shrink/grow effects ScaleTo() the creature root, so keeping it at 1
        // makes those effects multiply our 0.4 instead of overwriting it.
        pivot.Scale = new Vector2(VisualScale, VisualScale);
        pivot.AddChild(sprite);
        sprite.Name = "Sprite";
        sprite.Position = new Vector2(0f, -height * 0.5f);

        var player = new AnimationPlayer { Name = "AnimationPlayer", RootNode = new NodePath("..") };
        root.AddChild(player);

        var library = new AnimationLibrary();
        Add(library, "Idle", BuildIdle(), "idle");
        Add(library, "Attack", BuildAttack(), "attack");
        Add(library, "Cast", BuildCast(), "cast");
        Add(library, "Hurt", BuildHurt(), "hurt");
        Add(library, "Die", BuildDie(), "die");
        player.AddAnimationLibrary("", library);

        player.Autoplay = "Idle";
        return root;
    }

    private static void Add(AnimationLibrary library, string name, Animation anim, string lowerAlias)
    {
        library.AddAnimation(name, anim);
        library.AddAnimation(lowerAlias, anim);
    }

    private static Animation NewAnimation(float length, bool loop)
    {
        return new Animation
        {
            Length = length,
            LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None,
        };
    }

    private static int AddTrack(Animation anim, string path)
    {
        var track = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(track, new NodePath(path));
        anim.TrackSetInterpolationType(track, Animation.InterpolationType.Cubic);
        return track;
    }

    private static Animation BuildIdle()
    {
        var anim = NewAnimation(2.8f, true);
        var pos = AddTrack(anim, "Visuals:position");
        anim.TrackInsertKey(pos, 0.0, Vector2.Zero);
        anim.TrackInsertKey(pos, 1.4, new Vector2(0f, -5f));
        anim.TrackInsertKey(pos, 2.8, Vector2.Zero);

        var rot = AddTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rot, 0.0, -0.010f);
        anim.TrackInsertKey(rot, 1.4, 0.012f);
        anim.TrackInsertKey(rot, 2.8, -0.010f);

        var scale = AddTrack(anim, "Sprite:scale");
        anim.TrackInsertKey(scale, 0.0, Vector2.One);
        anim.TrackInsertKey(scale, 1.4, new Vector2(1.004f, 0.996f));
        anim.TrackInsertKey(scale, 2.8, Vector2.One);
        return anim;
    }

    private static Animation BuildAttack()
    {
        var anim = NewAnimation(0.5f, false);
        var pos = AddTrack(anim, "Visuals:position");
        anim.TrackInsertKey(pos, 0.0, Vector2.Zero);
        anim.TrackInsertKey(pos, 0.09, new Vector2(-6f, 2f));
        anim.TrackInsertKey(pos, 0.18, new Vector2(46f, -8f));
        anim.TrackInsertKey(pos, 0.30, new Vector2(32f, -3f));
        anim.TrackInsertKey(pos, 0.50, Vector2.Zero);

        var rot = AddTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rot, 0.0, 0f);
        anim.TrackInsertKey(rot, 0.09, -0.03f);
        anim.TrackInsertKey(rot, 0.18, 0.075f);
        anim.TrackInsertKey(rot, 0.50, 0f);

        var scale = AddTrack(anim, "Sprite:scale");
        anim.TrackInsertKey(scale, 0.0, Vector2.One);
        anim.TrackInsertKey(scale, 0.09, new Vector2(0.97f, 1.04f));
        anim.TrackInsertKey(scale, 0.18, new Vector2(1.06f, 0.94f));
        anim.TrackInsertKey(scale, 0.50, Vector2.One);

        var mod = AddTrack(anim, "Sprite:modulate");
        anim.TrackInsertKey(mod, 0.0, Colors.White);
        anim.TrackInsertKey(mod, 0.16, new Color(1.9f, 1.85f, 1.6f));
        anim.TrackInsertKey(mod, 0.50, Colors.White);
        return anim;
    }

    private static Animation BuildCast()
    {
        var anim = NewAnimation(0.65f, false);
        var pos = AddTrack(anim, "Visuals:position");
        anim.TrackInsertKey(pos, 0.0, Vector2.Zero);
        anim.TrackInsertKey(pos, 0.15, new Vector2(0f, -14f));
        anim.TrackInsertKey(pos, 0.40, new Vector2(0f, -4f));
        anim.TrackInsertKey(pos, 0.65, Vector2.Zero);

        var scale = AddTrack(anim, "Sprite:scale");
        anim.TrackInsertKey(scale, 0.0, Vector2.One);
        anim.TrackInsertKey(scale, 0.15, new Vector2(1.05f, 1.05f));
        anim.TrackInsertKey(scale, 0.65, Vector2.One);

        var mod = AddTrack(anim, "Sprite:modulate");
        anim.TrackInsertKey(mod, 0.0, Colors.White);
        anim.TrackInsertKey(mod, 0.15, new Color(0.75f, 1.55f, 0.85f));
        anim.TrackInsertKey(mod, 0.65, Colors.White);
        return anim;
    }

    private static Animation BuildHurt()
    {
        var anim = NewAnimation(0.32f, false);
        var pos = AddTrack(anim, "Visuals:position");
        anim.TrackInsertKey(pos, 0.0, Vector2.Zero);
        anim.TrackInsertKey(pos, 0.06, new Vector2(-11f, 1f));
        anim.TrackInsertKey(pos, 0.14, new Vector2(7f, -1f));
        anim.TrackInsertKey(pos, 0.22, new Vector2(-4f, 0f));
        anim.TrackInsertKey(pos, 0.32, Vector2.Zero);

        var mod = AddTrack(anim, "Sprite:modulate");
        anim.TrackInsertKey(mod, 0.0, Colors.White);
        anim.TrackInsertKey(mod, 0.06, new Color(1.5f, 0.55f, 0.5f));
        anim.TrackInsertKey(mod, 0.32, Colors.White);
        return anim;
    }

    private static Animation BuildDie()
    {
        var anim = NewAnimation(1.5f, false);
        var rot = AddTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rot, 0.0, 0f);
        anim.TrackInsertKey(rot, 0.55, -0.35f);
        anim.TrackInsertKey(rot, 1.5, -1.45f);

        var pos = AddTrack(anim, "Visuals:position");
        anim.TrackInsertKey(pos, 0.0, Vector2.Zero);
        anim.TrackInsertKey(pos, 0.55, new Vector2(-6f, 0f));
        anim.TrackInsertKey(pos, 1.5, new Vector2(-26f, 0f));

        var mod = AddTrack(anim, "Sprite:modulate");
        anim.TrackInsertKey(mod, 0.0, Colors.White);
        anim.TrackInsertKey(mod, 0.7, new Color(0.7f, 0.7f, 0.75f, 1f));
        anim.TrackInsertKey(mod, 1.5, new Color(0.5f, 0.5f, 0.55f, 0.35f));
        return anim;
    }
}
