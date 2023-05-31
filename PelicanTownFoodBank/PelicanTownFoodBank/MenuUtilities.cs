using StardewModdingAPI.Utilities;

namespace PelicanTownFoodBank;

internal class MenuUtilities
{
    private static readonly KeybindList ctrl = KeybindList.Parse("LeftControl, RightControl");
    private static readonly KeybindList shift = KeybindList.Parse("LeftShift, RightShift");

    internal static int GetIdealQuantityFromKeyboardState()
        => ctrl.IsDown() ? 25 :
            shift.IsDown() ? 5 : 1;
}