using AtraCore.Framework.ReflectionManager;

using HarmonyLib;

using StardewValley.SpecialOrders.Objectives;

namespace SpecialOrdersExtended.HarmonyPatches;

/// <summary>
/// Patches for debugging.
/// </summary>
internal static class DebuggingPatches
{
    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    internal static void Apply(Harmony harmony)
    {
#if !DEBUG
        if (!ModEntry.ModMonitor.IsVerbose)
        {
            return;
        }
#endif

        harmony.Patch(
            original: typeof(FishObjective).GetCachedMethod(nameof(FishObjective.OnFishCaught), ReflectionCache.FlagTypes.InstanceFlags),
            prefix: new HarmonyMethod(typeof(DebuggingPatches), nameof(PrefixFishMethod)));
    }

    private static void PrefixFishMethod(Item fish_item)
    {
        if (fish_item is null)
        {
            ModEntry.ModMonitor.Log("null fish?", LogLevel.Info);
            return;
        }

        ModEntry.ModMonitor.Log($"Checking fish {fish_item.Name} with context tags: {string.Join(", ", fish_item.GetContextTags())}", LogLevel.Info);
    }
}
