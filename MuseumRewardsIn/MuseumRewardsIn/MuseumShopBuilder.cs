using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewValley.GameData.Museum;
using StardewValley.Internal;
using StardewValley.Locations;

namespace MuseumRewardsIn;
internal static class MuseumShopBuilder
{
    internal static IList<ItemQueryResult> MuseumQuery(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
    {
        var library = Game1.getLocationFromName("ArchaeologyHouse") as LibraryMuseum;

        if (library is null)
        {
            return ItemQueryResolver.DefaultResolvers.ErrorResult(key, arguments, logError, "library could not be found");
        }

        var museumRewardData = Game1.content.Load<Dictionary<string, MuseumRewards>>("Data\\MuseumRewards");
        var items = new List<ItemQueryResult>();
        Dictionary<string, int> countsByTag = library.GetDonatedByContextTag(museumRewardData);

        foreach (var (id, reward) in museumRewardData)
        {
            if (!CheckAchievedReward(reward, countsByTag))
            {
                continue;
            }


        }

        return items;
    }

    private static bool CheckAchievedReward(MuseumRewards reward, Dictionary<string, int> countsByTag)
    {
        if (reward?.RewardItemId is null)
        {
            return false;
        }

        if (ItemRegistry.QualifyItemId(reward.RewardItemId) == "(O)326")
        {
            return false;
        }

        foreach (var tag in reward.TargetContextTags)
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
}
