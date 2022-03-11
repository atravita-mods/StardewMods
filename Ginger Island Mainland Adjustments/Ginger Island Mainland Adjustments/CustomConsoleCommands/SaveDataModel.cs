namespace GingerIslandMainlandAdjustments.CustomConsoleCommands;

/// <summary>
/// Model to save custom data into the save.
/// </summary>
/// <remarks>Only available for the main player.</remarks>
public sealed class SaveDataModel
{
    /// <summary>
    /// Gets or sets list of NPCS queued for the next day for Ginger Island.
    /// </summary>
    public List<string> NPCsForTomorrow { get; set; } = new();
}