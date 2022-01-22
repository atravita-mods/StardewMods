namespace GingerIslandMainlandAdjustments;

/// <summary>
/// Class to handle global variables.
/// </summary>
internal static class Globals
{
    // Values are set in the Mod.Entry method, so should never be null.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private static IMonitor modMonitor;
    private static ModConfig config;
    private static IReflectionHelper reflectionHelper;
    private static IContentHelper contentHelper;
    private static IModRegistry modRegistry;
    private static IModHelper helper;

    /// <summary>
    /// Gets SMAPI's logging service.
    /// </summary>
    internal static IMonitor ModMonitor => modMonitor;

    /// <summary>
    /// Gets or sets mod configuration class.
    /// </summary>
    internal static ModConfig Config
    {
        get => config;
        set => config = value;
    }

    /// <summary>
    /// Gets SMAPI's reflection helper.
    /// </summary>
    internal static IReflectionHelper ReflectionHelper => reflectionHelper;

    /// <summary>
    /// Gets SMAPI's Content helper.
    /// </summary>
    internal static IContentHelper ContentHelper => contentHelper;

    /// <summary>
    /// Gets SMAPI's mod registry helper.
    /// </summary>
    internal static IModRegistry ModRegistry => modRegistry;

    /// <summary>
    /// Gets SMAPI's helper class.
    /// </summary>
    internal static IModHelper Helper => helper;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Initialize globals.
    /// </summary>
    /// <param name="helper">SMAPI's IModHelper.</param>
    /// <param name="monitor">SMAPI's logging service.</param>
    internal static void Initialize(IModHelper helper, IMonitor monitor)
    {
        modMonitor = monitor;
        reflectionHelper = helper.Reflection;
        contentHelper = helper.Content;
        modRegistry = helper.ModRegistry;
        Globals.helper = helper;

        try
        {
            Config = helper.ReadConfig<ModConfig>();
        }
        catch
        {
            modMonitor.Log(I18n.IllFormatedConfig(), LogLevel.Warn);
            config = new();
        }
    }
}
