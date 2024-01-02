using AtraBase.Toolkit.StringHandler;

using CommunityToolkit.Diagnostics;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods for items.
/// </summary>
public static class ItemExtensions
{
    public static bool MatchesTagList(this Item item, string tagList)
    {
        Guard.IsNotNull(item);
        Guard.IsNotNull(tagList);

        foreach (SpanSplitEntry tag in tagList.StreamSplit(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!item.CheckOrTag(tag))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CheckOrTag(this Item item, ReadOnlySpan<char> split)
    {
        foreach (SpanSplitEntry tag in split.StreamSplit('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (item.HasContextTag(tag))
            {
                return true;
            }
        }

        return false;
    }
}
