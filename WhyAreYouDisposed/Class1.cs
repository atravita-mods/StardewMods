using System.Diagnostics;
using System.Reflection;

using HarmonyLib;

using Microsoft.Xna.Framework.Graphics;

namespace WhyAreYouDisposed;
internal sealed class ModEntry : Mod
{
    private static IMonitor ModMonitor = null!;

    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;

        var harmony = new Harmony(this.ModManifest.UniqueID);
        harmony.Patch(
            typeof(Texture).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)]),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix)));
    }

    private static void Prefix(Texture __instance)
    {
        if (!__instance.IsDisposed)
        {
            var stacktrace = new StackTrace();
            ModMonitor.Log($"Disposal of {__instance.Name ?? "unnamed texture"} at {stacktrace}", LogLevel.Info);
        }
    }
}
