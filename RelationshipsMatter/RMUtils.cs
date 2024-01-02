using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraCore.Framework.Caches;

namespace RelationshipsMatter;

/// <summary>
/// Generalized utilities for this mod.
/// </summary>
internal static class RMUtils
{
    private static Lazy<Dictionary<string, HashSet<string>>> relations = new(GenerateRelationsMap);

    private static IAssetName asset = null!;

    internal static void Init(IGameContentHelper parser)
    {
        asset = parser.ParseAssetName("Data/Characters");
    }

    internal static void Reset(IReadOnlySet<IAssetName>? assets)
    {
        if ((assets is null || assets.Contains(asset)) && relations.IsValueCreated)
        {
            relations = new(GenerateRelationsMap);
        }
    }

    private static Dictionary<string, HashSet<string>> GenerateRelationsMap()
    {
        Dictionary<string, HashSet<string>>? ret = new();

        IDictionary<string, StardewValley.GameData.Characters.CharacterData> dispos = Game1.characterData;

        foreach((string npc, StardewValley.GameData.Characters.CharacterData? dispo) in dispos)
        {
            HashSet<string> relations = new();

            // get the love interest.
            string love = dispo.LoveInterest.Trim();

            if (NPCCache.GetByVillagerName(love) is not null)
            {
                relations.Add(love);
            }

            // get other relatives - this is of the form `name 'relationship'` ie `Marnie 'aunt'`.
            foreach (string relative in dispo.FriendsAndFamily.Keys)
            {
                if (NPCCache.GetByVillagerName(relative) is not null)
                {
                    relations.Add(relative);
                }
            }

            ret[npc] = relations;
        }

        return ret;
    }
}
