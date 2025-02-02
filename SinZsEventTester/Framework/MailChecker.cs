using StardewModdingAPI.Events;

using StardewValley.Menus;

namespace SinZsEventTester.Framework;

internal sealed class MailChecker : IChecker
{
    private IMonitor monitor;
    private IGameLoopEvents gameLoopEvents;

    private Stack<(string, string)> mails = [];

    private int iterationsToSkip;

    internal MailChecker(IMonitor monitor, IGameLoopEvents gameLoopEvents)
    {
        this.monitor = monitor;
        this.gameLoopEvents = gameLoopEvents;

        var mail = DataLoader.Mail(Game1.content);
        foreach (var (key, value) in mail)
        {
            if (value.Contains('{'))
            {
                this.monitor.Log($"{key}-{value} is likely special, skipping.");
                continue;
            }

            this.mails.Push((key, value));
        }

        if (this.mails.Count > 0)
        {
            this.gameLoopEvents.UpdateTicked += this.OnTick;
        }
        else
        {
            this.Dispose();
        }
    }

    private void OnTick(object? sender, UpdateTickedEventArgs e)
    {
        // run at six times per second.
        if (!e.IsMultipleOf(10))
        {
            return;
        }

        if (this.iterationsToSkip > 0)
        {
            this.iterationsToSkip--;
            return;
        }

        if (Game1.CurrentEvent is { } evt)
        {
            evt.skipped = true;
            evt.skipEvent();
            Game1.eventFinished();
            return;
        }

        if (Game1.activeClickableMenu is LetterViewerMenu letter && ModEntry.Config.SkipDialogue)
        {
            if (letter.scale < 0.98f)
            {
                letter.scale = 0.98f;
                return;
            }

            ClickableComponent? clickable = null;
            if (letter.page < letter.mailMessage.Count - 1 && letter.forwardButton is { } forward)
            {
                clickable = forward;
            }
            else if (letter.itemsToGrab.Where(static item => item.item is not null).FirstOrDefault() is { } item)
            {
                clickable = item;
            }
            else if ((letter.questID is not null || letter.specialOrderId is not null) && letter.acceptQuestButton is { } quest)
            {
                clickable = quest;
            }
            else if (letter.readyToClose() && letter.upperRightCloseButton is { } close)
            {
                clickable = close;
            }

            var x = clickable?.bounds.Center.X ?? 0;
            var y = clickable?.bounds.Center.Y ?? 0;

            letter.receiveLeftClick(x, y, false);
            return;
        }

        Game1.activeClickableMenu?.emergencyShutDown();

        if (this.mails.TryPop(out var result))
        {
            var (key, value) = result;

            this.monitor?.Log($"Launching {key}");
            var letterMenu = new LetterViewerMenu(value, value.Split("[#]").ElementAtOrDefault(1) ?? "no-title", false);
            Game1.activeClickableMenu = letterMenu;

            this.monitor?.Log($"Message: {string.Join("\n\t", letterMenu.mailMessage)}");
            if (letterMenu.itemsLeftToGrab())
            {
                this.monitor?.Log($"Items: {string.Join(" ,", letterMenu.itemsToGrab.Select(item => item.item?.QualifiedItemId ?? "null item?" ))}");
            }

            if (letterMenu.HasQuestOrSpecialOrder)
            {
                this.monitor?.Log($"Quests: {letterMenu.questID} and {letterMenu.specialOrderId}");
            }
            this.iterationsToSkip = 6;
        }
        else
        {
            this.Dispose();
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not this instance is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    private void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            if (disposing)
            {
                this.gameLoopEvents.UpdateTicked -= this.OnTick;
            }

            this.monitor = null!;
            this.mails = null!;
            this.gameLoopEvents = null!;
            this.IsDisposed = true;
        }
    }


    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
