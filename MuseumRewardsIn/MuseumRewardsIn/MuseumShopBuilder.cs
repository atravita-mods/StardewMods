using System.Runtime.InteropServices;

using AtraBase.Models.RentedArrayHelpers;
using AtraBase.Toolkit.StringHandler;

using StardewValley.Delegates;
using StardewValley.GameData.Museum;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;

namespace MuseumRewardsIn;

/// <summary>
/// Builds the museum shop.
/// </summary>
internal static class MuseumShopBuilder
{
    /// <inheritdoc cref="ResolveItemQueryDelegate"/>
    internal static IList<ItemQueryResult> MuseumQuery(string key, string? arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string>? avoidItemIds, Action<string, string> logError)
    {
        LibraryMuseum? library = Game1.getLocationFromName("ArchaeologyHouse") as LibraryMuseum;

        if (library is null)
        {
            return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, "library could not be found");
        }

        // argument parsing
        HashSet<string>? types = null;
        int? count = null;

        ReadOnlySpan<char> argsSpan = (arguments ?? string.Empty).AsSpan().Trim();
        if (argsSpan.Length > 0)
        {
            StreamSplit args = argsSpan.StreamSplit(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            while (args.MoveNext())
            {
                ReadOnlySpan<char> arg = args.Current.Word;

                if (arg.Length < 2 || arg[0] != '@')
                {
                    return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"argument '{arg}' is not valid.");
                }

                if (arg.Equals("@has_type", StringComparison.OrdinalIgnoreCase))
                {
                    ReadOnlySpan<char> remainder = args.Remainder.TrimStart();
                    if (remainder.Length == 0 || remainder[0] == '@')
                    {
                        return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"argument '{arg}' was not given values");
                    }
                    types ??= new();

                    do
                    {
                        if (!args.MoveNext())
                        {
                            break;
                        }

                        string typeString = args.Current.Word.ToString();
                        if (typeString[0] != '(' || typeString[^1] != ')' || ItemRegistry.GetTypeDefinition(typeString) is null)
                        {
                            return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"type '{typeString}' is not a valid type.");
                        }
                        types.Add(typeString);

                        remainder = args.Remainder.TrimStart();
                    }
                    while (remainder.Length > 0 && remainder[0] != '@');

                }
                else if (arg.Equals("@count", StringComparison.OrdinalIgnoreCase))
                {
                    if (count is not null)
                    {
                        return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"Count argument was supplied twice.");
                    }

                    if (args.MoveNext() && int.TryParse(args.Current.Word, out int val) && val > 0)
                    {
                        count = val;
                    }
                    else
                    {
                        return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"Received invalid value for @Count");
                    }
                }
                else
                {
                    return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"argument '{arg}' is not valid.");
                }
            }
        }

        // parse museum data for the rewards.
        Dictionary<string, MuseumRewards> museumRewardData;
        try
        {
            museumRewardData = Game1.content.Load<Dictionary<string, MuseumRewards>>("Data\\MuseumRewards");
        }
        catch (Exception ex)
        {
            return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"could not load museum data: {ex}");
        }

        if (museumRewardData.Count == 0)
        {
            return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, $"museum data appears empty");
        }

        List<ItemQueryResult> items = new();
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
            items.Add(new(ItemRegistry.Create(item)));
        }

        if (count is null)
        {
            return items;
        }

        // SAFETY: Do not mutate items while the shuffler is active.
        ShuffledYielder<ItemQueryResult> shuffler = new(
            CollectionsMarshal.AsSpan(items),
            null,
            context?.Random ?? Utility.CreateDaySaveRandom(Utility.GetDeterministicHashCode(key), Utility.GetDeterministicHashCode(arguments)));
        List<ItemQueryResult> result = new(count.Value);

        foreach (ItemQueryResult? item in shuffler)
        {
            if (item is not null)
            {
                result.Add(item);
                if (result.Count >= count)
                {
                    break;
                }
            }
        }
        return result;
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
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }

            if (!CheckAchievedReward(reward, countsByTag))
            {
                continue;
            }

            yield return item;
        }
    }

    private static IEnumerable<string> GetMailRewards()
    {
        if (AssetManager.MailFlags.Count == 0)
        {
            yield break;
        }

        Dictionary<string, string> mail = Game1.content.Load<Dictionary<string, string>>("Data/mail");

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
