using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

namespace SinZsEventTester.Framework;
internal sealed class MonitorPerformance : IDisposable
{
    private IGameLoopEvents gameLoopEvents;
    private IDisplayEvents displayEvents;
    private IMonitor monitor;

    private int frames = 0;
    private string framerate = "--";

    private string RenderTime = "--";
    private string UpdateTime = "--";

    private readonly Stopwatch renderWatch = new();
    private readonly Stopwatch updateWatch = new();

    private readonly float renderWidth;
    private readonly float updateWidth;

    public MonitorPerformance(IGameLoopEvents gameLoopEvents, IDisplayEvents displayEvents, IMonitor monitor)
    {
        this.gameLoopEvents = gameLoopEvents;
        this.displayEvents = displayEvents;
        this.monitor = monitor;

        this.gameLoopEvents.UpdateTicked += this.UpdateTicked;
        this.gameLoopEvents.UpdateTicking += this.UpdateTicking;

        this.displayEvents.Rendering += this.Rendering;
        this.displayEvents.Rendered += this.Rendered;

        this.displayEvents.RenderedHud += this.RenderedHud;

        this.renderWidth = Game1.dialogueFont.MeasureString($"Render time: {0:00.00} ms.").X + 4;
        this.updateWidth = Game1.dialogueFont.MeasureString($"Update time: {0:00.00} ms.").X + 4;
    }

    internal bool IsDisposed { get; private set; }

    private void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            if (disposing)
            {
                this.gameLoopEvents.UpdateTicked -= this.UpdateTicked;
                this.gameLoopEvents.UpdateTicking -= this.UpdateTicking;

                this.displayEvents.Rendering -= this.Rendering;
                this.displayEvents.Rendered -= this.Rendered;

                this.displayEvents.RenderedHud -= this.RenderedHud;
            }

            this.gameLoopEvents = null!;
            this.displayEvents = null!;
            this.monitor = null!;

            this.IsDisposed = true;
        }
    }

    private void RenderedHud(object? sender, RenderedHudEventArgs e)
    {
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, 64), Color.Black * 0.5f);

        var drawloc = Vector2.One * 12;
        e.SpriteBatch.DrawString(Game1.dialogueFont, this.RenderTime, drawloc, Color.White);

        drawloc.X += this.renderWidth;
        e.SpriteBatch.DrawString(Game1.dialogueFont, this.UpdateTime, drawloc, Color.White);

        drawloc.X += this.updateWidth;
        e.SpriteBatch.DrawString(Game1.dialogueFont, this.framerate, drawloc, Color.White);
    }

    [EventPriority((EventPriority)int.MinValue)]
    private void Rendering(object? sender, RenderingEventArgs e)
    {
        this.renderWatch.Restart();
    }

    [EventPriority((EventPriority)int.MaxValue)]
    private void Rendered(object? sender, RenderedEventArgs e)
    {
        this.renderWatch.Stop();
        var ms = this.renderWatch.Elapsed.TotalMilliseconds;
        if (Game1.ticks % 5 == 0 || ms > 5)
        {
            this.RenderTime = $"Render time: {ms:00.00} ms.";
        }

        this.frames += 1;
    }

    [EventPriority((EventPriority)int.MinValue)]
    private void UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        this.updateWatch.Restart();
    }

    [EventPriority((EventPriority)int.MaxValue)]
    private void UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (e.IsOneSecond)
        {
            this.framerate = $"Framerate: {this.frames} Hz.";
            this.frames = 0;
        }

        this.updateWatch.Stop();
        var ms = this.updateWatch.Elapsed.TotalMilliseconds;
        if (Game1.ticks % 5 == 0 || ms > 5)
        {
            this.UpdateTime = $"Update time: {ms:00.00} ms.";
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
