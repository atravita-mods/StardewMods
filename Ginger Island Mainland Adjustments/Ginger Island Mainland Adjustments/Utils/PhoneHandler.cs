using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Class that handles patches against GameLocation...to handle the phone.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal class PhoneHandler
{
    /// <summary>
    /// Prefix that lets me inject Pam into the phone menu.
    /// </summary>
    /// <param name="__instance">Game Location.</param>
    /// <param name="question">Question (displayed to player).</param>
    /// <param name="answerChoices">Responses.</param>
    /// <param name="dialogKey">Question key, used to keep track of which question set.</param>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    public static void PrefixQuestionDialogue(GameLocation __instance, string question, ref Response[] answerChoices, string dialogKey)
    {
        if (dialogKey.Equals("telephone", StringComparison.OrdinalIgnoreCase)
            && Game1.getAllFarmers().Any((Farmer f) => f.eventsSeen.Contains(503180)) // replace with something better than just her nine heart.
            && Game1.getCharacterFromName("Pam") is NPC pam // omit if Pam inexplicably vanished.
            && answerChoices.Any((Response r) => r.responseKey.Equals("Carpenter", StringComparison.OrdinalIgnoreCase)))
        {
            List<Response> responseList = new() { new Response("PamBus", pam.displayName) };
            responseList.AddRange(answerChoices);
            answerChoices = responseList.ToArray();
        }
    }

    /// <summary>
    /// Postfixing answerDialogueAction to handle Pam's phone calls.
    /// </summary>
    /// <param name="__instance">Location we're calling from.</param>
    /// <param name="__0">questionAndAnswer.</param>
    /// <param name="__1">questionParams[].</param>
    /// <param name="__result">Result.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameLocation.answerDialogueAction))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static void PostfixAnswerDialogueAction(GameLocation __instance, string __0, string[] __1, ref bool __result)
    {
        Globals.ModMonitor.DebugLog(__0);
        if (__0.Equals("telephone_PamBus"))
        {
            Globals.ReflectionHelper.GetMethod(__instance, "playShopPhoneNumberSounds").Invoke(__0);
            Game1.player.freezePause = 4950;
            DelayedAction.functionAfterDelay(
            () =>
            {
                try
                {
                    Game1.playSound("bigSelect");
                    NPC? pam = Game1.getCharacterFromName("Pam");
                    if (pam is null)
                    {
                        Globals.ModMonitor.Log($"Pam cannot be found, ending phone call.", LogLevel.Warn);
                        return;
                    }
                    if (Game1.timeOfDay > 2200)
                    {
                        Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Bus_Late"));
                        return;
                    }
                    if (Game1.timeOfDay < 900)
                    {
                        if (Game1.IsVisitingIslandToday(pam.Name))
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString($"Strings\\Characters:Pam_Island_{Game1.random.Next(1, 4)}"));
                        }
                        else if (Utility.IsHospitalVisitDay(pam.Name))
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Doctor"));
                        }
                        else if (pam.hasMasterScheduleEntry(pam.dayScheduleName.Value) && pam.getMasterScheduleEntry(pam.dayScheduleName.Value).Contains("BusStop 11 10"))
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString($"Strings\\Characters:Pam_Bus_{Game1.random.Next(1, 4)}"));
                        }
                        else
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Other"));
                        }
                    }
                    else
                    {
                        if (Game1.IsVisitingIslandToday(pam.Name))
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Voicemail_Island"), Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"));
                        }
                        else if (Utility.IsHospitalVisitDay(pam.Name))
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Voicemail_Doctor"));
                        }
                        else if (pam.hasMasterScheduleEntry(pam.dayScheduleName.Value) && pam.getMasterScheduleEntry(pam.dayScheduleName.Value).Contains("BusStop 11 10"))
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Voicemail_Bus"), Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"));
                        }
                        else
                        {
                            Game1.drawDialogue(pam, Game1.content.LoadString("Strings\\Characters:Pam_Voicemail_Other"), Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"));
                        }
                    }
                    __instance.answerDialogueAction("HangUp", Array.Empty<string>());
                }
                catch (Exception ex)
                {
                    Globals.ModMonitor.Log($"Error handling Pam's phone call {ex}", LogLevel.Error);
                }
            },
            4950);
            __result = true;
        }
    }
}