﻿using System.Diagnostics.CodeAnalysis;
using GingerIslandMainlandAdjustments.ScheduleManager;
using HarmonyLib;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

/// <summary>
/// Class to handle patching of NPCs for dialogue.
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal class DialoguePatches
{
    /// <summary>
    /// Appends checkForNewCurrentDialogue to look for GI-specific dialogue.
    /// </summary>
    /// <param name="__instance">NPC instance.</param>
    /// <param name="__0">Heart level.</param>
    /// <param name="__1">Whether or not to have a season prefix.</param>
    /// <param name="__result">Whether or not new dialogue has been found.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NPC.checkForNewCurrentDialogue))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Convention used by Harmony")]
    public static void DoCheckIslandDialogue(ref NPC __instance, ref int __0, ref bool __1, ref bool __result)
    { // __0 = heartlevel, as int. __1 = whether or not to have a season prefix?
        try
        {
            if (__result)
            { // game code has returned a value, therefore skip me.
                return;
            }
            if (!Game1.netWorldState.Value.IslandVisitors.ContainsKey(__instance.Name)
                || !Game1.netWorldState.Value.IslandVisitors[__instance.Name])
            { // am not headed to island today.
                return;
            }
            if (__instance.currentLocation is (IslandLocation or FarmHouse))
            { // Currently on island or is spouse in Farmhouse, skip me.
                return;
            }
            string preface = __1 ? string.Empty : Game1.currentSeason;

            string baseKey;

            if (Game1.timeOfDay <= 1200)
            {
                baseKey = preface + "Resort_Approach";
            }
            else if (Game1.timeOfDay >= 1800)
            {
                baseKey = preface + "Resort_Left";
                if (!__instance.currentLocation.IsOutdoors && !(__instance.currentLocation.Name == "FishShop"))
                {
                    baseKey += __instance.currentLocation.Name; // use specific INDOOR keys.
                }
            }
            else
            {
                return;
            }

            // Handle group-specific dialogue.
            if (GIScheduler.CurrentVisitingGroup?.Contains(__instance) == true
                && GIScheduler.CurrentGroup is not null
                && DialogueUtilities.TryGetIslandDialogue(__instance, $"{baseKey}_{GIScheduler.CurrentGroup}", __0))
            {
                __result = true;
                return;
            }

            Farmer spouse = __instance.getSpouse();
            if (spouse != null && spouse == Game1.player)
            {
                if (DialogueUtilities.TryGetIslandDialogue(__instance, baseKey + "_marriage", __0))
                {
                    __result = true;
                    return;
                }
            }
            __result = DialogueUtilities.TryGetIslandDialogue(__instance, baseKey, __0);
            return;
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in checking for island dialogue for NPC {__instance.Name}\n{ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Append the marriageDuties function to replace dialogue with GI-specific dialogue.
    /// </summary>
    /// <param name="__instance">NPC instance.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NPC.marriageDuties))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Convention used by Harmony")]
    public static void AppendMarriageDuties(ref NPC __instance)
    {
        try
        {
            Game1.netWorldState.Value.IslandVisitors.TryGetValue(__instance.Name, out bool atIsland);
            if (!atIsland)
            {
                return;
            }
            if (__instance.getSpouse() is null)
            {
                return;
            }
            __instance.currentMarriageDialogue.Clear();
            __instance.setNewDialogue("MarriageDialogue", "GILeave_", -1, add: false, clearOnMovement: true);
            Globals.ModMonitor.Log($"Setting GILeave_{__instance.Name}.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in setting GILeave dialogue for {__instance.Name}:\n{ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Appends spouse arrival back at farmhouse to replace with GI-specific dialogue.
    /// </summary>
    /// <param name="__instance">NPC instance.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NPC.arriveAtFarmHouse))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Convention used by Harmony")]
    public static void AppendArrival(ref NPC __instance)
    {
        try
        {
            Game1.netWorldState.Value.IslandVisitors.TryGetValue(__instance.Name, out bool atIsland);
            if (!atIsland)
            {
                return;
            }
            __instance.currentMarriageDialogue.Clear();
            __instance.setNewDialogue("MarriageDialogue", "GIReturn_", -1, add: false, clearOnMovement: true);
            Globals.ModMonitor.Log($"Setting GIReturn_{__instance.Name}.", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in setting GIReturn dialogue for {__instance.Name}:\n{ex}", LogLevel.Error);
        }
    }
}