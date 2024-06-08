namespace TrashDoesNotConsumeBait.HarmonyPatches;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;

/// <summary>
/// Class that holds patches against the treasure menu.
/// </summary>
[HarmonyPatch(typeof(FishingRod))]
internal static class TreasureMenuPatches
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(FishingRod.openTreasureMenuEndFunction))]
    private static void Postfix()
    {
        if (Game1.activeClickableMenu is not ItemGrabMenu itemGrab || itemGrab.source != ItemGrabMenu.source_fishingChest || !ModEntry.Config.EmptyFishingChests)
        {
            return;
        }

        Vector2 init_pos = Game1.player.Position + new Vector2(0, -196f);
        try
        {
            for (int i = itemGrab.ItemsToGrabMenu.actualInventory.Count - 1; i >= 0; i--)
            {
                Item? item = itemGrab.ItemsToGrabMenu.actualInventory[i];
                if (item is null)
                {
                    itemGrab.ItemsToGrabMenu.actualInventory.RemoveAt(i);
                    continue;
                }

                int original_stack = item.Stack;
                Item? remainder = item;

                // try to equip if possible.
                if (Game1.player.CurrentTool is FishingRod rod && item is SObject obj)
                {
                    SObject? oldAttach = rod.attach(obj);
                    if (oldAttach is not null)
                    {
                        remainder = rod.attach(oldAttach);
                    }
                    else
                    {
                        remainder = null;
                    }

                    int addedNumber = original_stack - (remainder?.Stack ?? 0);
                    if (addedNumber > 0)
                    {
                        Game1.player.OnItemReceived(item, addedNumber, oldAttach, true);

                    }
                }

                remainder = Game1.player.addItemToInventory(remainder);
                if (remainder is null)
                {
                    itemGrab.ItemsToGrabMenu.actualInventory.RemoveAt(i);
                }
                else
                {
                    itemGrab.ItemsToGrabMenu.actualInventory[i] = remainder;
                }

                if (remainder is null || remainder.Stack < original_stack)
                {
                    int count = original_stack - (remainder?.Stack ?? 0);
                    Game1.addHUDMessage(HUDMessage.ForItemGained(item, count));
                    if (Game1.currentLocation is { } loc)
                    {
                        ParsedItemData parsed = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
                        if (parsed is not null)
                        {
                            string texture = parsed.TextureName;
                            Rectangle sourceRect = parsed.GetSourceRect();

                            for (int j = 0; j < count; j++)
                            {
                                TemporaryAnimatedSprite temporaryAnimatedSprite = new(
                                    texture,
                                    sourceRect,
                                    600,
                                    1,
                                    0,
                                    init_pos,
                                    flicker: false,
                                    flipped: false,
                                    MathF.BitIncrement((float)init_pos.Y / 10000),
                                    0.01f,
                                    item is ColoredObject colored ? colored.color.Value : Color.White,
                                    Game1.pixelZoom,
                                    0.01f,
                                    Random.Shared.NextBool() ? 0.02f : 0.02f,
                                    Random.Shared.NextBool() ? 0.02f : 0.02f)
                                {
                                    motion = new Vector2(Game1.random.Next(-30, 42) / 10f, Game1.random.Next(-9, -3)),
                                    acceleration = new Vector2(0f, 0.5f),
                                };

                                loc.temporarySprites.Add(temporaryAnimatedSprite);
                            }
                        }
                    }
                }
            }

            if (itemGrab.areAllItemsTaken())
            {
                itemGrab.exitThisMenuNoSound();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"moving items from treasure chest to inventory", ex);
        }
    }
}