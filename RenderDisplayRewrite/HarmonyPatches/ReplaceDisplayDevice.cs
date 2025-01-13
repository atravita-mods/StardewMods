
using HarmonyLib;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using MiniAtraShared.Extensions;

using RenderDisplayRewrite.Framework;

using xTile.Display;

namespace RenderDisplayRewrite.HarmonyPatches;
internal static class ReplaceDisplayDevice
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("StardewModdingAPI.Framework.SGame"), "CreateDisplayDevice"),
            prefix: new HarmonyMethod(typeof(ReplaceDisplayDevice), nameof(Prefix)));
    }

    private static bool Prefix(ContentManager content, GraphicsDevice graphicsDevice, ref IDisplayDevice __result)
    {
        try
        {
            __result = new DisplayDevice(content, graphicsDevice, ModEntry.ModMonitor);
            ModEntry.ModMonitor.Log("Overriding display device....");
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding graphics device", ex);
            return true;
        }
    }
}
