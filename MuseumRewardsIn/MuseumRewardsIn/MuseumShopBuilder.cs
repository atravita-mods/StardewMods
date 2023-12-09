namespace MuseumRewardsIn;

using AtraBase.Toolkit.StringHandler;

using StardewValley.Delegates;
using StardewValley.GameData.Museum;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;

/// <summary>
/// Builds the museum shop.
/// </summary>
internal static class MuseumShopBuilder
{
    /// <inheritdoc cref="ResolveItemQueryDelegate"/>
    internal static IEnumerable<ItemQueryResult> MuseumQuery(string key, string? arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string>? avoidItemIds, Action<string, string> logError)
    {
        if (Game1.getLocationFromName("ArchaeologyHouse") is not LibraryMuseum library)
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "library could not be found");
            yield break;
        }

        // argument parsing
        HashSet<string>? types = null;

        ReadOnlySpan<char> argsSpan = (arguments ?? string.Empty).AsSpan().Trim();
        if (argsSpan.Length > 0)
        {
            StreamSplit args = argsSpan.StreamSplit(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            while (args.MoveNext())
            {
                ReadOnlySpan<char> arg = args.Current.Word;

                if (arg.Length < 2 || arg[0] != '@')
                {
                    ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"argument '{arg}' is not valid.");
                    yield break;
                }

                if (arg.Equals("@has_type", StringComparison.OrdinalIgnoreCase))
                {
                    ReadOnlySpan<char> remainder = args.Remainder.TrimStart();
                    if (remainder.Length == 0 || remainder[0] == '@')
                    {
                        ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"argument '{arg}' was not given values");
                        yield break;
                    }
                    types ??= [];

                    do
                    {
                        if (!args.MoveNext())
                        {
                            break;
                        }

                        string typeString = args.Current.Word.ToString();
                        if (typeString[0] != '(' || typeString[^1] != ')' || ItemRegistry.GetTypeDefinition(typeString) is null)
                        {
                            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"type '{typeString}' is not a valid type.");
                            yield break;
                        }
                        types.Add(typeString);

                        remainder = args.Remainder.TrimStart();
                    }
                    while (remainder.Length > 0 && remainder[0] != '@');
                }
                else
                {
                    ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"argument '{arg}' is not valid.");
                    yield break;
                }
            }
        }

        // parse museum data for the rewards.
        Dictionary<string, MuseumRewards> museumRewardData;
        try
        {
            museumRewardData = DataLoader.MuseumRewards(Game1.content);
        }
        catch (Exception ex)
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"could not load museum data: {ex}");
            yield break;
        }

        if (museumRewardData.Count == 0)
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"museum data appears empty");
            yield break;
        }

        Dictionary<string, int> countsByTag = library.GetDonatedByContextTag(museumRewardData);
        HashSet<string>? repeats = avoidRepeat ? new() : null;

        foreach (string? item in GetEarnedMuseumRewards(museumRewardData, countsByTag).Concat(GetMailRewards()))
        {
            if (avoidItemIds?.Contains(item) == true)
            {
                continue;
            }

            ParsedItemData parsed = ItemRegistry.GetData(item);
            if (parsed.IsErrorItem)
            {
                continue;
            }

            if (types?.Contains(parsed.ItemType.Identifier) == false)
            {
                continue;
            }

            // track repeats and skip if they're already taken.
            if (repeats?.Add(item) == false)
            {
                continue;
            }
            yield return new(ItemRegistry.Create(item));
        }
    }

    #region helpers

    /// <summary>
    /// Iterates through museum data, yielding all valid rewards.
    /// </summary>
    /// <param name="museumRewardData">Museum data.</param>
    /// <param name="countsByTag">Represents rewards earned.</param>
    /// <returns>Valid rewards earned.</returns>
    private static IEnumerable<string> GetEarnedMuseumRewards(Dictionary<string, MuseumRewards> museumRewardData, Dictionary<string, int> countsByTag)
    {
        foreach (MuseumRewards reward in museumRewardData.Values)
        {
            string item = ItemRegistry.QualifyItemId(reward.RewardItemId);
            if (!string.IsNullOrEmpty(item) && CheckAchievedReward(reward, countsByTag))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<string> GetMailRewards()
    {
        if (AssetManager.MailFlags.Count == 0)
        {
            yield break;
        }

        Dictionary<string, string> mail = DataLoader.Mail(Game1.content);

        foreach (string? mailflag in Game1.player.mailReceived)
        {
            if (AssetManager.MailFlags.Contains(mailflag) && mail.TryGetValue(mailflag, out string? mailstring))
            {
                foreach (string candiate in mailstring.ParseItemsFromMail())
                {
                    yield return candiate;
                }
            }
        }

    }

    private static bool CheckAchievedReward(MuseumRewards reward, Dictionary<string, int> countsByTag)
    {
        switch(ItemRegistry.QualifyItemId(reward.RewardItemId))
        {
            case "(BC)21": // crystallarium
            case "(O)434": // stardrop
            case "(O)326": // translation guide
                return false;
        }

        foreach (MuseumDonationRequirement? tag in reward.TargetContextTags)
        {
            if (tag.Tag == string.Empty && tag.Count == -1)
            {
                if (countsByTag[tag.Tag] < LibraryMuseum.totalArtifacts)
                {
                    return false;
                }
            }
            else if (countsByTag[tag.Tag] < tag.Count)
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
