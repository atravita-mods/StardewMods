namespace HighlightEmptyMachines.Framework;

/// <summary>
/// Enum to hold the possible Machine statuses.
/// </summary>
internal enum MachineStatus
{
    /// <summary>
    /// This machine is enabled in settings and can receive input.
    /// </summary>
    Enabled,

    /// <summary>
    /// This machine is invalid for some reason.
    /// </summary>
    Invalid,

    /// <summary>
    /// This machine is disabled in settings.
    /// </summary>
    Disabled,
}