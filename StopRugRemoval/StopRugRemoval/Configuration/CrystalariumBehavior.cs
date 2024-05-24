// Ignore Spelling: Crystalarium

namespace StopRugRemoval.Configuration;

/// <summary>
/// Gets the behavior for swapping gems in the crystallarium.
/// </summary>
public enum CrystalariumBehavior
{
    /// <summary>
    /// Use vanilla behavior.
    /// </summary>
    Vanilla,

    /// <summary>
    /// Require breaking the crystallarium.
    /// </summary>
    Break,

    /// <summary>
    /// Causes the gem to swap.
    /// </summary>
    Swap,

    /// <summary>
    /// Require a keybind to be held.
    /// </summary>
    Keybind,
}