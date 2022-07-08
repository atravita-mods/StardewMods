using System.Linq.Expressions;
using System.Reflection;

namespace FrameRateLogger;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private Func<FrameRateCounter, int>? framerateGetter { get; set; } = null!;

    private FrameRateCounter? frameRateCounter { get; set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
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
            this.Monitor.Log($"Current framerate on {Game1.ticks} is {value}", value < 30 ? LogLevel.Alert : LogLevel.Trace);
        }
    }

    private void OnRenderedHud(object? sender, StardewModdingAPI.Events.RenderedHudEventArgs e)
    {
        this.frameRateCounter?.Update(Game1.currentGameTime);
        this.frameRateCounter?.Draw(Game1.currentGameTime);
    }
}
