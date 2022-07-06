using AtraCore.Framework.ItemManagement;
using AtraCore.Models;
using AtraShared.ConstantsAndEnums;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

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
                    ModEntry.ModMonitor.Log($"Could not resolve {model.itemType}, {model.Identifier}, skipping.", LogLevel.Warn);
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
                // handle the ones that have masks.
                if (!PrismaticMasks.TryGetValue(model.itemType, out Dictionary<int, Lazy<Texture2D>>? masks))
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
    /// Prefixes SObject's drawInMenu function in order to draw things prismatically.
    /// </summary>
    /// <param name="__instance">SObject instance.</param>
    /// <param name="color">Color to make things.</param>
    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SObject), nameof(SObject.drawInMenu))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void PrefixSObjectDrawInMenu(SObject __instance, ref Color color)
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
            ModEntry.ModMonitor.Log($"Failed in drawing prismatic item\n\n{ex}", LogLevel.Error);
        }
        return;
    }

    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.drawInMenu))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void PrefixRingDrawInMenu(Ring __instance, ref Color color)
    {
        try
        {
            if (__instance.GetItemType() is ItemTypeEnum type && PrismaticFull.TryGetValue(type, out HashSet<int>? set)
                && set.Contains(__instance.ParentSheetIndex))
            {
                color = Utility.GetPrismaticColor();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in drawing prismatic ring\n\n{ex}", LogLevel.Error);
        }
        return;
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "<Pending>")]
    private static void PostfixRingDrawInMenu(
        Ring __instance,
        SpriteBatch spriteBatch,
        Vector2 location,
        float scaleSize,
        float transparency,
        float layerDepth)
    {
        try
        {
            if (__instance.GetItemType() is ItemTypeEnum type && PrismaticMasks.TryGetValue(type, out var masks)
                && masks.TryGetValue(__instance.ParentSheetIndex, out var texture))
            {
                spriteBatch.Draw(
                    texture: texture.Value,
                    position: location + (new Vector2(32f, 32f) * scaleSize),
                    sourceRectangle: new Rectangle(0, 0, 16, 16),
                    color: Utility.GetPrismaticColor() * transparency,
                    rotation: 0f,
                    origin: new Vector2(8f, 8f) * scaleSize,
                    scale: scaleSize * 4f,
                    effects: SpriteEffects.None,
                    layerDepth: layerDepth);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in drawing prismatic mask\n\n{ex}", LogLevel.Error);
        }
        return;
    }
}
