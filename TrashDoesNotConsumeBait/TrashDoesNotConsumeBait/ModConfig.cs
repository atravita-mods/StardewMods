﻿using AtraShared.Integrations.GMCMAttributes;

namespace TrashDoesNotConsumeBait;

/// <summary>
/// The configuration class for this mod.
/// </summary>
internal sealed class ModConfig
{
    private float consumeChanceNormal = 1f;
    private float consumeChancePreserving = 0.5f;

    /// <summary>
    /// Gets or sets a value indicating whether or not tackles/bait should be automatically refilled.
    /// </summary>
    public bool AutomaticRefill { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether tackles should be only replaced with the same type of tackle.
    /// </summary>
    public bool SameTackleOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether bait should only be replaced with the same type of bait.
    /// </summary>
    public bool SameBaitOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets chance of consuming bait/tackle normally.
    /// </summary>
    [GMCMRange(0, 1)]
    [GMCMInterval(0.01)]
    public float ConsumeChanceNormal
    {
        get => this.consumeChanceNormal;
        set => this.consumeChanceNormal = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets chance of consuming bait/tackle with the Preserving enchantment.
    /// </summary>
    [GMCMRange(0, 1)]
    [GMCMInterval(0.01)]
    public float ConsumeChancePreserving
    {
        get => this.consumeChancePreserving;
        set => this.consumeChancePreserving = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this mod should affect crab pots as well.
    /// </summary>
    public bool CrabPotTrashDoesNotEatBait { get; set; } = true;
}