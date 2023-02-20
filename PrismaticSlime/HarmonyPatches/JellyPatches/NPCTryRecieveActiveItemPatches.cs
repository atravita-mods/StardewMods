using AtraCore.Framework.DialogueManagement;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace PrismaticSlime.HarmonyPatches.JellyPatches;

[HarmonyPatch(typeof(NPC))]
internal static class NPCTryRecieveActiveItemPatches
{
    [HarmonyPatch(nameof(NPC.tryToReceiveActiveObject))]
    private static bool Prefix(NPC __instance, Farmer who)
    {
        if (Utility.IsNormalObjectAtParentSheetIndex(who.ActiveObject, ModEntry.PrismaticJelly)
            && !who.team.specialOrders.Any(order => order.questKey.Value == "Wizard2"))
        {
            try
            {
                QueuedDialogueManager.PushCurrentDialogueToQueue(__instance);
                switch (__instance.Name)
                {
                    case "Wizard":
                    {
                        BuffEnum buffEnum = BuffEnumExtensions.GetRandomBuff();
                        Buff buff = buffEnum.GetBuffOf(1, 700, "The Wizard's Gift", I18n.WizardGift());
                        buff.glow = Color.PaleVioletRed;
                        Game1.buffsDisplay.addOtherBuff(buff);

                        __instance.doEmote(Character.exclamationEmote);
                        Dialogue item = new(I18n.PrismaticJelly_Wizard(), __instance)
                        {
                            onFinish = () => who.Money += 2000,
                        };
                        __instance.CurrentDialogue.Push(item);
                        break;
                    }
                    case "Gus":
                    {
                        Dialogue item = new(I18n.PrismaticJelly_Gus(), __instance)
                        {
                            onFinish = () =>
                            {
                                DelayedAction.functionAfterDelay(
                                    () => who.addItemByMenuIfNecessaryElseHoldUp(new SObject(ModEntry.PrismaticJellyToast, 1)),
                                    200);
                            },
                        };
                        __instance.CurrentDialogue.Push(item);
                        break;
                    }
                    default:
                    {
                        if (!who.friendshipData.TryGetValue(__instance.Name, out Friendship? friendship))
                        {
                            friendship = new(0);
                            who.friendshipData[__instance.Name] = friendship;
                        }
                        else if (friendship.GiftsToday >= 1 || friendship.GiftsThisWeek >= 2 || friendship.IsDivorced())
                        {
                            return true;
                        }

                        // update friendship stats
                        friendship.GiftsToday++;
                        friendship.GiftsThisWeek++;
                        friendship.LastGiftDate = new(Game1.Date);
                        Game1.stats.GiftsGiven++;

                        who.friendshipData[__instance.Name] = friendship;

                        __instance.doEmote(Character.happyEmote);
                        bool isBirthday = Game1.dayOfMonth == __instance.Birthday_Day &&
                            __instance.Birthday_Season?.Equals(Game1.currentSeason, StringComparison.OrdinalIgnoreCase) == true;
                        who.changeFriendship(isBirthday ? 800 : 100, __instance);
                        if (!__instance.Dialogue.TryGetValue("PrismaticSlimeJelly.Response" + (isBirthday ? ".Birthday" : string.Empty), out string? response))
                        {
                            response = isBirthday
                                ? I18n.PrismaticJelly_Response_Birthday(__instance.displayName)
                                : I18n.PrismaticJelly_Response(__instance.displayName);
                        }
                        __instance.CurrentDialogue.Push(new Dialogue(response, __instance));
                        break;
                    }
                }
                who.reduceActiveItemByOne();
                who.currentLocation.localSound("give_gift");
                Game1.drawDialogue(__instance);
                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed while trying to override NPC.{nameof(NPC.tryToReceiveActiveObject)}\n\n{ex}", LogLevel.Error);
            }
        }

        return true;
    }
}
