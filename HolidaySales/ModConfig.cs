using NetEscapades.EnumGenerators;

namespace HolidaySales;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating how shops should behave during festivals.
    /// </summary>
    public FestivalsShopBehavior StoreFestivalBehavior { get; set; } = FestivalsShopBehavior.MapDependent;
}

/// <summary>
/// The expected behavior of the shops during festivals.
/// </summary>
[EnumExtensions]
internal enum FestivalsShopBehavior
{
    /// <summary>
    /// Festivals close shops (default vanilla behavior).
    /// </summary>
    Closed,

    /// <summary>
    /// Festivals only close shops on "their" map.
    /// </summary>
    MapDependent,

    /// <summary>
    /// Festivals never close shops.
    /// </summary>
    Open,
}
