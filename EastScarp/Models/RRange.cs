using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EastScarp.Models;

/// <summary>
/// Represents a range, inclusive.
/// </summary>
[JsonConverter(typeof(RRangeConverter))]
public struct RRange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RRange"/> struct.
    /// </summary>
    public RRange()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RRange"/> struct.
    /// </summary>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    public RRange(int min, int max)
    {
        this.Min = min;
        this.Max = max;
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public int Min { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public int Max { get; set; } = 1;

    /// <inheritdoc/>
    public override readonly string ToString() => $"{this.Min}-{this.Max}";

    /// <summary>
    /// Tries to parse a string value to a range.
    /// </summary>
    /// <param name="value">Value to parse.</param>
    /// <param name="result">Range if parsed, default if not.</param>
    /// <returns>True if parsable, false otherwise.</returns>
    internal static bool TryParse(string? value, out RRange? result)
    {
        if (value is not null
            && value.AsSpan().TrySplitOnce('-', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
            && int.TryParse(first, out int start)
            && int.TryParse(second, out int end))
        {
            result = new (start, end);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// Gets a random value in this range.
    /// </summary>
    /// <returns>Random value in this range.</returns>
    internal readonly int Get() => Random.Shared.Next(this.Min, Math.Max(this.Min, this.Max) + 1);
}

/// <summary>
/// Handles json conversions for a range.
/// </summary>
public class RRangeConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => objectType == typeof(RRange) || Nullable.GetUnderlyingType(objectType) == typeof(RRange);

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                JObject obj = JObject.Load(reader);
                JToken? startToken = obj.GetValue(nameof(RRange.Min), StringComparison.OrdinalIgnoreCase);
                JToken? endToken = obj.GetValue(nameof(RRange.Max), StringComparison.OrdinalIgnoreCase);
                if (startToken is not null && endToken is not null)
                {
                    return new RRange(startToken.Value<int>(), endToken.Value<int>());
                }

                break;
            }
            case JsonToken.String:
                if (RRange.TryParse((string?)reader.Value, out RRange? val))
                {
                    return val;
                }
                break;
        }

        throw new InvalidDataException($"Could not parse {nameof(RRange)} from {reader.Value} at {reader.Path}");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        RRange range = (RRange)value!;
        writer.WriteValue(range.ToString());
    }
}