using System.Linq.Expressions;
using System.Reflection;

namespace FrameRateLogger;

/// <inheritdoc />
internal class ModEntry : Mod
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    private static IMonitor ModMonitor { get; set; }

    private Func<FrameRateCounter, int>? framerateGetter { get; set; }

    private FrameRateCounter? frameRateCounter { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        this.frameRateCounter = new(GameRunner.instance);
        helper.Reflection.GetMethod(this.frameRateCounter, "LoadContent").Invoke();
        FieldInfo? field = helper.Reflection.GetField<int>(this.frameRateCounter, "frameRate").FieldInfo;

        ParameterExpression? objparam = Expression.Parameter(typeof(FrameRateCounter), "obj");
        MemberExpression? fieldgetter = Expression.Field(objparam, field);
        this.framerateGetter = Expression.Lambda<Func<FrameRateCounter, int>>(fieldgetter, objparam).Compile();

        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.ReturnedToTitle;
    }

    private void ReturnedToTitle(object? sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e)
        => this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.OnUpdateTicked;

    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        => this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.OnUpdateTicked;

    private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
    {
        if (this.frameRateCounter is not null && this.framerateGetter?.Invoke(this.frameRateCounter) is int value)
        {
            this.Monitor.Log($"Current framerate on {Game1.ticks} is {value}", value < 30 ? LogLevel.Alert : LogLevel.Info);
        }
    }

    private void OnRenderedHud(object? sender, StardewModdingAPI.Events.RenderedHudEventArgs e)
    {
        this.frameRateCounter?.Update(Game1.currentGameTime);
        this.frameRateCounter?.Draw(Game1.currentGameTime);
    }
}
