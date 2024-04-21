namespace GingerIslandMainlandAdjustments.Niceties;

using AtraCore.Framework.Caches;

using AtraShared.Caching;
using AtraShared.Utils.Extensions;

using GingerIslandMainlandAdjustments.AssetManagers;
using GingerIslandMainlandAdjustments.MultiplayerHandler;

using Microsoft.Xna.Framework.Graphics;

using StardewValley.Objects;

/// <summary>
/// Handles Pam's phone call.
/// </summary>
internal sealed class PamPhoneHandler : IPhoneHandler
{
    private static readonly TickCache<bool> HasGottenPamMail = new(static () => Game1.player.mailReceived.Contains(AssetEditor.PAMMAILKEY));

    /// <inheritdoc />
    public string? CheckForIncomingCall(Random random) => null;

    /// <inheritdoc />
    public bool TryHandleIncomingCall(string callId, out Action? showDialogue)
    {
        showDialogue = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string>> GetOutgoingNumbers()
    {
        if (HasGottenPamMail.GetValue() && NPCCache.GetByVillagerName("Pam") is NPC pam)
        {
            yield return new KeyValuePair<string, string>("PamBus", pam.displayName);
        }
    }

    /// <inheritdoc />
    public bool TryHandleOutgoingCall(string callId)
    {
        if (callId != "PamBus")
        {
            return false;
        }

        GameLocation location = Game1.currentLocation;
        location.playShopPhoneNumberSounds("PamBus");
        Game1.player.freezePause = 4950;

        DelayedAction.functionAfterDelay(
            func: () =>
            {
                try
                {
                    Game1.playSound(GameLocation.PHONE_PICKUP_SOUND);
                    if (NPCCache.GetByVillagerName("Pam") is not NPC pam)
                    {
                        Globals.ModMonitor.Log($"Pam cannot be found, ending phone call.", LogLevel.Warn);
                    }
                    else if (Game1.timeOfDay > 2200)
                    {
                        Game1.DrawDialogue(pam, "Strings\\Characters:Pam_Bus_Late");
                    }
                    else if (Game1.timeOfDay < 900)
                    {
                        if (Game1.IsVisitingIslandToday(pam.Name))
                        {
                            Game1.DrawDialogue(pam, $"Strings\\Characters:Pam_Island_{Random.Shared.Next(1, 4)}");
                        }
                        else if (Utility.IsHospitalVisitDay(pam.Name))
                        {
                            Game1.DrawDialogue(pam, "Strings\\Characters:Pam_Doctor");
                        }
                        else if (MultiplayerSharedState.PamsSchedule is null)
                        {
                            Globals.ModMonitor.Log("Something very odd has happened. Pam's dayScheduleName is null", LogLevel.Debug);
                            Game1.DrawDialogue(pam, "Strings\\Characters:Pam_Other");
                        }
                        else if (MultiplayerSharedState.PamsSchedule.Contains("BusStop 21 10"))
                        {
                            Game1.DrawDialogue(pam, $"Strings\\Characters:Pam_Bus_{Random.Shared.Next(1, 4)}");
                        }
                        else
                        {
                            Game1.DrawDialogue(pam, "Strings\\Characters:Pam_Other");
                        }
                    }
                    else
                    {
                        if (Game1.IsVisitingIslandToday(pam.Name))
                        {
                            Game1.DrawDialogue(new Dialogue(pam, "Strings\\Characters:Pam_Voicemail_Island")
                            {
                                overridePortrait = Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"),
                            });
                        }
                        else if (Utility.IsHospitalVisitDay(pam.Name))
                        {
                            Game1.DrawDialogue(new Dialogue(pam, "Strings\\Characters:Pam_Voicemail_Doctor")
                            {
                                overridePortrait = Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"),
                            });
                        }
                        else if (MultiplayerSharedState.PamsSchedule is null)
                        {
                            Globals.ModMonitor.Log("Something very odd has happened. Pam's dayScheduleName is not found?", LogLevel.Debug);
                            Game1.DrawDialogue(new Dialogue(pam, "Strings\\Characters:Pam_Voicemail_Other")
                            {
                                overridePortrait = Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"),
                            });
                        }
                        else if (MultiplayerSharedState.PamsSchedule.Contains("BusStop 21 10"))
                        {
                            Game1.DrawDialogue(new Dialogue(pam, "Strings\\Characters:Pam_Voicemail_Bus")
                            {
                                overridePortrait = Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"),
                            });
                        }
                        else
                        {
                            Game1.DrawDialogue(new Dialogue(pam, "Strings\\Characters:Pam_Voicemail_Other")
                            {
                                overridePortrait = Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine"),
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Globals.ModMonitor.LogError("handling Pam's phone call", ex);
                }
            },
            delay: 4950);
        return true;
    }
}
