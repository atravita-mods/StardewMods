﻿#if DEBUG

using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;

namespace PamTries.HarmonyPatches;

[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class BusDriverSchedulePatch
{
    /// <summary>
    /// Gets or sets the current bus driver.
    /// </summary>
    internal static string CurrentDriver { get; set; } = "Pam";

    internal static string GetCurrentDriver() => CurrentDriver;

    [HarmonyPatch(nameof(GameLocation.busLeave))]
    private static bool Prefix(GameLocation __instance)
    {
        ModEntry.ModMonitor.Log("Reached BusLeave!", LogLevel.Alert);
        NPC? driver = __instance.getCharacterFromName(CurrentDriver);
        if (driver is null)
        {
            ModEntry.ModMonitor.Log($"Driver {CurrentDriver} is not found!", LogLevel.Error);
            if (__instance is BusStop || __instance.Name.Equals("BusStop", StringComparison.OrdinalIgnoreCase))
            {
                Game1.warpFarmer("Desert", 32, 27, flip: true);
            }
            else
            {
                Game1.warpFarmer("BusStop", 9, 9, flip: true);
            }
            return false;
        }
        return false;
    }
}

#endif