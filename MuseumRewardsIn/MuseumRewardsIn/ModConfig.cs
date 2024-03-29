﻿using AtraShared.Integrations.GMCMAttributes;
using Microsoft.Xna.Framework;

namespace MuseumRewardsIn;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating where to stick
    /// the box for the museum shop.
    /// </summary>
    [GMCMDefaultIgnore]
    public Vector2 BoxLocation { get; set; } = new(-1, -1);

    /// <summary>
    /// Gets or sets a value indicating whether or not buybacks should be enabled.
    /// </summary>
    public bool AllowBuyBacks { get; set; } = false;
}
