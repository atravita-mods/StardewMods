using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.InventoryModels;
using GrowableGiantCrops.HarmonyPatches.Compat;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches on SObject for misc stuff.
/// </summary>
[HarmonyPatch(typeof(SObject))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PatchesForSObject
{
    /// <summary>
    /// Marks an SObject placed with this mod.
    /// </summary>
    internal const string ModDataMiscObject = "atravita.GrowableGiantCrops.PlacedObject";

    private const string ModDataKey = "atravita.GrowableGiantCrops.PlacedSlimeBall";

    private static bool IsSmallTerrainObject(this SObject obj) => obj.IsBreakableStone() || obj.IsWeeds() || obj.IsTwig();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SObject.placementAction))]
    private static void PostfixPlacement(SObject __instance, GameLocation location, int x, int y)
    {
        try
        {
            if (__instance?.bigCraftable?.Value == true
                && location.Objects.TryGetValue(new Vector2(x / Game1.tileSize, y / Game1.tileSize), out SObject? placed))
            {
                if (__instance.Name == "Slime Ball")
                {
                    placed.modData.Remove(SlimeProduceCompat.SlimeBall);
                    placed.modData.SetBool(ModDataKey, true);
                    placed.TileLocation = new Vector2(x / Game1.tileSize, y / Game1.tileSize);
                }
                else if (__instance.Name == "Mushroom Box")
                {
                    placed.modData?.SetBool(ModDataMiscObject, true);
                    placed.TileLocation = new Vector2(x / Game1.tileSize, y / Game1.tileSize);
                    placed.Fragility = SObject.fragility_Removable;
                }
                else if (__instance.ParentSheetIndex == 78)
                {
                    placed.modData?.SetBool(ModDataMiscObject, true);
                    placed.Fragility = SObject.fragility_Removable;
                }
            }
            else if (__instance?.bigCraftable?.Value == false)
            {
                if (SObject.isWildTreeSeed(__instance.ItemId)
                    && location.terrainFeatures.TryGetValue(new Vector2(x / Game1.tileSize, y / Game1.tileSize), out TerrainFeature? terrain)
                    && terrain is Tree tree)
                {
                    tree.modData?.SetEnum(InventoryTree.ModDataKey, (TreeIndexes)tree.treeType.Value);
                }
                if (InventoryFruitTree.IsValidFruitTree(__instance.ParentSheetIndex)
                    && location.terrainFeatures.TryGetValue(new Vector2(x / Game1.tileSize, y / Game1.tileSize), out TerrainFeature? feature)
                    && feature is FruitTree fruitTree)
                {
                    fruitTree.modData?.SetInt(InventoryFruitTree.ModDataKey, __instance.ParentSheetIndex);
                }
                if (__instance.IsSmallTerrainObject())
                {
                    __instance.modData?.SetBool(ModDataMiscObject, true);
                    __instance.TileLocation = new Vector2(x / Game1.tileSize, y / Game1.tileSize);

                    if (location is MineShaft shaft && __instance.Name == "Stone")
                    {
                        int stonesLeft = ShovelTool.MineRockCountGetter.Value(shaft);
                        stonesLeft++;
                        ModEntry.ModMonitor.DebugOnlyLog($"{stonesLeft} stones left on floor {shaft.mineLevel}", LogLevel.Info);
                        ShovelTool.MineRockCountSetter.Value(shaft, stonesLeft);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjusting placement logic", ex);
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(nameof(SObject.checkForAction))]
    private static bool PrefixSlimeBall(SObject __instance, ref bool __result)
    {
        if (!ModEntry.Config.CanSquishPlacedSlimeBalls
            && __instance?.bigCraftable?.Value == true && __instance.Name == "Slime Ball"
            && __instance.modData?.GetBool(ModDataKey) == true)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SObject.isPlaceable))]
    private static void PostfixIsPlaceable(SObject __instance, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        try
        {
            if (!__instance.bigCraftable.Value && __instance.GetType() == typeof(SObject))
            {
                if (__instance.ParentSheetIndex == 590 // artifact spot
                    || __instance.IsSmallTerrainObject())
                {
                    __result = true;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("making small terrain items placeable", ex);
        }
    }
}
