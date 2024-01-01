using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AtraShared.Niceties;

/// <summary>
/// A more user friendly color converter.
/// </summary>
public sealed class ColorConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => objectType == typeof(Color) || Nullable.GetUnderlyingType(objectType) == typeof(Color);

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                JObject obj = JObject.Load(reader);
                if (obj.TryGetValueIgnoreCase<int>(nameof(Color.R), out int r)
                    && obj.TryGetValueIgnoreCase<int>(nameof(Color.G), out int g)
                    && obj.TryGetValueIgnoreCase<int>(nameof(Color.B), out int b))
                {
                    int alpha = obj.TryGetValueIgnoreCase<int>(nameof(Color.A), out int a) ? a : byte.MaxValue;
                    return new Color(r, g, b, alpha);
                }
                break;
            }
            case JsonToken.String:
            {
                if (ColorHandler.TryParseColor((string?)reader.Value, out Color color))
                {
                    return color;
                }
                break;
            }
            case JsonToken.Integer:
            {
                if (reader.Value is int packed)
                {
                    unchecked
                    {
                        return new Color((uint)packed);
                    }
                }
                break;
            }
        }

        return ThrowHelper.ThrowInvalidDataException<object?>($"Could not parse {nameof(Color)} from {reader.Value} at {reader.Path}");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        Color color = (Color)value!;
        writer.WriteValue(ColorHandler.ToHexString(color));
    }
}