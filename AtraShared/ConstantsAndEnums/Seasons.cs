namespace AtraShared.ConstantsAndEnums;

#pragma warning disable SA1602 // Enumeration items should be documented. Should be obvious enough
/// <summary>
/// Seasons as flags.
/// </summary>
[Flags]
internal enum StardewSeasons : uint
{

    Spring = 0b1,

    Summer = 0b10,
    Fall = 0b100,
    Winter = 0b1000,
}

/// <summary>
/// Weathers as flags....
/// </summary>
[Flags]
internal enum StardewWeather : uint
{
    Sun = 0b1,
    Rain = 0b10,
    Storm = 0b100,
    Snow = 0b1000,
    Wind = 0b10000,
}
#pragma warning restore SA1602 // Enumeration items should be documented