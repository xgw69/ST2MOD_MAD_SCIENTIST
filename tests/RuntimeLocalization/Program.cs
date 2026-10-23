// Managed assembly checks only: no game initialization, native Godot calls or save access.
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;

if (args.Length != 4)
    throw new ArgumentException("Usage: RuntimeLocalization <game-data-dir> <BaseLib.dll> <MadScientist.dll> <localization-dir>");
var gameDir = Path.GetFullPath(args[0]);
var baseLibPath = Path.GetFullPath(args[1]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = name.Name == "BaseLib" ? baseLibPath : Path.Combine(gameDir, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameDir, "sts2.dll"));
var baseLib = AssemblyLoadContext.Default.LoadFromAssemblyPath(baseLibPath);
var mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[2]));
const BindingFlags methods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
var getEntry = game.GetType("MegaCrit.Sts2.Core.Models.ModelDb", true)!.GetMethod("GetEntry", methods)!;
var prefixId = baseLib.GetType("BaseLib.Patches.Content.PrefixIdPatch", true)!.GetMethod("AdjustID", methods)!;
string RuntimeId(Type type)
{
    // Invoke exactly the installed game's ID resolver and BaseLib's postfix,
    // without PatchAll or initializing the rest of either mod.
    object?[] values = [getEntry.Invoke(null, [type]), type];
    prefixId.Invoke(null, values);
    return (string)values[0]!;
}
int passed = 0;
void Check(bool ok, string label)
{
    if (!ok) throw new InvalidOperationException("FAIL " + label);
    Console.WriteLine("PASS " + label);
    passed++;
}
var models = mod.GetTypes().Where(t => !t.IsAbstract && t.IsClass).ToArray();
var powerBase = game.GetType("MegaCrit.Sts2.Core.Models.PowerModel", true)!;
var powers = models.Where(t => t.IsSubclassOf(powerBase)).ToArray();
Check(powers.Length == 15, "15 concrete production powers");
foreach (var lang in new[] { "eng", "zhs" })
{
    Dictionary<string, string> Table(string name) => JsonSerializer.Deserialize<Dictionary<string, string>>(
        File.ReadAllText(Path.Combine(args[3], lang, name + ".json")))!;
    var cards = Table("cards");
    foreach (var name in new[] { "FusedCard", "FusedXCard" })
    {
        var type = models.Single(t => t.Name == name);
        var expected = name == "FusedCard" ? "FUSED_CARD" : "FUSED_X_CARD";
        var id = RuntimeId(type);
        Check(id == expected, lang + " preserves existing model ID " + expected);
        Check(cards.TryGetValue(id + ".title", out var title) && !string.IsNullOrWhiteSpace(title)
            && cards.TryGetValue(id + ".description", out var description) && description.Contains("{Effects}"),
            lang + " runtime fusion ID resolves title and material description: " + id);
    }
    var texts = Table("powers");
    foreach (var type in powers)
    {
        var id = RuntimeId(type);
        Check(new[] { ".title", ".description", ".smartDescription" }.All(s =>
            texts.TryGetValue(id + s, out var text) && !string.IsNullOrWhiteSpace(text)),
            lang + " runtime power ID resolves all descriptions: " + id);
    }
    var soulType = powers.Single(t => t.Name == "SoulResearchPower");
    // Supply the resolved identity on an otherwise uninitialized managed object;
    // do not initialize gameplay state merely to exercise this key getter.
    var soul = RuntimeHelpers.GetUninitializedObject(soulType);
    var modelId = Activator.CreateInstance(game.GetType("MegaCrit.Sts2.Core.Models.ModelId", true)!,
        "power", RuntimeId(soulType));
    game.GetType("MegaCrit.Sts2.Core.Models.AbstractModel", true)!
        .GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(soul, modelId);
    var key = soulType.GetProperty("SmartDescriptionLocKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
    Check((string)key.GetValue(soul)! == RuntimeId(soulType) + ".smartDescription",
        lang + " native active smart-description key");
    soulType.GetProperty("Used")!.SetValue(soul, true);
    var spentKey = (string)key.GetValue(soul)!;
    Check(spentKey.EndsWith(".spent") && texts.ContainsKey(spentKey),
        lang + " native spent smart-description key resolves");
    var character = models.Single(t => t.FullName == "MadScientist.MadScientistCode.Character.MadScientist");
    var prefix = "THE_ARCHITECT.talk." + RuntimeId(character) + ".";
    var ancients = Table("ancients");
    Check(new[] { "0-0r.char", "0-0r.next", "0-1r.ancient", "0-attack" }.All(s => ancients.ContainsKey(prefix + s)),
        lang + " runtime character ID resolves Architect dialogue");
    var attackers = game.GetType("MegaCrit.Sts2.Core.Entities.Ancients.ArchitectAttackers", true)!;
    Check(Enum.Parse(attackers, ancients[prefix + "0-attack"]).ToString() == "Both",
        lang + " Architect attackers parse as the installed game's enum");
}
Console.WriteLine($"{passed} managed runtime localization checks passed; no gameplay or save round-trip executed.");
