using AtraBase.Toolkit.StringHandler;

namespace MuseumRewardsIn;

/// <summary>
/// The utility class for this mod.
/// </summary>
internal static class MRUtils
{
    /// <summary>
    /// Finds the items attached to a mail string.
    /// </summary>
    /// <param name="mail">The mail data to process.</param>
    /// <returns>The items attached.</returns>
    internal static IEnumerable<string> ParseItemsFromMail(this string mail)
    {
        List<string>? ret = null;

        ReadOnlySpan<char> mailSpan = mail.AsSpan().Trim();
        while (mailSpan.Length > 0)
        {
            int startIndex = mailSpan.IndexOf("%item", StringComparison.Ordinal);
            if (startIndex < 0)
            {
                break;
            }
            ReadOnlySpan<char> remainder = mailSpan[(startIndex + "%item".Length)..];

            int endIndex = remainder.IndexOf("%%", StringComparison.Ordinal);
            if (endIndex < 0)
            {
                break;
            }

            ReadOnlySpan<char> substring = remainder[..endIndex].Trim();
            mailSpan = remainder[endIndex..].Trim();

            if (substring.Length <= 0)
            {
                continue;
            }

            if (substring.StartsWith("object ", StringComparison.OrdinalIgnoreCase))
            {
                ret ??= [];

                bool isItem = true;
                foreach (SpanSplitEntry split in substring["object ".Length..].Trim().StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (isItem)
                    {
                        string item = $"{ItemRegistry.type_object}{split}";
                        if (ItemRegistry.Exists(item))
                        {
                            ret.Add(item);
                        }
                    }
                    isItem = !isItem;
                }
                return ret;
            }
            else if (substring.StartsWith("id", StringComparison.OrdinalIgnoreCase))
            {
                ret ??= [];
                bool isItem = true;
                foreach (SpanSplitEntry split in substring["id ".Length..].Trim().StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (isItem)
                    {
                        string item = split.ToString();
                        if (ItemRegistry.Exists(item))
                        {
                            ret.Add(item);
                        }
                    }
                    isItem = !isItem;
                }
            }
            else if (substring.StartsWith("bigobject ", StringComparison.OrdinalIgnoreCase))
            {
                ret ??= [];

                foreach (SpanSplitEntry split in substring["bigobject ".Length..].Trim().StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    string item = $"{ItemRegistry.type_bigCraftable}{split}";
                    if (ItemRegistry.Exists(item))
                    {
                        ret.Add(item);
                    }
                }

                return ret;
            }
            else if (substring.StartsWith("furniture ", StringComparison.OrdinalIgnoreCase))
            {
                ret ??= [];

                foreach (SpanSplitEntry split in substring["furniture ".Length..].Trim().StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    string item = $"{ItemRegistry.type_furniture}{split}";
                    if (ItemRegistry.Exists(item))
                    {
                        ret.Add(item);
                    }
                }

                return ret;
            }
        }

        return ret ?? Enumerable.Empty<string>();
    }
}
