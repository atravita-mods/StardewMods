using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace PrismaticSlime.HarmonyPatches.JellyPatches;

/// <summary>
/// A patch to handle NPCs receiving the prismatic slime jelly.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
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
                switch (__instance.Name)
                {
                    case "Wizard":
                    {
                        BuffEnum buffEnum = BuffEnumExtensions.GetRandomBuff();
                        Buff buff = buffEnum.GetBuffOf(1, 700, "The Wizard's Gift", I18n.WizardGift());
                        buff.glow = Color.PaleVioletRed;
                        Game1.player.applyBuff(buff);

                        __instance.doEmote(Character.exclamationEmote);
                        Dialogue item = new(__instance, null, I18n.PrismaticJelly_Wizard())
                        {
                            onFinish = () => who.Money += 2000,
                        };
                        __instance.CurrentDialogue.Push(item);
                        break;
                    }
                    case "Gus":
                    {
                        Dialogue item = new(__instance, null, I18n.PrismaticJelly_Gus())
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
                    case "Emily":
                    {
                        if (Game1.player.modData?.GetInt(DyePotPatches.ModData) is > 0)
                        {
                            goto default;
                        }
                        __instance.CurrentDialogue.Push(new(__instance, null, I18n.PrismaticJelly_Emily()));
                        Game1.player.modData?.SetInt(DyePotPatches.ModData, 10);
                        break;
                    }
                    default: // treat as a gift.
                    {
                        bool isBirthday = Game1.dayOfMonth == __instance.Birthday_Day &&
                            __instance.Birthday_Season?.Equals(Game1.currentSeason, StringComparison.OrdinalIgnoreCase) == true;
                        if (!who.friendshipData.TryGetValue(__instance.Name, out Friendship? friendship))
                        {
                            friendship = new(0);
                            who.friendshipData[__instance.Name] = friendship;
                        }
                        else if (friendship.IsDivorced())
                        {
                            __instance.CurrentDialogue.Push(new Dialogue(__instance, "Strings\\Characters:Divorced_gift"));
                            Game1.drawDialogue(__instance);
                            return false;
                        }
                        else if (friendship.GiftsToday >= 1)
                        {
                            Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3981", __instance.displayName)));
                            return false;
                        }
                        else if (!isBirthday && friendship.GiftsThisWeek >= 2 && __instance.getSpouse() != who)
                        {
                            Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3987", __instance.displayName, 2)));
                            return false;
                        }

                        // update friendship stats
                        friendship.GiftsToday++;
                        friendship.GiftsThisWeek++;
                        friendship.LastGiftDate = new(Game1.Date);
                        Game1.stats.GiftsGiven++;

                        __instance.doEmote(Character.happyEmote);
                        who.changeFriendship(isBirthday ? 800 : 100, __instance);
                        Dialogue response = __instance.TryGetDialogue("PrismaticSlimeJelly.Response" + (isBirthday ? ".Birthday" : string.Empty))
                            ?? new Dialogue(__instance, null, isBirthday ? I18n.PrismaticJelly_Response_Birthday(__instance.displayName) : I18n.PrismaticJelly_Response(__instance.displayName));
                        __instance.CurrentDialogue.Push(response);
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
                ModEntry.ModMonitor.LogError($"overriding NPC.{nameof(NPC.tryToReceiveActiveObject)}", ex);
            }
        }

        return true;
    }
}
