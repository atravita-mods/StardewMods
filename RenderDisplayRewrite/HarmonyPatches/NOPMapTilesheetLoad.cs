using HarmonyLib;
using RenderDisplayRewrite.Framework;
using xTile;
using xTile.Display;

namespace RenderDisplayRewrite.HarmonyPatches;
internal static class NOPMapTilesheetLoad
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Map), nameof(Map.LoadTileSheets)),
            prefix: new HarmonyMethod(typeof(NOPMapTilesheetLoad), nameof(Prefix))
        );
    }

    private static bool Prefix(IDisplayDevice displayDevice) => displayDevice is not DisplayDevice;
}
