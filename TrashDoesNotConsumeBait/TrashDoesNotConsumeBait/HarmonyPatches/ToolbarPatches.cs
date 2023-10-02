namespace TrashDoesNotConsumeBait.HarmonyPatches;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Menus;
using StardewValley.Tools;

/// <summary>
/// Class that holds patches against the tool bar.
/// </summary>
[HarmonyPatch(typeof(Toolbar))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ToolbarPatches
{
    /***********
     * ATTENTION: Before you try refactoring this, look at how farmer.ActiveObject works.
     * Cuz it's real weird.
     * *********/
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Toolbar.receiveRightClick))]
    private static void PostfixRightClick(List<ClickableComponent> ___buttons, int x, int y)
    {
        try
        {
            if (Game1.player.UsingTool || Game1.IsChatting
                || (Game1.player.ActiveObject?.Category is not SObject.baitCategory && Game1.player.ActiveObject?.Category is not SObject.tackleCategory))
            {
                return;
            }
            foreach (ClickableComponent button in ___buttons)
            {
                if (button.containsPoint(x, y) && int.TryParse(button.name, out int val) && Game1.player.Items[val] is FishingRod fishingRod)
                {
                    SObject activeObj = Game1.player.ActiveObject;
                    switch (activeObj.Category)
                    {
                        case SObject.baitCategory:
                            if (fishingRod.UpgradeLevel >= 2)
                            {
                                SObject? prev = fishingRod.attachments[0];

                                if (prev is not null && prev.canStackWith(activeObj))
                                {
                                    prev.Stack = activeObj.addToStack(prev);
                                    if (prev.Stack <= 0)
                                    {
                                        prev = null;
                                    }
                                }

                                // setting the ActiveObject to an item adds it to inventory. If prev is null, ActiveObject is just removed from the inventory
                                Game1.player.ActiveObject = prev;
                                fishingRod.attachments[0] = activeObj;
                            }
                            return;
                        case SObject.tackleCategory:
                            if (fishingRod.UpgradeLevel >= 3)
                            {
                                Game1.player.ActiveObject = fishingRod.attachments[1];
                                fishingRod.attachments[1] = activeObj;
                            }
                            return;
                        default:
                            return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("swapping out bait or tackle", ex);
        }
    }
}