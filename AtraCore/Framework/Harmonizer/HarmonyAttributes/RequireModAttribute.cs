﻿namespace AtraCore.Framework.Harmonizer.HarmonyAttributes;

/// <summary>
/// Indicates the following patch should only be applied if a specific mod is installed.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireModAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireModAttribute"/> class.
    /// </summary>
    /// <param name="uniqueID">The uniqueID of the mod to require.</param>
    public RequireModAttribute(string uniqueID)
        => this.UniqueID = uniqueID;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireModAttribute"/> class.
    /// </summary>
    /// <param name="uniqueID">The uniqueID of the mod to require.</param>
    /// <param name="minVersion">The minimum minVersion to patch.</param>
    public RequireModAttribute(string uniqueID, string? minVersion = null, string? maxVersion = null)
        : this(uniqueID)
    {
        this.MinVersion = minVersion;
        this.MaxVersion = maxVersion;
    }

    internal string UniqueID { get; init; }

    internal string? MinVersion { get; init; }

    internal string? MaxVersion { get; init; }
}
