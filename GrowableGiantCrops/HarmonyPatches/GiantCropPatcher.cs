using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.InventoryModels;
using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches;

/// <summary>
/// Holds patches to remove the tapper before the big crop is destroyed.
/// </summary>
[HarmonyPatch(typeof(GiantCrop))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class GiantCropPatcher
{
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(GiantCrop.performToolAction))]
    private static bool Prefix(GiantCrop __instance, Tool t)
    {
        if (t is not ShovelTool)
        {
            return true;
        }

        // make sure to pop off tap giant crop's tapper!
        try
        {
            for (int x = (int)__instance.Tile.X; x < (int)__instance.Tile.X + __instance.width.Value; x++)
            {
                for (int y = (int)__instance.Tile.Y; y < (int)__instance.Tile.Y + __instance.width.Value; y++)
                {
                    Vector2 tile = new(x, y);
                    if (Game1.currentLocation.objects.TryGetValue(tile, out SObject? obj)
                        && obj.IsTapper())
                    {
                        if (obj.readyForHarvest.Value && obj.heldObject.Value is SObject held)
                        {
                            Game1.currentLocation.debris.Add(new(held, tile * 64));
                        }
                        obj.heldObject.Value = null;
                        obj.readyForHarvest.Value = false;

                        InventoryGiantCrop.ShakeGiantCrop(__instance);
                        obj.performRemoveAction();
                        Game1.createItemDebris(obj, tile * 64f, -1);
                        Game1.currentLocation.objects.Remove(tile);
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("popping the tapper off", ex);
        }
        return true;
    }
}
