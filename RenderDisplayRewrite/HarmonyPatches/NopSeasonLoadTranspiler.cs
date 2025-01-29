using System.Reflection.Emit;

using HarmonyLib;

using Microsoft.Xna.Framework.Graphics;

using RenderDisplayRewrite.Framework;

using xTile.Display;
using xTile.Tiles;

namespace RenderDisplayRewrite.HarmonyPatches;

internal static class NopSeasonLoadTranspiler
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(GameLocation), nameof(GameLocation.updateSeasonalTileSheets)),
            transpiler: new (typeof(NopSeasonLoadTranspiler), nameof(Transpiler)));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);

        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Game1), nameof(Game1.mapDisplayDevice))),
            new CodeMatch((code) => code.IsLdloc()),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(IDisplayDevice), nameof(IDisplayDevice.LoadTileSheet))))
        .ThrowIfInvalid("mapDisplayDevice")
        .Advance(2)
        .RemoveInstruction()
        .Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NopSeasonLoadTranspiler), nameof(AdjustSeasonalTilesheet))));

        return matcher.InstructionEnumeration();
    }

    private static void AdjustSeasonalTilesheet(IDisplayDevice device, TileSheet sheet)
    {
        if (device is DisplayDevice)
        {
            // we load so it still throws an error if it's not found.
            Game1.content.Load<Texture2D>(sheet.ImageSource);
        }
        else
        {
            device.LoadTileSheet(sheet);
        }
    }
}
