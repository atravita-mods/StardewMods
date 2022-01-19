using GingerIslandMainlandAdjustments.ScheduleManager;

namespace GingerIslandMainlandAdjustments;

/// <summary>
/// Class to handle global variables.
/// </summary>
internal class Globals
{
    // This would defeat the whole purpose of *having* a globals class, eh?
#pragma warning disable SA1401 // Fields should be private
    // Values are set in the Mod.Entry method, so should never be null.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// SMAPI's logging service.
    /// </summary>
    public static IMonitor ModMonitor;

    /// <summary>
    /// Mod configuration class.
    /// </summary>
    public static ModConfig Config;

    /// <summary>
    /// SMAPI's reflection helper.
    /// </summary>
    public static IReflectionHelper ReflectionHelper;

    /// <summary>
    /// SMAPI's Content helper.
    /// </summary>
    public static IContentHelper ContentHelper;

    /// <summary>
    /// SMAPI's mod registry helper.
    /// </summary>
    public static IModRegistry ModRegistry;

    /// <summary>
    /// SMAPI's helper class.
    /// </summary>
    public static IModHelper Helper;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore SA1401 // Fields should be private
}
