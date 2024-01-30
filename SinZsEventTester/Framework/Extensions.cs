namespace SinZsEventTester.Framework;
using StardewValley.Menus;

/// <summary>
/// The extension methods for this mod
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Faster replacement for str.Split()[index];.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <param name="deliminator">deliminator to use.</param>
    /// <param name="index">index of the chunk to get.</param>
    /// <returns>a readonlyspan char with the chunk, or an empty readonlyspan for failure.</returns>
    public static ReadOnlySpan<char> GetNthChunk(this string str, char deliminator, int index = 0)
    {
        int start = 0;
        int ind = 0;
        while (index-- >= 0)
        {
            ind = str.IndexOf(deliminator, start);
            if (ind == -1)
            {
                // since we've previously decremented index, check against -1;
                // this means we're done.
                if (index == -1)
                {
                    return str.AsSpan()[start..];
                }

                // else, we've run out of entries
                // and return an empty span to mark as failure.
                return ReadOnlySpan<char>.Empty;
            }

            if (index > -1)
            {
                start = ind + 1;
            }
        }
        return str.AsSpan()[start..ind];
    }

    /// <summary>
    /// Speeds up dialogue boxes.
    /// </summary>
    /// <param name="db">the dialogue box to speed up.</param>
    public static void SpeedUp(this DialogueBox db)
    {
        if (db.safetyTimer < 12)
        {
            return;
        }

        db.finishTyping();
        db.safetyTimer = 10;

        if (db.transitioningBigger)
        {
            db.transitionX = db.x;
            db.transitionY = db.y;
            db.transitionWidth = db.width;
            db.transitionHeight = db.height;
        }
    }

    /// <summary>
    /// Tries to split once by a deliminator.
    /// </summary>
    /// <param name="str">Text to split.</param>
    /// <param name="deliminator">Deliminator to split by.</param>
    /// <param name="first">The part that precedes the deliminator, or the whole text if not found.</param>
    /// <param name="second">The part that is after the deliminator.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [Pure]
    public static bool TrySplitOnce(this string str, char deliminator, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        ArgumentNullException.ThrowIfNull(str, nameof(str));
        return str.AsSpan().TrySplitOnce(deliminator, out first, out second);
    }

    /// <summary>
    /// Tries to split once by a deliminator.
    /// </summary>
    /// <param name="str">Text to split.</param>
    /// <param name="deliminator">Deliminator to split by.</param>
    /// <param name="first">The part that precedes the deliminator, or the whole text if not found.</param>
    /// <param name="second">The part that is after the deliminator.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [Pure]
    public static bool TrySplitOnce(this ReadOnlySpan<char> str, char deliminator, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        int idx = str.IndexOf(deliminator);

        if (idx < 0)
        {
            first = str;
            second = [];
            return false;
        }

        first = str[..idx];
        second = str[(idx + 1)..];
        return true;
    }
}