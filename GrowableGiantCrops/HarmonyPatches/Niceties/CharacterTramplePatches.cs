using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.InventoryModels;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches;

/// <summary>
/// Holds patches that lets NPCs trample resource clumps.
/// </summary>
[HarmonyPatch(typeof(Character))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class CharacterTramplePatches
{
    private static readonly Api Api = new();

    [MethodImpl(TKConstants.Hot)]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(Character.MovePosition))]
    private static void Prefix(Character __instance, GameLocation currentLocation)
    {
        if (__instance is not NPC npc || !npc.isVillager()
            || currentLocation?.resourceClumps is null)
        {
            return;
        }

        try
        {
            Rectangle nextPosition = npc.nextPosition(npc.FacingDirection);
            for (int i = currentLocation.resourceClumps.Count - 1; i >= 0; i--)
            {
                ResourceClump clump = currentLocation.resourceClumps[i];
                if (!clump.getBoundingBox().Contains(nextPosition))
                {
                    continue;
                }

                switch (clump)
                {
                    case GiantCrop crop when ModEntry.Config.ShouldNPCsTrampleGiantCrops:
                        if (crop.modData.ContainsKey(InventoryGiantCrop.ModDataKey)
                            && Api.GetMatchingCrop(crop) is InventoryGiantCrop inventoryGiant)
                        {
                            currentLocation.debris.Add(new(inventoryGiant, inventoryGiant.TileLocation * Game1.tileSize));
                            currentLocation.resourceClumps.RemoveAt(i);
                        }
                        break;
                    case ResourceClump resourceClump when ModEntry.Config.ShouldNPCsTrampleResourcesClumps:
                        if (resourceClump.modData.ContainsKey(InventoryResourceClump.ResourceModdata)
                            && Api.GetMatchingClump(resourceClump) is InventoryResourceClump inventoryClump)
                        {
                            currentLocation.debris.Add(new(inventoryClump, inventoryClump.TileLocation * Game1.tileSize));
                            currentLocation.resourceClumps.RemoveAt(i);
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trampling a resource clump", ex);
        }
    }
}
