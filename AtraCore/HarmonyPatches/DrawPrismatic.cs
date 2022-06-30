using AtraCore.Framework.ItemManagement;
using AtraCore.Models;
using AtraShared.ConstantsAndEnums;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AtraCore.HarmonyPatches;

[HarmonyPatch]
internal static class DrawPrismatic
{
    internal static SortedList<ItemTypeEnum, Dictionary<int, Lazy<Texture2D>>> PrismaticMasks = new();
    internal static SortedList<ItemTypeEnum, HashSet<int>> PrismaticFull = new();

    /// <summary>
    /// Load the prismatic data.
    /// Called on SaveLoaded.
    /// </summary>
    internal static void LoadPrismaticData()
    {
        List<DrawPrismaticModel>? models = AssetManager.GetPrismaticModels();
        if (models is null)
        {
            return;
        }

        PrismaticFull.Clear();
        PrismaticMasks.Clear();

        foreach (DrawPrismaticModel? model in models)
        {
            if (!int.TryParse(model.Identifier, out int id))
            {
                id = DataToItemMap.GetID(model.itemType, model.Identifier);
                if (id == -1)
                {
                    ModEntry.ModMonitor.Log($"Could not resolve", LogLevel.Warn);
                    continue;
                }
            }

            // Handle the full prismatics.
            if (string.IsNullOrWhiteSpace(model.Mask))
            {
                if (!PrismaticFull.TryGetValue(model.itemType, out HashSet<int>? set))
                {
                    set = new();
                }
                set.Add(id);
                PrismaticFull[model.itemType] = set;
            }
            else
            {
                if (!PrismaticMasks.TryGetValue(model.itemType, out var masks))
                {
                    masks = new();
                }
                if (!masks.TryAdd(id, new(() => Game1.content.Load<Texture2D>(model.Mask))))
                {
                    ModEntry.ModMonitor.Log($"{model.itemType} - {model.Identifier} appears to be a duplicate, ignoring", LogLevel.Warn);
                }
                PrismaticMasks[model.itemType] = masks;
            }
        }
    }

    /// <summary>
    /// Prefixes SObject's draw functions in order to draw things prismatically.
    /// </summary>
    /// <param name="__instance">SObject instance.</param>
    /// <param name="color">Color to make things.</param>
    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SObject), "drawInMenu")]
    private static void PrefixSObjectDraw(SObject __instance, ref Color color)
    {
        try
        {
            if (__instance.GetItemType() is ItemTypeEnum type && PrismaticFull.TryGetValue(type, out var set)
                && set.Contains(__instance.ParentSheetIndex))
            {
                color = Utility.GetPrismaticColor();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in drawing prismatic item\b\b{ex}", LogLevel.Error);
        }
        return;
    }
}
