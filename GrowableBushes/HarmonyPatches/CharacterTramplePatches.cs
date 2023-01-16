using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GrowableBushes.Framework;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace GrowableBushes.HarmonyPatches;

[HarmonyPatch(typeof(Character))]
internal static class CharacterTramplePatches
{
    [HarmonyPatch(nameof(Character.MovePosition))]
    private static void Prefix(Character __instance, GameLocation currentLocation)
    {
        if (!ModEntry.Config.ShouldNPCsTrampleBushes || __instance is not NPC npc || !npc.isVillager()
            || currentLocation?.largeTerrainFeatures is null)
        {
            return;
        }

        try
        {
            Rectangle nextPosition = npc.nextPosition(npc.FacingDirection);

            for (int i = 0; i < currentLocation.largeTerrainFeatures.Count; i++)
            {
                var feature = currentLocation.largeTerrainFeatures[i];
                if (feature is Bush bush && bush.getBoundingBox().Contains(nextPosition)
                    && bush.modData.ContainsKey(InventoryBush.BushModData))
                {
                    bush.health = -1f;
                    Axe axe = new();
                    axe.UpgradeLevel = 3;
                    bush.performToolAction(axe, 0, bush.currentTileLocation, currentLocation);
                    currentLocation.largeTerrainFeatures.RemoveAt(i);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in trying to trample a bush:\n\n{ex}", LogLevel.Error);
        }
    }
}
