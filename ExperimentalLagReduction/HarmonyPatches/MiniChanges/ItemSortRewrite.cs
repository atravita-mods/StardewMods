using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ExperimentalLagReduction.HarmonyPatches.MiniChanges;

[HarmonyPatch(typeof(Item))]
internal static class ItemSortRewrite
{
    [HarmonyPatch(nameof(Item.CompareTo))]
    private static bool Prefix(Item __instance, object other, ref int __result)
    {
        if (other is not Item otherItem)
        {
            __result = 0;
            return false;
        }

        // sort by type
        __result = __instance.GetItemTypeId().CompareTo(otherItem.GetItemTypeId());
        if (__result != 0)
        {
            return false;
        }

        // sort category first
        __result = otherItem.getCategorySortValue() - __instance.getCategorySortValue();
        if (__result != 0)
        {
            return false;
        }

        // sort by internal name
        string my_name = GetInternalName(__instance);
        string other_name = GetInternalName(otherItem);

        __result = my_name.CompareTo(other_name);

        if (__result != 0)
        {
            return false;
        }

        // sort by qualified Id
        __result = __instance.QualifiedItemId.CompareTo(otherItem.QualifiedItemId);
        if (__result != 0)
        {
            return false;
        }

        // sort by level for trinkets
        if (__instance is Trinket me && otherItem is Trinket otherTrinket)
        {
            TrinketEffect myData = me.GetEffect();
            TrinketEffect otherData = otherTrinket.GetEffect();

            __result = myData.general_stat_1 - otherData.general_stat_1;
            if (__result != 0)
            {
                return false;
            }
        }

        // sort by preserve ID for preserves.
        if (__instance is SObject myObj && myObj.HasTypeObject() && otherItem is SObject otherObj && otherObj.HasTypeObject())
        {
            string? myPreserveId = myObj.preservedParentSheetIndex.Value;
            string? otherPreserveId = otherObj.preservedParentSheetIndex.Value;

            if (myPreserveId == "-1")
            {
                myPreserveId = null;
            }
            if (otherPreserveId == "-1")
            {
                otherPreserveId = null;
            }

            string? myPreserveName = myPreserveId?.GetInternalObjectName();
            string? otherPreserveName = otherPreserveId?.GetInternalObjectName();

            __result = myPreserveName?.CompareTo(otherPreserveName) ?? (myPreserveName == otherPreserveName ? 0 : -1);
            if (__result != 0)
            {
                return false;
            }
        }

        // sort by quality?
        __result = otherItem.Quality.CompareTo(__instance.Quality);
        if (__result != 0)
        {
            return false;
        }

        // sort by color for colored items
        if (__instance is ColoredObject myColor)
        {
            if (otherItem is ColoredObject otherColor)
            {
                Color myColorV = myColor.color.Value;
                ColorPicker.RGBtoHSV(myColorV.R, myColorV.G, myColorV.B, out float myHue, out float mySat, out float myValue);

                Color otherColorV = otherColor.color.Value;
                ColorPicker.RGBtoHSV(otherColorV.R, otherColorV.G, otherColorV.B, out float otherHue, out float otherSat, out float otherValue);

                __result = myHue.CompareTo(otherHue);
                if (__result != 0)
                {
                    return false;
                }

                __result = mySat.CompareTo(otherSat);
                if (__result != 0)
                {
                    return false;
                }

                __result = myValue.CompareTo(otherValue);
                if (__result != 0)
                {
                    return false;
                }
            }
            else
            {
                __result = -1;
                return false;
            }
        }
        else if (otherItem is ColoredObject)
        {
            __result = 1;
            return false;
        }

        // sort by stack
        __result = __instance.Stack - otherItem.Stack;
        return __result == 0;
    }

    private static string GetInternalName(this Item item)
        => ItemRegistry.GetData(item.QualifiedItemId)?.InternalName ?? (string.IsNullOrEmpty(item.Name) ? item.DisplayName : item.Name);

    private static string? GetInternalObjectName(this string id)
        => ItemRegistry.GetTypeDefinition(ItemRegistry.type_object)?.GetData(id)?.InternalName;
}