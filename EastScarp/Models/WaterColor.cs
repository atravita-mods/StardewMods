﻿namespace EastScarp.Models;

/// <summary>
/// A color to apply to the water.
/// </summary>
public sealed class WaterColor : BaseEntry
{
    public string Color { get; set; } = string.Empty;
}
