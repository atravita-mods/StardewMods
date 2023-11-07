namespace LastDayToPlantRedux.Framework;

using System.Diagnostics;

using StardewValley.TerrainFeatures;


/// <summary>
/// A subclass of Hoedirt that nixes all the location specific stuff.
/// Also nixes a few other methods.
/// </summary>
[DebuggerDisplay("fertilizer {fertilizer.Value} near water {nearWaterForPaddy.Value}")]
internal class DummyHoeDirt : HoeDirt
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DummyHoeDirt"/> class.
    /// </summary>
    /// <param name="fertilizer">Fertilizer to use.</param>
    public DummyHoeDirt(string? fertilizer)
        : base(HoeDirt.dry, Game1.getFarm())
    {
        this.fertilizer.Value = fertilizer;
    }

    /// <summary>
    /// nop'ed.
    /// </summary>
    public override void dayUpdate()
    {
    }

    /// <summary>
    /// nop'ed.
    /// </summary>
    /// <param name="onLoad">irrelevant.</param>
    /// <returns>always false.</returns>
    public override bool seasonUpdate(bool onLoad) => false;

    /// <summary>
    /// Calculates how long it takes for a specific crop to grow with a given farmer.
    /// </summary>
    /// <param name="who">Farmer to check.</param>
    /// <returns>Number of days it takes to grow.</returns>
    internal int? CalculateTimings(Farmer who)
    {
        this.applySpeedIncreases(who);
        int ret = 0;
        for(int i = 0; i < this.crop.phaseDays.Count - 1; i++)
        {
            ret += this.crop.phaseDays[i];
        }
        return ret;
    }
}
