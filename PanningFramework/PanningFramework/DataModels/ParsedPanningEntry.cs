﻿using AtraShared.ConstantsAndEnums;

namespace PanningFramework.DataModels;

/// <summary>
/// A parsed panning entry.
/// </summary>
internal class ParsedPanningEntry
{
    internal ParsedPanningEntry(ItemTypeEnum itemType, int index, double chance)
    {
        this.ItemType = itemType;
        this.Index = index;
        this.Chance = chance;
    }

    internal ItemTypeEnum ItemType { get; set; }

    internal int Index { get; set; } = -1;

    internal int MinStack { get; set; } = 1;

    internal int MaxStack { get; set; } = 5;

    internal double Chance { get; set; } = 0.05;

    internal double DailyLuckAdjustment { get; set; } = 0;

    internal double LuckStatAdjustment { get; set; } = 0;


}