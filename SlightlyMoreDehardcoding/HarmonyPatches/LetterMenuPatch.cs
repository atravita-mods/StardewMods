using HarmonyLib;

using Microsoft.Xna.Framework;

using SlightlyMoreDehardcoding.Framework;

using StardewValley.Menus;

namespace SlightlyMoreDehardcoding.HarmonyPatches;

/// <summary>
/// A patch on lettermenu to add items.
/// </summary>
internal static class LetterMenuPatch
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(LetterViewerMenu), nameof(LetterViewerMenu.HandleItemCommand)),
            prefix: new HarmonyMethod(typeof(LetterMenuPatch), nameof(Postfix)));
    }

    private static void Postfix(LetterViewerMenu __instance)
    {
        if (__instance.isFromCollection || AssetManager.GetExtendedMailData(__instance.mailTitle) is not { } data)
        {
            return;
        }

        foreach (var item in data.Get(Game1.currentLocation, Game1.player, __instance.mailTitle))
        {
            if (item.Item is not Item realItem)
            {
                continue;
            }

            __instance.itemsToGrab.Add(new ClickableComponent(
                new Rectangle(__instance.xPositionOnScreen + (__instance.width / 2) - 48, __instance.yPositionOnScreen + __instance.height - 32 - 96, 96, 96),
                realItem)
            {
                myID = LetterViewerMenu.region_itemGrabButton,
                leftNeighborID = 101,
                rightNeighborID = 102,
            });
        }
    }
}
