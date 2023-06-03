using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Characters;
using StardewValley.Locations;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Returns escaped children.
/// </summary>
[HarmonyPatch(typeof(Child))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class EscapedChildFixer
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(Child.dayUpdate))]
    private static bool Prefix(Child __instance)
    {
        try
        {
            if (__instance.currentLocation is not FarmHouse)
            {
                ModEntry.ModMonitor.Log($"Child {__instance.Name} seems to have escaped the farmhouse, sending them back. Current location {__instance.currentLocation?.NameOrUniqueName}", LogLevel.Debug);

                Farmer parent = Game1.MasterPlayer;

                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    if (farmer.UniqueMultiplayerID == __instance.idOfParent.Value)
                    {
                        parent = farmer;
                        break;
                    }
                }

                if ((Utility.getHomeOfFarmer(parent) ?? Game1.getLocationFromName("FarmHouse")) is not FarmHouse house)
                {
                    ModEntry.ModMonitor.Log($"Failed to find farmhouse", LogLevel.Error);
                    return false;
                }

                // day update should fix the location if this succeeds.
                Game1.warpCharacter(__instance, house, Vector2.One);

                if (__instance.currentLocation is not FarmHouse)
                {
                    ModEntry.ModMonitor.Log($"Failed while trying to return child {__instance.Name} to Farmhouse.", LogLevel.Error);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("returning child to farmhouse", ex);
        }

        return true;
    }
}
