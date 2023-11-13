using System.Reflection;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using FastExpressionCompiler.LightExpression;

using GrowableGiantCrops.Framework;

using HarmonyLib;

using StardewValley.Enchantments;

namespace GrowableGiantCrops.HarmonyPatches.Compat;

/// <summary>
/// Patches for FTM's burial spot.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FTMArtifactSpotPatch
{
    #region delegates
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "Reviewed.")]
    private static readonly Lazy<Func<SObject, bool>?> isBuriedItem = new(() =>
    {
        Type? buriedItem = AccessTools.TypeByName("FarmTypeManager.ModEntry+BuriedItems");
        if (buriedItem is null)
        {
            return null;
        }

        ParameterExpression? obj = Expression.ParameterOf<SObject>("obj");
        TypeBinaryExpression? express = Expression.TypeIs(obj, buriedItem);
        return Expression.Lambda<Func<SObject, bool>>(express, obj).CompileFast();
    });

    /// <summary>
    /// Gets whether an item is a MoreGrassStarter grass starter.
    /// </summary>
    internal static Func<SObject, bool>? IsBuriedItem => isBuriedItem.Value;
    #endregion

    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        Type? buriedItem = AccessTools.TypeByName("FarmTypeManager.ModEntry+BuriedItems");
        if (buriedItem is null)
        {
            ModEntry.ModMonitor.Log($"Farm Type Manager's buried items may not behave correctly if dug up with the shovel.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(
                original: buriedItem.GetCachedMethod("performToolAction", ReflectionCache.FlagTypes.InstanceFlags),
                prefix: new HarmonyMethod(typeof(FTMArtifactSpotPatch).StaticMethodNamed(nameof(Prefix))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("patching FTM to support artifact spots.", ex);
        }
    }

    private static bool Prefix(SObject __instance, Tool t, GameLocation location, ref bool __result)
    {
        if (t is not ShovelTool shovel)
        {
            return true;
        }

        try
        {
            __result = true;
            int count = shovel.hasEnchantmentOfType<GenerousEnchantment>() ? 2 : 1;
            MethodInfo method = __instance.GetType().GetCachedMethod("releaseContents", ReflectionCache.FlagTypes.InstanceFlags);

            do
            {
                method.Invoke(__instance, new[] { location });
            }
            while (count-- > 0);

            if (!location.terrainFeatures.ContainsKey(__instance.TileLocation))
            {
                location.makeHoeDirt(__instance.TileLocation);
            }
            location.playSound("hoeHit");
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("using shovel on FTM artifact spot", ex);
        }

        return true;
    }
}
