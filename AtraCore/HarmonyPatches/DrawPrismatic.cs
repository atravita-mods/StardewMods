using AtraCore.Framework.ItemManagement;
using AtraCore.Models;
using AtraShared.ConstantsAndEnums;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace AtraCore.HarmonyPatches;

#pragma warning disable SA1124 // Do not use regions. Reviewed.
/// <summary>
/// Draws things with a prismatic tint or overlay.
/// </summary>
[HarmonyPatch]
internal static class DrawPrismatic
{
    private static readonly SortedList<ItemTypeEnum, Dictionary<int, Lazy<Texture2D>>> PrismaticMasks = new();
    private static readonly SortedList<ItemTypeEnum, HashSet<int>> PrismaticFull = new();

#region LOADDATA
    /// <summary>
    /// Load the prismatic data.
    /// Called on SaveLoaded.
    /// </summary>
    internal static void LoadPrismaticData()
    {
        Dictionary<string, DrawPrismaticModel>? models = AssetManager.GetPrismaticModels();
        if (models is null)
        {
            return;
        }

        PrismaticFull.Clear();
        PrismaticMasks.Clear();

        foreach (DrawPrismaticModel? model in models.Values)
        {
            if (!int.TryParse(model.Identifier, out int id))
            {
                id = DataToItemMap.GetID(model.ItemType, model.Identifier);
                if (id == -1)
                {
                    ModEntry.ModMonitor.Log($"Could not resolve {model.ItemType}, {model.Identifier}, skipping.", LogLevel.Warn);
                    continue;
                }
            }

            // Handle the full prismatics.
            if (string.IsNullOrWhiteSpace(model.Mask))
            {
                if (!PrismaticFull.TryGetValue(model.ItemType, out HashSet<int>? set))
                {
                    set = new();
                }
                set.Add(id);
                PrismaticFull[model.ItemType] = set;
            }
            else
            {
                // handle the ones that have masks.
                if (!PrismaticMasks.TryGetValue(model.ItemType, out Dictionary<int, Lazy<Texture2D>>? masks))
                {
                    masks = new();
                }
                if (!masks.TryAdd(id, new(() => Game1.content.Load<Texture2D>(model.Mask))))
                {
                    ModEntry.ModMonitor.Log($"{model.ItemType} - {model.Identifier} appears to be a duplicate, ignoring", LogLevel.Warn);
                }
                PrismaticMasks[model.ItemType] = masks;
            }
        }
    }
#endregion

#region SOBJECT

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
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SObject), nameof(SObject.drawInMenu))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void PostfixSObjectDrawInMenu(
        SObject __instance,
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

#endregion

#region RING

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

    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.drawInMenu))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
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

#endregion
#pragma warning restore SA1124 // Do not use regions
}
