﻿using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.TerrainFeatures;

namespace GiantCropFertilizer.HarmonyPatches;

/// <summary>
/// Holds patches against HoeDirt that replaces our fertilizer.
/// This way MultiFertlizer doesn't clear us...
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class HoeDirtPatcher
{
    /// <summary>
    /// Applies the hoedirt compat patches for multifertilizer.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    /// <remarks>This is only needed for certain versions of multifertilizer.</remarks>
    internal static void ApplyPatches(Harmony harmony)
    {
        HarmonyMethod? prefix = new(
            typeof(HoeDirtPatcher).GetCachedMethod(nameof(PrefixMulti), ReflectionCache.FlagTypes.StaticFlags),
            priority: Priority.VeryHigh);
        HarmonyMethod? postfix = new(
            typeof(HoeDirtPatcher).GetCachedMethod(nameof(HoeDirtPatcher.PostfixMulti), ReflectionCache.FlagTypes.StaticFlags),
            priority: Priority.VeryLow);

        harmony.Patch(
            typeof(HoeDirt).GetCachedMethod("applySpeedIncreases", ReflectionCache.FlagTypes.InstanceFlags),
            prefix: prefix,
            postfix: postfix);

        harmony.Patch(
            typeof(HoeDirt).GetCachedMethod(nameof(HoeDirt.dayUpdate), ReflectionCache.FlagTypes.InstanceFlags),
            prefix: prefix,
            postfix: postfix);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HoeDirt), nameof(HoeDirt.plant))]
    private static bool PrefixCanPlant(HoeDirt __instance, int index, bool isFertilizer, ref bool __result)
    {
        if (isFertilizer && ModEntry.GiantCropFertilizerID != -1 && ModEntry.GiantCropFertilizerID == index && __instance.fertilizer.Value == index)
        {
            Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13916-2"));
            __result = false;
            return false;
        }
        return true;
    }

    private static void PrefixMulti(HoeDirt __instance, out int? __state)
    {
        if(ModEntry.GiantCropFertilizerID != -1 && ModEntry.GiantCropFertilizerID == __instance.fertilizer.Value)
        {
            ModEntry.ModMonitor.DebugOnlyLog("Found fertilizer, saving");
            __state = ModEntry.GiantCropFertilizerID;
        }
        else
        {
            __state = null;
        }
    }

    private static void PostfixMulti(HoeDirt __instance, int? __state)
    {
        if (__state is not null && __state.Value == ModEntry.GiantCropFertilizerID)
        {
            ModEntry.ModMonitor.DebugOnlyLog("Found fertilizer, restoring");
            __instance.fertilizer.Value = __state.Value;
        }
    }
}