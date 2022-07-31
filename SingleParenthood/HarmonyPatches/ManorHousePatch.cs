using AtraShared.Menuing;
using HarmonyLib;
using StardewValley.Locations;

namespace SingleParenthood.HarmonyPatches;

/// <summary>
/// Patches manor house to adjust the divorce book action.
/// </summary>
[HarmonyPatch(typeof(ManorHouse))]
internal static class ManorHousePatch
{
    private static void ChildMenu()
    {
        List<Response> responses = new()
        {
            new("HaveChild", "Have kid"),
            new("NoChild", "Nah"),
        };

        List<Action?> actions = new()
        {
            () =>
            {
                Game1.player.modData[ModEntry.countUp] = "0";
            },
        };
    }

    [HarmonyPatch(nameof(ManorHouse.performAction))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static bool Prefix(ManorHouse __instance, string action)
    {
        if (action is "DivorceBook"
            && Utility.getHomeOfFarmer(Game1.player) is FarmHouse house
            && house.upgradeLevel >= 2
            && Game1.player.modData.ContainsKey(ModEntry.countUp)
            && Game1.player.getChildrenCount() < ModEntry.Config.MaxKids
            && Game1.player.AllKidsOutOfCrib())
        {
            if (Game1.player.divorceTonight.Value)
            {
                return true;
            }
            try
            {
                if (Game1.player.isMarried())
                {
                    List<Response> responses = new()
                    {
                        new("atravita.child", "child"),
                        new("atravita.divorce", "Get Divorced"),
                        new("atravita.close", "Close Menu"),
                    };

                    List<Action?> actions = new()
                    {
                        ChildMenu,
                        () =>
                        {
                            string s = Game1.player.hasCurrentOrPendingRoommate()
                                ? Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Question_Krobus", Game1.player.getSpouse().displayName)
                                : Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Question");
                            __instance.createQuestionDialogue(s, __instance.createYesNoResponses(), "divorce");
                        },
                    };

                    Game1.activeClickableMenu = new DialogueAndAction("atravita.marriedChildMenu", responses, actions);
                }
                else
                {
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_NoSpouse"));
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed while overriding divorce book.\n\n{ex}", LogLevel.Error);
            }
        }
        return true;
    }
}
