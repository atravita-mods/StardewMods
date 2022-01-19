using System.Diagnostics.CodeAnalysis;

namespace GingerIslandMainlandAdjustments;

/// <summary>
/// Enum for day of week, for the settings.
/// </summary>
public enum DayOfWeek
{
    /// <summary>
    /// Monday.
    /// </summary>
    Monday,

    /// <summary>
    /// Tuesday.
    /// </summary>
    Tuesday,

    /// <summary>
    /// Wednesday.
    /// </summary>
    Wednesday,

    /// <summary>
    /// Thursday.
    /// </summary>
    Thursday,

    /// <summary>
    /// Friday.
    /// </summary>
    Friday,

    /// <summary>
    /// Saturday.
    /// </summary>
    Saturday,

    /// <summary>
    /// Sunday.
    /// </summary>
    Sunday,

}

#pragma warning disable SA1201 // Elements should appear in the correct order. Fields appear close to their properties for this class.
/// <summary>
/// Configuration class for mod.
/// </summary>
public class ModConfig
{
    /// <summary>
    /// Default day for Gus's visit.
    /// </summary>
    public const DayOfWeek DEFAULT_GUS_VISIT_DAY = DayOfWeek.Tuesday;

    /// <summary>
    /// Gets or sets a value indicating whether EnforceGITiming is enabled.
    /// When enabled, rejects time points too close together.
    /// And warns for them.
    /// </summary>
    public bool EnforceGITiming { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether whether to use the game's GI scheduler
    /// or mine.
    /// </summary>
    public bool UseThisScheduler { get; set; } = false;

    /// <summary>
    /// Maximum number of people allowed on Ginger island.
    /// </summary>
    private int capacity = 6;

    /// <summary>
    /// Gets or sets the maximum number of people allowed on Ginger Island.
    /// </summary>
    public int Capacity
    {
        get => this.capacity;
        set => this.capacity = Math.Clamp(value, 0, 12);
    }

    /// <summary>
    /// Probability for a group of explorers to try to explore the rest of the island.
    /// </summary>
    private float explorerChance = 0.05f;

    /// <summary>
    /// Gets or sets the probability that the explorers will try to explore the rest of the island.
    /// </summary>
    public float ExplorerChance
    {
        get => this.explorerChance;
        set => this.explorerChance = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets which day of the week Gus should go to Ginger Island.
    /// </summary>
    public DayOfWeek GusDay { get; set; } = DEFAULT_GUS_VISIT_DAY;
    /// <summary>
    /// Probability Gus will go the resort on his assigned day of the week.
    /// </summary>
    private float gusChance = 0.5f;

    /// <summary>
    /// Gets or sets the probability GUS will go the resort on his assigned day of the week.
    /// </summary>
    public float GusChance
    {
        get => this.gusChance;
        set => this.gusChance = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Returns the enum value DayOfWeek as a short string.
    /// </summary>
    /// <returns>Short day of week string.</returns>
    public string GusDayAsShortString()
    {
        return this.GusDay switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => "Tue",
        };
    }

    /// <summary>
    /// Attempts to parse a string into a boolean value.
    /// Returns the default if not possible.
    /// </summary>
    /// <param name="rawstring">Raw string to parse.</param>
    /// <returns>Day of week as enum.</returns>
    [Pure]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed")]
    public static DayOfWeek TryParseDayOfWeekOrGetDefault(string rawstring)
    {
        if (Enum.TryParse(typeof(DayOfWeek), rawstring, true, out object? value) && value is DayOfWeek dayofWeek)
        {
            return dayofWeek;
        }
        return DayOfWeek.Tuesday;
    }
}
#pragma warning restore SA1201 // Elements should appear in the correct order
