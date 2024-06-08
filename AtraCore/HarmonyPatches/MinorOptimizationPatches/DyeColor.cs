using System.Collections.Concurrent;

using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace AtraCore.HarmonyPatches.MinorOptimizationPatches;

[HarmonyPatch(typeof(ItemContextTagManager))]
internal static class DyeColor
{
    private static readonly ConcurrentDictionary<string, Color?> _dyeColor = new();

    internal static void Reset() => _dyeColor.Clear();

    private const string key = "atravita.DyeColorOverride";

    [HarmonyPatch(nameof(ItemContextTagManager.GetColorFromTags))]
    private static bool Prefix(Item item, ref Color? __result)
    {
        try
        {
            if (_dyeColor.TryGetValue(item.QualifiedItemId, out var color))
            {
                if (color is null)
                {
                    return true;
                }
                else
                {
                    __result = color;
                    return false;
                }
            }
            else if (ItemRegistry.GetData(item.QualifiedItemId) is { } parsed && parsed.RawData is { } rawData)
            {
                switch (rawData)
                {
                    case ObjectData objectData:
                    {
                        if (objectData?.CustomFields?.TryGetValue(key, out string? s) == true
                            && ColorHandler.TryParseColor(s, out Color c))
                        {
                            __result = _dyeColor[item.QualifiedItemId] = c;
                            return false;
                        }
                        break;
                    }
                    case BigCraftableData bigCraftableData:
                    {
                        if (bigCraftableData?.CustomFields?.TryGetValue(key, out string? s) == true
                            && ColorHandler.TryParseColor(s, out Color c))
                        {
                            __result = _dyeColor[item.QualifiedItemId] = c;
                            return false;
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            _dyeColor[item.QualifiedItemId] = null;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"overriding dye color for {item.QualifiedItemId}", ex);
        }

        return true;
    }

    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(ItemContextTagManager.GetColorFromTags))]
    private static void Postfix(Item item, Color? __result, bool __runOriginal)
    {
        if (__runOriginal && __result is not null)
        {
            _dyeColor[item.QualifiedItemId] = __result.Value;
        }
    }
}
