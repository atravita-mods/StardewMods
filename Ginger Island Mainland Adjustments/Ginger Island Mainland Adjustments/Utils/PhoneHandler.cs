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
    public static void PrefixQuestionDialogue(GameLocation __instance, string question, ref Response[] answerChoices, string dialogKey)
    {
        if (dialogKey.Equals("telephone", StringComparison.OrdinalIgnoreCase)
            && answerChoices.Any((Response r) => r.responseKey.Equals("Carpenter", StringComparison.OrdinalIgnoreCase)))
        {
            List<Response> responseList = new() { new Response("PamBus", Game1.getCharacterFromName("Pam")?.displayName ?? "Pam") };
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
            Globals.ModMonitor.DebugLog("Caught telephone_Pam");

            Globals.ReflectionHelper.GetMethod(__instance, "playShopPhoneNumberSounds").Invoke(__0);
            Game1.player.freezePause = 4950;
            DelayedAction.functionAfterDelay(
            () =>
            {
                Game1.playSound("bigSelect");
                NPC pam = Game1.getCharacterFromName("Pam");
                if (Game1.timeOfDay > 2200)
                {
                    Game1.drawDialogue(pam, I18n.PamBusLate());
                    return;
                }
                if (Game1.timeOfDay < 900)
                {
                    if (Game1.IsVisitingIslandToday("Pam"))
                    {
                        Game1.drawDialogue(pam, I18n.GetByKey($"Pam_Island_{Game1.random.Next(1, 4)}"));
                    }
                    else
                    {
                        Game1.drawDialogue(pam, I18n.GetByKey($"Pam_Bus_{Game1.random.Next(1, 4)}"));
                    }
                }
                else
                {
                    if (Game1.IsVisitingIslandToday("Pam"))
                    {
                        Game1.drawDialogue(pam, I18n.PamVoicemailIsland(), Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"));
                    }
                    else
                    {
                        Game1.drawDialogue(pam, I18n.PamVoicemailBus(), Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"));
                    }
                }
                __instance.answerDialogueAction("HangUp", Array.Empty<string>());
            },
            4950);
            __result = true;
        }
    }
}