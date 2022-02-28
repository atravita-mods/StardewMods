using System.Globalization;
using System.Runtime.CompilerServices;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions to more easily interact with the ModData <see cref="ModDataDictionary" /> dictionary.
/// </summary>
/// <remarks>Inspired by https://github.com/spacechase0/StardewValleyMods/blob/main/SpaceShared/ModDataHelper.cs. </remarks>
internal static class ModDataExtensions
{
    // Instead of storing a real bool, just store 0 or 1

    /// <summary>
    /// Gets a boolean value out of ModData.
    /// </summary>
    /// <param name="modData">ModData.</param>
    /// <param name="key">Key.</param>
    /// <returns>Boolean value, or null if not found/not parseable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? GetBool(this ModDataDictionary modData, string key)
        => modData.TryGetValue(key, out string val) ? !(val == "0") : null;

    /// <summary>
    /// Sets a boolean value into modData.
    /// </summary>
    /// <param name="modData">ModData.</param>
    /// <param name="key">Key.</param>
    /// <param name="val">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBool(this ModDataDictionary modData, string key, bool val)
        => modData[key] = val ? "1" : "0";

    /// <summary>
    /// Gets a float value out of ModData.
    /// </summary>
    /// <param name="modData">ModData.</param>
    /// <param name="key">Key.</param>
    /// <returns>Float value, or null of not found/not parseable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float? GetFloat(this ModDataDictionary modData, string key)
        => modData.TryGetValue(key, out string val) && float.TryParse(val, out float result) ? result : null;

    /// <summary>
    /// Sets a float value into modData. To reduce reads/writes, rounds.
    /// </summary>
    /// <param name="modData">ModData.</param>
    /// <param name="key">Key.</param>
    /// <param name="val">Value.</param>
    /// <param name="decimals">Decimal points to round to.</param>
    /// <param name="format">Format string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetFloat(this ModDataDictionary modData, string key, float val, int decimals = 2, string? format = "G")
        => modData[key] = Math.Round(val, decimals, MidpointRounding.ToEven).ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a int value out of ModData.
    /// </summary>
    /// <param name="modData">ModData.</param>
    /// <param name="key">Key.</param>
    /// <returns>Int value, or null of not found/not parseable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? GetInt(this ModDataDictionary modData, string key)
        => modData.TryGetValue(key, out string val) && int.TryParse(val, out int result) ? result : null;

    /// <summary>
    /// Sets a int value into modData.
    /// </summary>
    /// <param name="modData">ModData.</param>
    /// <param name="key">Key.</param>
    /// <param name="val">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetInt(this ModDataDictionary modData, string key, int val)
        => modData[key] = val.ToString(CultureInfo.InvariantCulture);
}