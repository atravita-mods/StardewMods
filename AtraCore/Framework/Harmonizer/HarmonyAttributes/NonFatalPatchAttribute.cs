﻿namespace AtraCore.Framework.Harmonizer.HarmonyAttributes;

/// <summary>
/// Indicates that the patch should be ignored if the target can not be found.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class NonFatalPatchAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonFatalPatchAttribute"/> class.
    /// </summary>
    public NonFatalPatchAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NonFatalPatchAttribute"/> class.
    /// </summary>
    /// <param name="message">A custom message to add.</param>
    public NonFatalPatchAttribute(string? message)
        => this.Message = message;

    /// <summary>
    /// Gets the custom message to add, if there is one.
    /// </summary>
    internal string? Message { get; init; }
}
