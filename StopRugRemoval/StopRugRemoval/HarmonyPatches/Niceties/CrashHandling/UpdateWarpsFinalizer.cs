using AtraShared.ConstantsAndEnums;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Adds a patch so bad warps do not break everything.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class UpdateWarpsFinalizer
{
    [HarmonyPatch(nameof(GameLocation.updateWarps))]
    private static Exception? Finalizer(GameLocation __instance, Exception __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"GameLocation {__instance.NameOrUniqueName} has invalid warps., suppressing error.", LogLevel.Error);
            ModEntry.ModMonitor.Log(__exception.ToString(), LogLevel.Info);

            if (__instance.map.Properties.TryGetValue("NPCWarp", out var npcWarps))
            {
                ModEntry.ModMonitor.Log($"Relevant NPC warps:{npcWarps ?? "null"}", LogLevel.Info);
            }

            if (__instance.map.Properties.TryGetValue("Warp", out var warps))
            {
                ModEntry.ModMonitor.Log($"Relevant NPC warps:{warps ?? "null"}", LogLevel.Info);
            }

            if (__instance.warps.Count == 0)
            {
                ModEntry.ModMonitor.Log("Adding safety warp", LogLevel.Info);

                // adds an out of bounds warp to the backwoods. This is the least likely to break anything.
                __instance.warps.Add(new(-15, -15, "Backwoods", 15, 18, false));
            }
        }
        return null;
    }
}
