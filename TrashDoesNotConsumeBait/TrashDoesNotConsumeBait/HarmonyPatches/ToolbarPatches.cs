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
                    if (ReferenceEquals(fishingRod, Game1.player.ActiveObject))
                    {
                        return;
                    }

                    SObject? activeObj = Game1.player.ActiveObject;
                    Game1.player.ActiveObject = fishingRod.attach(activeObj);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("swapping out bait or tackle", ex);
        }
    }
}