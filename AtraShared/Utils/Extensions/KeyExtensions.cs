namespace AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework.Input;

/// <summary>
/// Adds extension methods for grabbing specific key values.
/// </summary>
public static class KeyExtensions
{
    /// <summary>
    /// Gets the number pad key a number represents.
    /// </summary>
    /// <param name="i">The number.</param>
    /// <returns>The number pad key, if it's in range.</returns>
    public static Keys? MapNumberToKey(this int i)
        => i switch
        {
            0 => Keys.NumPad0,
            1 => Keys.NumPad1,
            2 => Keys.NumPad2,
            3 => Keys.NumPad3,
            4 => Keys.NumPad4,
            5 => Keys.NumPad5,
            6 => Keys.NumPad6,
            7 => Keys.NumPad7,
            8 => Keys.NumPad8,
            9 => Keys.NumPad9,
            _ => null,
        };
}