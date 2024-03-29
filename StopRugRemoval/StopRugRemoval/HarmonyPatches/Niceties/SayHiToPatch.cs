using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;
using Microsoft.Xna.Framework;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Patches for SayHiTo...
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SayHiToPatch
{
    // Short circuit this if there's no player on the map. It handles only the text NPCs say when they enter locations.
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)] // run after every other prefix.
    [HarmonyPatch(nameof(NPC.arriveAt))]
    private static bool PrefixArriveAt(GameLocation l)
        => l == Game1.player.currentLocation;

    // Short circuit this if there's no player on the map. It literally only handles saying hi to people.
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)] // run after every other prefix.
    [HarmonyPatch(nameof(NPC.performTenMinuteUpdate))]
    private static bool PrefixTenMinuteUpdate(GameLocation location)
        => location == Game1.player.currentLocation;

    // Get the NPCs to pretty frequently greet each other in the saloon?
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    [HarmonyPatch(nameof(NPC.performTenMinuteUpdate))]
    private static void PostfixTenMinuteUpdate(NPC __instance, GameLocation location, int ___textAboveHeadTimer)
    {
        try
        {
            if (Game1.player.currentLocation == location && location.Name == "Saloon"
                && __instance.IsVillager)
            {
                if (__instance.isMoving() && ___textAboveHeadTimer < 0 && Random.Shared.OfChance(0.6))
                {
                    // Invert the check here to favor the farmer. :(
                    // Goddamnit greet me more often plz.
                    Character? c = Utility.isThereAFarmerWithinDistance(__instance.Tile, 4, location);
                    if (c is null)
                    {
                        Vector2 loc = __instance.Tile;
                        foreach (NPC npc in location.characters)
                        {
                            if ((npc.Tile - loc).LengthSquared() <= 16 && __instance.isFacingToward(npc.Tile))
                            {
                                c = npc;
                                break;
                            }
                        }
                    }

                    if (c is not null)
                    {
                        __instance.sayHiTo(c);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("making npcs greet the farmer", ex);
        }
    }
}
