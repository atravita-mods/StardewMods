﻿using AtraBase.Toolkit.Extensions;

using AtraShared.Wrappers;

using NetEscapades.EnumGenerators;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// Enum for all the vanilla machines that require input.
/// </summary>
/// <remarks>Positive numbers refer to Big Craftables, negative ordinary SObjects.</remarks>
[EnumExtensions]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Should be obvious enough.")]
public enum VanillaMachinesEnum
{
    /// <summary>
    /// Beehouse.
    /// </summary>
    BeeHouse = 10,

    /// <summary>
    /// Keg.
    /// </summary>
    Keg = 12,

    /// <summary>
    /// Furnace.
    /// </summary>
    Furnace = 13,

    /// <summary>
    /// Preserves Jar.
    /// </summary>
    PreservesJar = 15,

    /// <summary>
    /// Cheese Press.
    /// </summary>
    CheesePress = 16,

    /// <summary>
    /// Loom.
    /// </summary>
    Loom = 17,

    /// <summary>
    /// Oil maker.
    /// </summary>
    OilMaker = 19,

    /// <summary>
    /// Recycling machine.
    /// </summary>
    RecyclingMachine = 20,

    /// <summary>
    /// Crystalarium.
    /// </summary>
    Crystalarium = 21,

    /// <summary>
    /// Mayo machine.
    /// </summary>
    MayonnaiseMachine = 24,

    /// <summary>
    /// Seed maker.
    /// </summary>
    SeedMaker = 25,
    BoneMill = 90,
    Incubator = 101,
    CharcoalKiln = 114,
    SlimeIncubator = 156,
    SlimeEggPress = 158,
    Cask = 163,
    GeodeCrusher = 182,
    WoodChipper = 211,
    OstrichIncubator = 254,
    Deconstructor = 265,
    CrabPot = -710,
}

/// <summary>
/// Holds extension methods against this enum.
/// </summary>
public static partial class VanillaMachinesEnumExtensions
{
    /// <summary>
    /// Tries to find the correct translation string for this machine.
    /// </summary>
    /// <param name="machine">VanillaMachineEnum.</param>
    /// <returns>A string, hopefully the translation.</returns>
    public static string GetBestTranslatedString(this VanillaMachinesEnum machine)
    {
        if (machine < 0 && Game1Wrappers.ObjectInfo.TryGetValue(-(int)machine, out string? val))
        {
            ReadOnlySpan<char> translatedName = val.GetNthChunk('/', SObject.objectInfoDisplayNameIndex);
            if (translatedName.Length > 0)
            {
                return translatedName.ToString();
            }
        }
        else if (machine > 0 && Game1.bigCraftablesInformation.TryGetValue((int)machine, out string? value))
        {
            int index = value.LastIndexOf('/');
            if (index >= 0)
            {
                return value[(index + 1)..];
            }
        }
        return machine.ToStringFast();
    }
}