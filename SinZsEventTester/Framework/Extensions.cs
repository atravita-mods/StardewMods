﻿namespace SinZsEventTester.Framework;
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
        db.finishTyping();
        db.safetyTimer = 0;

        if (db.transitioningBigger)
        {
            db.transitionX = db.x;
            db.transitionY = db.y;
            db.transitionWidth = db.width;
            db.transitionHeight = db.height;
        }
    }
}