using StardewValley.TokenizableStrings;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Just some extension methods for token parsing.
/// </summary>
public static class TokenParserExtensions
{
    /// <summary>
    /// An extension method form of <see cref="TokenParser.ParseText(string, Random, StardewValley.Delegates.TokenParserDelegate, Farmer)"/>
    /// </summary>
    /// <param name="tokenized">Tokenized string.</param>
    /// <returns>Rendered string.</returns>
    /// <remarks>This entirely exists to make code look prettier. I'm petty.</remarks>
    public static string? ParseTokens(this string? tokenized)
        => TokenParser.ParseText(tokenized, Random.Shared);
}