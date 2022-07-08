namespace HolidaySales;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    public FestivalsShopBehavior StoreFestivalBehavior { get; set; } = FestivalsShopBehavior.MapDependent;
}

internal enum FestivalsShopBehavior
{
    Closed,
    MapDependent,
    Open
}
