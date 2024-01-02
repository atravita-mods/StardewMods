using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EastScarp.Models;

/// <summary>
/// Represents an range in time (inclusive.)
/// </summary>
[JsonConverter(typeof(TimeRangeConverter))]
public record struct TimeRange : IComparable<TimeRange>
{
    private int startTime = 600;
    private int endTime = 2600;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeRange"/> class.
    /// </summary>
    public TimeRange() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeRange"/> class.
    /// </summary>
    /// <param name="startTime">the start time.</param>
    /// <param name="endTime">the end time.</param>
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
        readonly get => this.startTime;
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
    public readonly int CompareTo(TimeRange other) => this.StartTime - other.StartTime;

    /// <inheritdoc/>
    public override string ToString() => $"{this.StartTime:D4}-{this.EndTime:D4}";

    /// <summary>
    /// Tries to parse a string to a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">The timespan.</param>
    /// <returns>True if successful, false otherwise.</returns>
    internal static bool TryParse(string? value, out TimeRange result)
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
    /// Merges together a list of times to remove overlaps.
    /// </summary>
    /// <param name="times">List of times.</param>
    /// <returns>Array of times.</returns>
    internal static TimeRange[] FoldTimes(TimeRange[] times)
    {
        if (times.Length is 0 or 1)
        {
            return times;
        }

        Array.Sort(times);

        bool nonoverlap = true;
        for (int i = 1; i < times.Length; i++)
        {
            TimeRange first = times[i - 1];
            TimeRange second = times[i];
            if (first.EndTime >= second.StartTime)
            {
                nonoverlap = false;
                break;
            }
        }

        if (nonoverlap)
        {
            return times;
        }

        List<TimeRange>? proposed = [];
        TimeRange prev = times[0];
        for (int i = 1; i < times.Length; i++)
        {
            TimeRange current = times[i];
            if (current.StartTime <= prev.EndTime)
            {
                prev = new (prev.StartTime, Math.Max(prev.EndTime, current.EndTime));
            }
            else
            {
                proposed.Add(prev);
                prev = current;
            }
        }

        proposed.Add(prev);
        return proposed.ToArray();
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
            {
                if (TimeRange.TryParse((string?)reader.Value, out TimeRange range))
                {
                    return range;
                }
                break;
            }
        }

        throw new InvalidDataException($"Could not parse {nameof(TimeRange)} from {reader.Value} at {reader.Path}.");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        TimeRange timeRange = (TimeRange)value!;
        writer.WriteValue(timeRange.ToString());
    }
}

file static class Extensions
{
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