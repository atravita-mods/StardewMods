using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using RenderDisplayRewrite.Framework;

using xTile.Layers;
using xTile.Tiles;

namespace RenderDisplayRewrite.HarmonyPatches;
internal static class ReplaceStaticTile
{
    internal static void Apply(Harmony harmony)
    {
        var tmxtileType = AccessTools.TypeByName("TMXTile.TMXFormat");
        MethodBase[] methods = [
            AccessTools.Method(tmxtileType, "LoadTile"),
            AccessTools.Method(AccessTools.TypeByName("StardewModdingAPI.Framework.Content.AssetDataForMap"), "CreateTile"),
            AccessTools.Method(AccessTools.TypeByName("xTile.Format.TbinFormat"), "LoadStaticTile"),
            AccessTools.Method(AccessTools.TypeByName("xTile.Format.TideFormat"), "LoadStaticTile")
            ];
        foreach (var method in methods)
        {
            ModEntry.ModMonitor.Log($"Transpiling {method.FullDescription()}");
            harmony.Patch(
                original: method,
                transpiler: new HarmonyMethod(typeof(ReplaceStaticTile), nameof(Transpiler)),
                postfix: new HarmonyMethod(typeof(ReplaceStaticTile), nameof(Postfix)));
        }

        foreach (var inner in tmxtileType.GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (!inner.Name.StartsWith("<>c", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var method in inner.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (method.ReturnType == typeof(StaticTile) && method.Name.Contains("<LoadTile>"))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1)
                    {
                        continue;
                    }
                    ModEntry.ModMonitor.Log($"Transpiling {method.FullDescription()}");
                    harmony.Patch(
                        original: method,
                        transpiler: new HarmonyMethod(typeof(ReplaceStaticTile), nameof(Transpiler)));
                }
            }
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
    {
        var constructor = typeof(StaticTile).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(Layer), typeof(TileSheet), typeof(BlendMode), typeof(int)])
                            ?? throw new InvalidOperationException("could not find static tile constructor");

        var replacement = typeof(RotTile).GetConstructor(BindingFlags.Instance | BindingFlags.Public, [typeof(Layer), typeof(TileSheet), typeof(BlendMode), typeof(int)])
                            ?? throw new InvalidOperationException("could not find rot tile constructor");

        int count = 0;
        foreach (var code in codes)
        {
            if (code.opcode == OpCodes.Newobj && constructor == (code.operand as ConstructorInfo))
            {
                code.operand = replacement;
                ++count;
            }
            yield return code;
        }

        ModEntry.ModMonitor.Log($"{count} instances found.");
    }

    private static void Postfix(Tile __result)
    {
        if (__result is AnimatedTile animated)
        {
            foreach (var tile in animated.TileFrames)
            {
                tile.FixUpRotTile();
            }
        }
        else
        {
            __result.FixUpRotTile();
        }
    }

    private static void FixUpRotTile(this Tile tile)
    {
        if (tile is not RotTile rot)
        {
            ModEntry.ModMonitor.VerboseLog($"Huh, uncaught Static Tile? {new StackTrace()}");
            return;
        }
        rot.UpdateEffects();
        rot.UpdateRotation();
    }

}
