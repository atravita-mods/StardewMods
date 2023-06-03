using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Objects;

namespace CritterRings.HarmonyPatches;

#warning - this will need to be refactored for 1.6

/// <summary>
/// Adds the other effects.
/// </summary>
[HarmonyPatch(typeof(Ring))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class RingPatches
{
    private const int ButterflyMagneticism = 128;

    #region delegates
    private static readonly Lazy<Func<Ring, int?>> lightIDSourceGetter = new(() =>
        typeof(Ring).GetCachedField("_lightSourceID", ReflectionCache.FlagTypes.InstanceFlags)
                    .GetInstanceFieldGetter<Ring, int?>());

    private static readonly Lazy<Action<Ring, int?>> lightIDSourceSetter = new(() =>
        typeof(Ring).GetCachedField("_lightSourceID", ReflectionCache.FlagTypes.InstanceFlags)
                    .GetInstanceFieldSetter<Ring, int?>());
    #endregion

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onUnequip))]
    private static void PostfixUnequip(Ring __instance, Farmer who, GameLocation location)
    {
        if (__instance.ParentSheetIndex < 0)
        {
            return;
        }

        try
        {
            if (__instance.ParentSheetIndex == ModEntry.ButterflyRing)
            {
                who.MagneticRadius -= ButterflyMagneticism;
            }
            else if (__instance.ParentSheetIndex == ModEntry.FireFlyRing)
            {
                RemoveLightFrom(__instance, location);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("dequipping ring", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onNewLocation))]
    private static void PostfixNewLocation(Ring __instance, Farmer who, GameLocation environment)
    {
        if (__instance.ParentSheetIndex < 0)
        {
            return;
        }

        try
        {
            if (__instance.ParentSheetIndex == ModEntry.FireFlyRing)
            {
                __instance.onEquip(who, environment);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("dealing with new location", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onLeaveLocation))]
    private static void PostfixLeaveLocation(Ring __instance, GameLocation environment)
    {
        if (__instance.ParentSheetIndex < 0)
        {
            return;
        }

        try
        {
            if (__instance.ParentSheetIndex == ModEntry.FireFlyRing)
            {
                RemoveLightFrom(__instance, environment);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("leaving location", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onEquip))]
    private static void PostfixEquip(Ring __instance, Farmer who, GameLocation location)
    {
        if (__instance.ParentSheetIndex < 0)
        {
            return;
        }

        try
        {
            if (__instance.ParentSheetIndex == ModEntry.ButterflyRing)
            {
                who.MagneticRadius += ButterflyMagneticism;
            }
            else if (__instance.ParentSheetIndex == ModEntry.FireFlyRing)
            {
                int startingID;
                int lightID;
                unchecked
                {
                    startingID = __instance.uniqueID.Value + (int)who.UniqueMultiplayerID;
                    lightID = startingID;
                    while (location.sharedLights.ContainsKey(lightID))
                    {
                        lightID++;
                    }
                }

                lightIDSourceSetter.Value(__instance, lightID);
                location.sharedLights[lightID] = new LightSource(
                    textureIndex: 1,
                    new Vector2(who.Position.X + 21f, who.Position.Y + 64f),
                    radius: 12f,
                    new Color(0, 80, 0),
                    identifier: startingID,
                    light_context: LightSource.LightContext.None,
                    playerID: who.UniqueMultiplayerID);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("equipping ring", ex);
        }
    }

    private static void RemoveLightFrom(Ring __instance, GameLocation location)
    {
        int? lightID = lightIDSourceGetter.Value(__instance);
        if (lightID.HasValue)
        {
            location.removeLightSource(lightID.Value);
            lightIDSourceSetter.Value(__instance, null);
        }
    }
}
