using AtraShared.Menuing;
using HarmonyLib;
using StardewValley.Locations;
using xTile.Dimensions;

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
            new("Have kid", "Have kid"),
            new("Nah", "Nah"),
        };

        List<Action?> actions = new()
        {
            () =>
            {
            },
        };
    }

    [HarmonyPatch(nameof(ManorHouse.performAction))]
    private static bool Prefix(ManorHouse __instance, string action)
    {
        if (action is "DivorceBook")
        {
            try
            {

                if (Game1.player.divorceTonight.Value)
                {
                    return true;
                }
                else if (Game1.player.isMarried())
                {
                    List<Response> responses = new()
                    {
                        new("atravita.child", "child"),
                        new("atravita.divorce", "Get Divorced"),
                        new("atravita.close", "Close Menu"),
                    };

                    List<Action?> actions = new()
                    {
                        () =>
                        {
                        
                        },
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
