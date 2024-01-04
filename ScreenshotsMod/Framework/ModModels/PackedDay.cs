using AtraBase.Toolkit.Extensions;
using AtraShared.ConstantsAndEnums;

using CommunityToolkit.Diagnostics;

using Newtonsoft.Json;

namespace ScreenshotsMod.Framework.ModModels;

/// <summary>
/// A struct that represents a packed format of a day/season constraint.
/// </summary>
[JsonConverter(typeof(PackedDayConverter))]
internal readonly struct PackedDay(uint value)
{
    /// <summary>
    /// Represents all mondays.
    /// </summary>
    private const uint Monday = 0b1 | 0b1 << 7 | 0b1 << 14 | 0b1 << 21;
    private const uint AllDays = 0x0FFF_FFFF;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackedDay"/> struct.
    /// Gets the packed day representation of the current day.
    /// </summary>
    public PackedDay()
        : this((Math.Clamp((uint)Game1.seasonIndex, 0u, 3u) + 28u) | (1u << ((Game1.dayOfMonth % 28) - 1)))
    {
    }

    private uint Value => value;

    /// <summary>
    /// Parses days and seasons to the PackedDay struct.
    /// </summary>
    /// <param name="seasons">Seasons to parse.</param>
    /// <param name="days">Days to parse.</param>
    /// <param name="error">Error.</param>
    /// <returns>PackedValue, or null if there's an issue.</returns>
    public static PackedDay? Parse(string[] seasons, string[] days, out string? error)
    {
        if (seasons.Length == 0 || days.Length == 0)
        {
            error = "is empty";
            return null;
        }

        uint packed = 0;
        foreach (string season in seasons)
        {
            string trimmed = season.Trim();
            if (trimmed.Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                packed |= 0xF000_0000;
                break;
            }
            if (!StardewSeasonsExtensions.TryParse(trimmed, out StardewSeasons s, ignoreCase: true))
            {
                error = $"could not parse {trimmed} as a valid season";
                return null;
            }

            packed |= ((uint)s) << 28;
        }

        foreach (string day in days)
        {
            string d = day.Trim();
            if (d.Equals("Any", StringComparison.OrdinalIgnoreCase) || d == "-")
            {
                packed |= AllDays;
                break;
            }
            if (Enum.TryParse(d, ignoreCase: true, out DayOfWeek dayOfWeek))
            {
                int shift = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
                packed |= Monday << shift;
            }
            else if (d.TrySplitOnce('-', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
            {
                int min = int.TryParse(first.Trim(), out int v) ? Math.Clamp(v, 1, 28) : 1;
                int max = int.TryParse(second.Trim(), out int v2) ? Math.Clamp(v2, 1, 28) : 28;

                if (max < min)
                {
                    continue;
                }

                uint full = AllDays;

                // shift to the right to get the right number of bits.
                // total number of days is (max - min + 1). I originally have 28 bits
                // so shift away 28 - (max - min + 1);
                full >>= 28 - (max - min + 1);

                // shift back enough for min.
                full <<= min - 1;
                packed |= full;
            }
            else if (int.TryParse(d, out int val))
            {
                val = Math.Clamp(val, 1, 28);
                packed |= 0b1u << (val - 1);
            }
            else
            {
                error = $"could not parse {d} as valid day range";
                return null;
            }
        }

        if ((0xF000_0000 & packed) == 0 || (AllDays & packed) == 0)
        {
            error = "is empty";
            return null;
        }

        error = null;
        return new(packed);
    }

    /// <inheritdoc />
    public override string ToString() => value.ToString("X8");

    /// <summary>
    /// Checks to see if the current day is allowed by this value.
    /// </summary>
    /// <param name="current">The current day.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    internal bool Check(PackedDay current)
        => (current.Value & value) == current.Value;
}

/// <summary>
/// A JSON converter for the Packed Day.
/// </summary>
public class PackedDayConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => objectType == typeof(PackedDay);

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Integer:
                return new PackedDay((uint)reader.Value!);
            case JsonToken.String:
            {
                if (uint.TryParse((string?)reader.Value, out uint val))
                {
                    return new PackedDay(val);
                }
                break;
            }
        }
        return ThrowHelper.ThrowInvalidDataException<object?>($"Could not parse {reader.Value} as {nameof(PackedDay)} at {reader.Path}.");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        PackedDay packed = (PackedDay)value!;
        writer.WriteValue(packed.ToString());
    }
}