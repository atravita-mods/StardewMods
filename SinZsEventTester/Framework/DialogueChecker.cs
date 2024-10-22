using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace SinZsEventTester.Framework;

/// <summary>
/// Warps farmer around, talking to each npc and taking note of their dialogue.
/// </summary>
internal sealed class DialogueChecker : IDisposable
{
    private IMonitor monitor;
    private IGameLoopEvents gameLoopEvents;

    private Stack<(NPC, string?)> stack = [];

    private string? lastShownDialogue;

    protected static IReflectedField<ScreenFade?>? _fade = null!;
    private int iterationsToSkip;
    private Dialogue? currentDialogue;
    private NPC? currentNPC;

    internal static void Init(IReflectionHelper reflector)
    {
        _fade = reflector.GetField<ScreenFade?>(typeof(Game1), "screenFade", false);
    }

    internal DialogueChecker(IMonitor monitor, IGameLoopEvents gameLoopEvents, Span<string> args)
    {
        this.monitor = monitor;
        this.gameLoopEvents = gameLoopEvents;

        this.gameLoopEvents.UpdateTicked += this.OnTick;

        if (args.Length == 0 || (args.Length == 1 && args[0].Equals("today", StringComparison.OrdinalIgnoreCase)))
        {
            Utility.ForEachVillager((npc) =>
            {
                // check for new current dialogue.
                int friendship = Game1.player.friendshipData.TryGetValue(npc.Name, out var data) ? data.Points : 0;
                _ = npc.checkForNewCurrentDialogue(friendship / 250) || npc.checkForNewCurrentDialogue(friendship / 250, true) || npc.setTemporaryMessages(Game1.player);

                if (npc.CurrentDialogue?.Count > 0)
                {
                    this.stack.Push((npc, null));
                }
                return true;
            });
        }
        else
        {
            foreach (string name in args)
            {
                if (Utility.fuzzyCharacterSearch(name) is not NPC npc)
                {
                    this.monitor.Log($"{name} does not seem to be a valid NPC!", LogLevel.Warn);
                    continue;
                }

                if (npc.CurrentDialogue?.TryPeek(out Dialogue? current) == true && !current.isOnFinalDialogue())
                {
                    this.stack.Push((npc, null));
                }

                foreach (string key in npc.Dialogue.Keys)
                {
                    this.stack.Push((npc, key));
                }
            }
        }

        this.monitor.Log($"Okay, {this.stack.Count} dialogues queued.");
    }

    private void OnTick(object? sender, UpdateTickedEventArgs e)
    {
        // run at six times per second.
        if (!e.IsMultipleOf(10))
        {
            return;
        }

        if (Game1.CurrentEvent is { } evt)
        {
            evt.skipped = true;
            evt.skipEvent();
            Game1.eventFinished();
            return;
        }

        // sneakily advancing the screen fade to make things "appear" to go faster.
        if (Game1.globalFade)
        {
            ScreenFade? fade = _fade?.GetValue();
            if (fade?.fadeIn == true)
            {
                fade.fadeToBlackAlpha = Math.Max(0f, fade.fadeToBlackAlpha - (fade.globalFadeSpeed * 5));
            }
        }

        if (Game1.activeClickableMenu is DialogueBox db)
        {
            if (db.safetyTimer > 0)
            {
                db.SpeedUp();
                return;
            }

            // log last dialogue.
            string s = db.getCurrentString();
            if (s != this.lastShownDialogue)
            {
                this.monitor.Log($"{db.characterDialogue?.speaker?.displayName ?? "None"}: {s}", LogLevel.Info);
                this.lastShownDialogue = s;
            }

            // click dialogue
            if (db.responses?.Length > 0)
            {
                db.selectedResponse = Random.Shared.Next(db.responses.Length);
            }

            db.receiveLeftClick(0, 0);

            return;
        }

        this.lastShownDialogue = null;

        if (this.iterationsToSkip > 0)
        {
            this.iterationsToSkip--;
        }

        if (!Context.IsPlayerFree || Game1.actionsWhenPlayerFree?.Count > 0 || this.iterationsToSkip > 0)
        {
            return;
        }

        IMonitor monitor = this.monitor;

        // check to see if dialogue is done.
        if (this.currentNPC?.CurrentDialogue?.TryPeek(out Dialogue? d) == true
            && ReferenceEquals(d, this.currentDialogue) && d.currentDialogueIndex > 0 && !d.isOnFinalDialogue())
        {
            monitor.VerboseLog($"Continuation of {d.TranslationKey ?? "no translation key"} - {d.currentDialogueIndex}.");
            Game1.drawDialogue(this.currentNPC);
            if (Game1.activeClickableMenu is DialogueBox)
            {
                return;
            }
        }

        // queue next npc.
        if (this.stack?.TryPop(out (NPC, string?) next) == true)
        {
            monitor.Log($"{this.stack.Count + 1} dialogues remaining");
            (NPC npc, string? dialogue) = next;

            GameLocation nextLocation = npc.currentLocation;

            if (nextLocation?.Name == "Club" && !Game1.player.hasClubCard)
            {
                // player does not have club card, will not be allowed to warp in.
                return;
            }

            this.iterationsToSkip = 10;
            if (nextLocation is not null)
            {
                Point pos = npc.TilePoint;
                switch (npc.FacingDirection)
                {
                    case Game1.up:
                        pos.Y--;
                        break;
                    case Game1.down:
                        pos.Y++;
                        break;
                    case Game1.left:
                        pos.X--;
                        break;
                    case Game1.right:
                        pos.X++;
                        break;
                }

                if (dialogue is not null && npc.TryGetDialogue(dialogue) is { } nextDialogue)
                {
                    npc.setNewDialogue(nextDialogue);
                }

                if (!npc.CurrentDialogue.TryPeek(out this.currentDialogue))
                {
                    return;
                }
                this.currentNPC = npc;

                void ActivateDialogue()
                {
                    this.iterationsToSkip = 0;
                    Game1.drawDialogue(npc);
                    if (Game1.activeClickableMenu is DialogueBox boxen && boxen?.characterDialogue?.TranslationKey is { } translation)
                    {
                        monitor.Log($"Translation key {translation} loaded", LogLevel.Debug);
                    }
                }

                Game1.PerformActionWhenPlayerFree(() =>
                {
                    if (nextLocation != Game1.currentLocation)
                    {
                        LocationRequest context = Game1.getLocationRequest(nextLocation.Name);
                        context.OnLoad += ActivateDialogue;
                        Game1.warpFarmer(context, pos.X, pos.Y, (npc.FacingDirection + 2) % 4);
                    }
                    else
                    {
                        Game1.player.Position = pos.ToVector2() * 64f;
                        Game1.player.faceDirection((npc.FacingDirection + 2) % 4);
                        ActivateDialogue();
                    }
                });
            }

            return;
        }

        if (Game1.activeClickableMenu is null && this.stack?.Count is 0 or null && Game1.actionsWhenPlayerFree?.Count is 0 or null)
        {
            this.monitor.Log($"Okay! Dialogue check done", LogLevel.Info);
            this.Dispose();
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not this instance is disposed.
    /// </summary>
    internal bool IsDisposed {get; private set; }

    private void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            if (disposing)
            {
                this.gameLoopEvents.UpdateTicked -= this.OnTick;
            }

            this.stack = null!;
            this.lastShownDialogue = null!;

            this.gameLoopEvents = null!;
            this.monitor = null!;
            this.IsDisposed = true;

            this.currentDialogue = null;
            this.currentNPC = null;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
