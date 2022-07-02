namespace HolidaySales;
internal class ModConfig
{
    public FestivalsShopBehavior StoreFestivalBehavior { get; set; } = FestivalsShopBehavior.MapDependent;
}

internal enum FestivalsShopBehavior
{
    Closed,
    MapDependent,
    Open
}
