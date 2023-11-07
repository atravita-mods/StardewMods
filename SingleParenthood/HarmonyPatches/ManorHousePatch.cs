using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using SingleParenthood.Framework;

using StardewValley.Locations;

namespace SingleParenthood.HarmonyPatches;

/// <summary>
/// Patches manor house to adjust the divorce book action.
/// </summary>
[HarmonyPatch(typeof(ManorHouse))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ManorHousePatch
{
    private static void ChildMenu()
    {
        List<Response> responses = new()
        {
            new("atravita.HaveChild", "Have kid"),
            new("atravita.NoChild", "Nah"),
        };

        List<Action?> actions = new()
        {
            () =>
            {
                Game1.player.modData[ModEntry.CountUp] = "0";

                if (Game1.player.hasCurrentOrPendingRoommate())
                {
                    Game1.player.modData.SetEnum(ModEntry.Relationship, RelationshipType.Roommates);
                }
                else if (Game1.player.IsMarried())
                {
                    Game1.player.modData.SetEnum(ModEntry.Relationship, RelationshipType.Married);
                }
                else
                {
                    Game1.player.modData.SetEnum(ModEntry.Relationship, RelationshipType.Single);
                }

                Game1.player.modData.SetEnum(ModEntry.Type, ParenthoodType.Adoption);
            },
        };
    }

    [HarmonyPatch(nameof(ManorHouse.performAction))]
    private static bool Prefix(ManorHouse __instance, string action)
    {
        if (action is "DivorceBook"
            && Utility.getHomeOfFarmer(Game1.player) is FarmHouse house
            && house.upgradeLevel >= 2
            && Game1.player.modData.ContainsKey(ModEntry.CountUp)
            && Game1.player.getChildrenCount() < ModEntry.Config.MaxKids
            && Game1.player.AllKidsOutOfCrib())
        {
            if (Game1.player.divorceTonight.Value)
            {
                return true;
            }
            try
            {
                if (Game1.player.IsMarried())
                {
                    Response[] responses = new Response[]
                    {
                        new("atravita.child", I18n.Adoption()),
                        new("atravita.divorce", I18n.Divorce()),
                        new("atravita.close", I18n.Leave()),
                    };

                    Action?[] actions = new[]
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

                    Game1.activeClickableMenu = new DialogueAndAction(I18n.Services(), responses, actions, ModEntry.InputHelper);
                }
                else
                {
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_NoSpouse"));
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("overriding divorce book", ex);
            }
        }
        return true;
    }
}
