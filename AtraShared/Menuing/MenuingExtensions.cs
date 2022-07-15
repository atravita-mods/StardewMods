namespace AtraShared.Menuing;

/// <summary>
/// Extensions to help with menus.
/// </summary>
public static class MenuingExtensions
{
    public static bool CanRaiseMenu()
        => Context.IsWorldReady && Context.CanPlayerMove && !Game1.player.isRidingHorse()
            && Game1.currentLocation is not null && !Game1.eventUp && !Game1.isFestival() && !Game1.IsFading()
            && Game1.activeClickableMenu is null;
}
