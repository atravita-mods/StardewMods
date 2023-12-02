using AtraBase.Toolkit.Extensions;

using CommunityToolkit.Diagnostics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents an range in time (inclusive.)
/// </summary>
[JsonConverter(typeof(TimeRangeConverter))]
public record struct TimeRange : IComparable<TimeRange>
{
    private int startTime = 600;
    private int endTime = 2600;

    public TimeRange() { }

    public TimeRange(int startTime, int endTime)
    {
        this.StartTime = startTime;
        this.EndTime = endTime;
    }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public int StartTime
    {
        get => this.startTime;
        set => this.startTime = Math.Clamp(value - (value % 10), 600, 2600);
    }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public int EndTime
    {
        get => this.endTime;
        set => this.endTime = Math.Clamp(value - (value % 10), 600, 2600);
    }

    /// <inheritdoc/>
    public int CompareTo(TimeRange other) => this.StartTime - other.StartTime;

    /// <inheritdoc/>
    public override string ToString() => $"{this.StartTime:D4}-{this.EndTime:D4}";

    public static bool TryParse(string? value, out TimeRange result)
    {
        if (value is not null
            && value.TrySplitOnce('-', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
            && int.TryParse(first, out int start)
            && int.TryParse(second, out int end))
        {
            result = new(start, end);
            return true;
        }
        result = default;
        return false;
    }
}

/// <summary>
/// Converts a time range to and from json.
/// </summary>
public class TimeRangeConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
        => objectType == typeof(TimeRange) || Nullable.GetUnderlyingType(objectType) == typeof(TimeRange);

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                JObject obj = JObject.Load(reader);
                JToken? startToken = obj.GetValue(nameof(TimeRange.StartTime), StringComparison.OrdinalIgnoreCase);
                JToken? endToken = obj.GetValue(nameof(TimeRange.EndTime), StringComparison.OrdinalIgnoreCase);
                if (startToken is not null && endToken is not null)
                {
                    return new TimeRange(startToken.Value<int>(), endToken.Value<int>());
                }

                break;
            }
            case JsonToken.String:
                if (TimeRange.TryParse((string?)reader.Value, out var range))
                {
                    return range;
                }
                break;
        }

        return ThrowHelper.ThrowInvalidDataException<object?>($"Could not parse {nameof(TimeRange)} from {reader.Value} at {reader.Path}");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        TimeRange timeRange = (TimeRange)value!;
        writer.WriteValue(timeRange.ToString());
    }
}