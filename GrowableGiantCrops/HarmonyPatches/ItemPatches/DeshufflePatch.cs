using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ItemManagement;
using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework.InventoryModels;
using HarmonyLib;

namespace GrowableGiantCrops.HarmonyPatches.ItemPatches;

/// <summary>
/// Patches JA to also deshuffle inventory giant crops.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DeshufflePatch
{
    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        Type? ja = AccessTools.TypeByName("JsonAssets.Mod");
        if (ja is null)
        {
            ModEntry.ModMonitor.Log($"JA mod instance could not be found, deshuffling will fail!", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(
                original: ja.GetCachedMethod("FixItem", ReflectionCache.FlagTypes.InstanceFlags),
                prefix: new HarmonyMethod(typeof(DeshufflePatch).StaticMethodNamed(nameof(Prefix))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("patching JA to deshuffle inventory giant crops", ex);
        }
    }

    /// <summary>
    /// Hitchhikes off JA to deshuffle giant inventory crops.
    /// </summary>
    /// <param name="item">The item to deshuffle.</param>
    /// <param name="__result">true to delete it, false to keep it.</param>
    /// <returns>true to continue to ja's method, false to not do that.</returns>
    private static bool Prefix(Item item, ref bool __result)
    {
        if (item is InventoryGiantCrop giantCrop)
        {
            try
            {
                string name = giantCrop.Name[InventoryGiantCrop.InventoryGiantCropPrefix.Length..];
                int id = DataToItemMap.GetID(ItemTypeEnum.SObject, name);
                if (id != -1)
                {
                    ModEntry.ModMonitor.Log($"Fixing up {giantCrop.Name} by name");
                    giantCrop.ParentSheetIndex = id;
                    __result = false;
                }
                else
                {
                    ModEntry.ModMonitor.Log($"Attempting to fix up {giantCrop.Name} by id");
                    SObject dummy = new(giantCrop.ParentSheetIndex, 1);
                    __result = ModEntry.JaAPI?.FixIdsInItem(dummy) ?? true;
                    giantCrop.ParentSheetIndex = dummy.ParentSheetIndex;
                }

                giantCrop.ResetDrawFields();

                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"deshuffling {giantCrop.Name}", ex);
            }
        }

        if (item is InventoryFruitTree fruitTree)
        {
            try
            {
                string name = fruitTree.Name[InventoryFruitTree.InventoryTreePrefix.Length..];
                int id = DataToItemMap.GetID(ItemTypeEnum.SObject, name);
                if (id != -1)
                {
                    ModEntry.ModMonitor.Log($"Fixing up {fruitTree.Name} by name");
                    fruitTree.ParentSheetIndex = id;
                    __result = false;
                }
                else
                {
                    ModEntry.ModMonitor.Log($"Attempting to fix up {fruitTree.Name} by id");
                    SObject dummy = new(fruitTree.ParentSheetIndex, 1);
                    __result = ModEntry.JaAPI?.FixIdsInItem(dummy) ?? true;
                    fruitTree.ParentSheetIndex = dummy.ParentSheetIndex;
                }

                fruitTree.Reset();

                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"deshuffling {fruitTree.Name}", ex);
            }
            return true;
        }

        return true;
    }
}