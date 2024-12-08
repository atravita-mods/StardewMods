using System.Diagnostics;
using System.Reflection;

using HarmonyLib;

using Microsoft.Xna.Framework.Graphics;

namespace WhyAreYouDisposed;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static IMonitor ModMonitor = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;

        Harmony harmony = new (this.ModManifest.UniqueID);
        harmony.Patch(
            typeof(Texture).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)]),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix)));
    }

    private static void Prefix(Texture __instance, bool disposing)
    {
        if (!__instance.IsDisposed && disposing)
        {
            ModMonitor.Log($"Disposal of {__instance.Name ?? "unnamed texture"} at {new StackTrace()}", LogLevel.Info);
        }
    }
}
