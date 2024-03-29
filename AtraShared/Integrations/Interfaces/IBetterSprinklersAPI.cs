﻿/************
 * The following file was copied from : https://gitlab.com/speeder1/SMAPISprinklerMod/-/blob/master/SMAPISprinklerMod/IBetterSprinklersApi.cs
 *
 * It appears to be entirely unlicensed; this file was copied solely for use with SMAPI's api proxying.
 * ************/

using Microsoft.Xna.Framework;

namespace AtraShared.Integrations.Interfaces;

/// <summary>The API which provides access to Better Sprinklers for other mods.</summary>
/// <remarks>Copied from https://gitlab.com/speeder1/SMAPISprinklerMod/-/blob/master/SMAPISprinklerMod/IBetterSprinklersApi.cs .</remarks>
public interface IBetterSprinklersApi
{
    /// <summary>Get the maximum sprinkler coverage supported by this mod (in tiles wide or high).</summary>
    int GetMaxGridSize();

    /// <summary>Get the relative tile coverage by supported sprinkler ID.</summary>
    IDictionary<int, Vector2[]> GetSprinklerCoverage();
}