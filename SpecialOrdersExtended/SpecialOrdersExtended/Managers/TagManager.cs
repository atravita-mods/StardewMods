using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;

using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;

namespace SpecialOrdersExtended.Managers;

/// <summary>
/// Static class to hold tag-management functions.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Reviewed.")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class TagManager
{
#region random

    private static Random? random;

    /// <summary>
    /// Gets a seeded random that changes once per in-game week.
    /// </summary>
    internal static Random Random
    {
        get
        {
            if (random is null)
            {
                random = new Random(((int)Game1.uniqueIDForThisGame * 26) + (int)((Game1.stats.DaysPlayed / 7) * 36));
                random.PreWarm();
            }
            return random;
}
    }

    /// <summary>
    /// Delete's the random so it can be reset later.
    /// </summary>
    internal static void ResetRandom()
    {
        if (Game1.dayOfMonth % 7 == 0)
        {
            random = null;
        }
    }

#endregion

#region cache

    private static readonly ConcurrentDictionary<string, bool> Cache = new();
    private static int lastTick = -1;

    /// <summary>
    /// Clears the cache.
    /// </summary>
    internal static void ClearCache()
    {
        lastTick = -1;
        Cache.Clear();
    }

#endregion

    /// <summary>
    /// Prefixes CheckTag to handle special mod tags.
    /// </summary>
    /// <param name="__result">the result for the original function.</param>
    /// <param name="tag">tag to check.</param>
    /// <returns>true to continue to the vanilla function, false otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SpecialOrder.CheckTag))]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool PrefixCheckTag(ref bool __result, string tag)
    {
        {
            if (ModEntry.Config.UseTagCache && Cache.TryGetValue(tag, out bool result))
            {
                if (Game1.ticks != lastTick)
                {
                    Cache.Clear();
                    lastTick = Game1.ticks;
                }
                else
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"Hit cache: {tag}, {result}", LogLevel.Trace);
                    __result = result;
                    return false;
                }
            }
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Checking tag {tag}", LogLevel.Trace);
        try
        {
            if (tag.TrySplitOnce('_', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
            {
                first = first.Trim();
                second = second.Trim();

                if (first.Length > 256)
                {
                    // not a tag I recognize by far, bail to original function.
                    return true;
                }

                // SAFETY: length was checked earlier, caps to 256
                Span<char> tagName = stackalloc char[first.Length + 10];
                int copiedCount = first.ToLowerInvariant(tagName);
                if (copiedCount < 0)
                {
                    ModEntry.ModMonitor.LogOnce($"Issue lowercasing {first}, what.", LogLevel.Warn);
                    return true;
                }
                tagName = tagName[..copiedCount];
                switch (tagName)
                {
                    case "year":
                        // year_X
                        if (int.TryParse(second, out int yearval))
                        {
                            __result = Game1.year == yearval;
                            return false;
                        }
                        break;
                    case "atleastyear":
                        // atleastyear_X
                        if (int.TryParse(second, out int yearnum))
                        {
                            __result = Game1.year >= yearnum;
                            return false;
                        }
                        break;
                    case "yearunder":
                        // yearunder_X
                        if (int.TryParse(second, out int yearint))
                        {
                            __result = Game1.year < yearint;
                            return false;
                        }
                        break;
                    case "week":
                        // week_X
                        if (int.TryParse(second, out int weeknum))
                        {
                            __result = (Game1.dayOfMonth + 6) / 7 == weeknum;
                            return false;
                        }
                        break;
                    case "daysplayed":
                    {
                        // daysplayed_X, daysplayed_under_X
                        ReadOnlySpan<char> daysSpan = second;
                        bool inverse = false;
                        if (second.TrySplitOnce('_', out ReadOnlySpan<char> a, out ReadOnlySpan<char> b))
                        {
                            daysSpan = b;
                            inverse = a.Equals("under", StringComparison.OrdinalIgnoreCase);
                        }
                        if (int.TryParse(daysSpan, out int daysPlayed))
                        {
                            __result = Game1.stats.DaysPlayed >= daysPlayed;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "anyplayermail":
                    {
                        // anyplayermail_mailkey, anyplayermail_mailkey_not
                        string mail = HandleTrailingNot(second, out bool inverse).ToString();
                        __result = Game1.getAllFarmers().Any((Farmer f) => f.mailReceived.Contains(mail));
                        if (inverse)
                        {
                            __result = !__result;
                        }
                        return false;
                    }
                    case "anyplayerseenevent":
                    {
                        // anyplayerseenevent_eventID, anyplayerseenevent_eventID_not
                        string evt = HandleTrailingNot(second, out bool inverse).ToString();
                        __result = Game1.getAllFarmers().All((Farmer f) => !f.eventsSeen.Contains(evt));
                        if (inverse)
                        {
                            __result = !__result;
                        }
                        return false;
                    }
                    case "dropboxroom":
                    {
                        // dropboxRoom_roomName
                        string roomname = second.ToString();
                        foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                        {
                            if (specialOrder.questState.Value != SpecialOrderStatus.InProgress)
                            {
                                continue;
                            }
                            foreach (OrderObjective objective in specialOrder.objectives)
                            {
                                if (objective is DonateObjective donateobjective && donateobjective.dropBoxGameLocation.Value.Equals(roomname, StringComparison.OrdinalIgnoreCase))
                                {
                                    __result = true;
                                    return false;
                                }
                            }
                        }
                        __result = false;
                        return false;
                    }
                    case "conversation":
                    {
                        // conversation_CTname, conversation_CTname_not
                        string conversationTopic = HandleTrailingNot(second, out bool inverse).ToString();
                        __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.activeDialogueEvents.ContainsKey(conversationTopic));
                        if (inverse)
                        {
                            __result = !__result;
                        }
                        return false;
                    }
                    case "married":
                    {
                        // married_NPCname, married_NPCname_not
                        string spouse = HandleTrailingNot(second, out bool inverse).ToString();
                        __result = NPCCache.GetByVillagerName(spouse)?.getSpouse() is not null;
                        if (inverse)
                        {
                            __result = !__result;
                        }
                        return false;
                    }
                    case "profession":
                    {
                        // profession_name_skill, profession_name_skill_not
                        ReadOnlySpan<char> profession = HandleTrailingNot(second, out bool inverse);
                        string? skill = null;
                        if (profession.TrySplitOnce('_', out ReadOnlySpan<char> a, out ReadOnlySpan<char> b))
                        {
                            profession = a;
                            skill = b.ToString();
                        }
                        __result = false;
                        if (GetProfession(profession, skill) is int professionInt)
                        {
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.professions.Contains(professionInt));
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "hasspecialitem":
                    {
                        // hasspecialitem_X, hasspecialitem_X_not
                        ReadOnlySpan<char> special = HandleTrailingNot(second, out bool inverse);
                        if (WalletItemsExtensions.TryParse(special, out WalletItems item, ignoreCase: true)
                                    && BitOperations.PopCount((uint)item) == 1)
                        {
                            __result = Game1.getAllFarmers().Any(farmer => farmer is not null && farmer.HasSingleWalletItem(item));
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "craftingrecipe":
                    {
                        // craftingrecipe_X, craftingrecipe_X_not
                        string recipe = HandleTrailingNot(second, out bool inverse).ToString();
                        __result = false;
                        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.craftingRecipes.ContainsKey(recipe)))
                        {
                            __result = true;
                        }
                        else
                        {
                            recipe = recipe.Replace('-', ' ');
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.craftingRecipes.ContainsKey(recipe));
                        }
                        if (inverse)
                        {
                            __result = !__result;
                        }
                        return false;
                    }
                    case "cookingrecipe":
                    {
                        // cookingrecipe_X, cookingrecipe_X_not
                        string recipe = HandleTrailingNot(second, out bool inverse).ToString();
                        __result = false;
                        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.cookingRecipes.ContainsKey(recipe)))
                        {
                            __result = true;
                        }
                        else
                        {
                            recipe = recipe.Replace('-', ' ');
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.cookingRecipes.ContainsKey(recipe));
                        }
                        if (inverse)
                        {
                            __result = !__result;
                        }
                        return false;
                    }
                    case "achievement":
                    {
                        // achievement_X, achievement_X_not
                        ReadOnlySpan<char> achievement = HandleTrailingNot(second, out bool inverse);
                        if (int.TryParse(achievement, out int val))
                        {
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.achievements.Contains(val));
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "haskilled":
                    {
                        // haskilled_Monster-name_X, haskilled_Monster-name_under_X
                        if (HandleIntermediateUnder(second, out ReadOnlySpan<char> monsterSpan, out bool inverse, out int count) && monsterSpan.Length > 0)
                        {
                            string monster = monsterSpan.ToString().Replace('-', ' ');
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.getMonstersKilled(monster) >= count);
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "friendship":
                    {
                        // friendship_NPCName_X, friendship_NPCName_under_X
                        if (HandleIntermediateUnder(second, out ReadOnlySpan<char> friendSpan, out bool inverse, out int friendshipNeeded) && friendSpan.Length > 0)
                        {
                            string friend = friendSpan.ToString();
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.getFriendshipLevelForNPC(friend) >= friendshipNeeded);
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "minelevel":
                    {
                        // minelevel_X, minelevel_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int mineLevel))
                        {
                            __result = Utility.GetAllPlayerDeepestMineLevel() >= mineLevel;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "houselevel":
                    {
                        // houselevel_X, houselevel_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int houseLevel))
                        {
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.HouseUpgradeLevel >= houseLevel);
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "moneyearned":
                    {
                        // moneyearned_X, moneyearned_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int moneyEarned) && moneyEarned > 0)
                        {
                            __result = Game1.MasterPlayer.totalMoneyEarned >= moneyEarned;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "skilllevel":
                    {
                        // skilllevel_skill_X, skilllevel_skill_under_X
                        if (HandleIntermediateUnder(second, out ReadOnlySpan<char> skillSpan, out bool inverse, out int levelWanted))
                        {
                            if (SkillsExtensions.TryParse(skillSpan, out Skills skill, ignoreCase: true) && BitOperations.PopCount((uint)skill) == 1)
                            {
                                __result = Game1.getAllFarmers().Any(farmer => farmer.GetSkillLevelFromEnum(skill, includeBuffs: false) >= levelWanted);
                            }
                            else
                            {
                                string skillString = skillSpan.ToString();
                                __result = ModEntry.SpaceCoreAPI is not null && ModEntry.SpaceCoreAPI.GetCustomSkills().Contains(skillString)
                                        && Game1.getAllFarmers().Any(farmer => ModEntry.SpaceCoreAPI.GetLevelForCustomSkill(farmer, skillString) >= levelWanted);
                            }

                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "stats":
                    {
                        // stats_statsname_X, stats_statsname_under_X
                        if (HandleIntermediateUnder(second, out ReadOnlySpan<char> statsSpan, out bool inverse, out int stat))
                        {
                            string statKey = statsSpan.ToString();
                            __result = Game1.getAllFarmers().Any((Farmer farmer) => StatsManager.GrabBasicProperty(statKey, farmer.stats) >= stat);
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "walnutcount":
                    {
                        // walnutcount_X, walnutcount_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int walnutCount))
                        {
                            __result = Game1.netWorldState.Value.GoldenWalnutsFound >= walnutCount;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "specialorderscompleted":
                    {
                        // specialorderscompleted_X, specialorderscompleted_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int required))
                        {
                            __result = Game1.player.team.completedSpecialOrders.Count() >= required;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "slots":
                    {
                        // slots_X, slots_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int required))
                        {
                            __result = Club.timesPlayedSlots >= required;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "blackjack":
                    {
                        // blackjack_X, blackjac_under_X
                        if (HandleIntermediateUnder(second, out _, out bool inverse, out int required))
                        {
                            __result = Club.timesPlayedCalicoJack >= required;
                            if (inverse)
                            {
                                __result = !__result;
                            }
                            return false;
                        }
                        break;
                    }
                    case "random":
                        // random_x
                        return float.TryParse(second, out float result) && Random.OfChance(result); // not convinced on this implementation. Should I save values instead?
                    default:
                        // Not a tag I recognize, return true.
                        return true;
                }
                ModEntry.ModMonitor.Log($"Invalid tag {tag}.", LogLevel.Warn);
            }

        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"checking tag {tag}", ex);
        }
        return true; // continue to base code.
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last - 200)]
    [HarmonyPatch(nameof(SpecialOrder.CheckTag))]
    private static void WatchTag(bool __result, string tag)
    {
        if (ModEntry.Config.UseTagCache)
        {
            if (Game1.ticks != lastTick)
            {
                Cache.Clear();
                lastTick = Game1.ticks;
            }

            Cache[tag] = __result;
        }
    }

    private static ReadOnlySpan<char> HandleTrailingNot(ReadOnlySpan<char> second, out bool inverse)
    {
        ReadOnlySpan<char> ret;
        inverse = false;

        second.TrimEnd('_');
        int idx = second.LastIndexOf('_');
        if (idx < 0)
        {
            ret = second;
        }
        else
        {
            ret = second[..idx];
            inverse = second[(idx + 1)..].Equals("not", StringComparison.OrdinalIgnoreCase);
        }

        return ret;
    }

    private static bool HandleIntermediateUnder(ReadOnlySpan<char> second, out ReadOnlySpan<char> tag, out bool inverse, out int count)
    {
        // ex stats_statsname_X, stats_statsname_under_X
        second.Trim();
        tag = second;
        inverse = false;
        count = default;

        // number is at the end, let's go grab that.
        int idx = second.LastIndexOf('_');
        if (idx < 0)
        {
            return false;
        }

        ReadOnlySpan<char> numberSpan = second[(idx + 1)..];
        ReadOnlySpan<char> remainder = second[..idx];
        if (!int.TryParse(numberSpan, out count))
        {
            return false;
        }

        // possible "under"
        idx = remainder.LastIndexOf('_');
        if (idx < 0)
        {
            if (remainder.Equals("under", StringComparison.OrdinalIgnoreCase))
            {
                tag = ReadOnlySpan<char>.Empty;
                inverse = true;
            }
            else
            {
                tag = remainder;
            }
            return true;
        }
        else
        {
            tag = remainder[..idx];
            inverse = remainder[(idx + 1)..].Equals("under", StringComparison.OrdinalIgnoreCase);
            return true;
        }
    }

    /// <summary>
    /// Returns the integer ID of a profession.
    /// </summary>
    /// <param name="profession">Profession name.</param>
    /// <param name="skill">Skill name (leave null for vanilla skill).</param>
    /// <returns>Integer profession, null for not found.</returns>
    private static int? GetProfession(ReadOnlySpan<char> profession, string? skill = null)
    {
        if (profession.Length > 256)
        {
            // please no spacecore professions longer than 256 bloody characters.
            return null;
        }

        // SAFETY: length checked earlier.
        Span<char> lowercased = stackalloc char[profession.Length + 10];
        int copiedCount = profession.ToLowerInvariant(lowercased);
        if (copiedCount < 0)
        {
            ModEntry.ModMonitor.LogOnce($"Issue lowercasing profession '{profession}'", LogLevel.Warn);
            return null;
        }

        lowercased = lowercased[..copiedCount];
        int? professionNumber = lowercased switch
        {
            /* Farming professions */
            "rancher" => Farmer.rancher,
            "tiller" => Farmer.tiller,
            "coopmaster" => Farmer.butcher, // [sic]
            "shepherd" => Farmer.shepherd,
            "artisan" => Farmer.artisan,
            "agriculturist" => Farmer.agriculturist,
            /* Fishing professions */
            "fisher" => Farmer.fisher,
            "trapper" => Farmer.trapper,
            "angler" => Farmer.angler,
            "pirate" => Farmer.pirate,
            "mariner" => Farmer.baitmaster, // [sic]
            "luremaster" => Farmer.mariner, // [sic]
            /* Foraging professions */
            "forester" => Farmer.forester,
            "gatherer" => Farmer.gatherer,
            "lumberjack" => Farmer.lumberjack,
            "tapper" => Farmer.tapper,
            "botanist" => Farmer.botanist,
            "tracker" => Farmer.tracker,
            /* Mining professions */
            "miner" => Farmer.miner,
            "geologist" => Farmer.geologist,
            "blacksmith" => Farmer.blacksmith,
            "prospector" => Farmer.burrower, // [sic]
            "excavator" => Farmer.excavator,
            "gemologist" => Farmer.gemologist,
            /* Combat professions */
            "fighter" => Farmer.fighter,
            "scout" => Farmer.scout,
            "brute" => Farmer.brute,
            "defender" => Farmer.defender,
            "acrobat" => Farmer.acrobat,
            "desperado" => Farmer.desperado,
            _ => null
        };
        if (professionNumber is null && skill is not null)
        {
            string professionString = profession.ToString();
            try
            {
                if (ModEntry.SpaceCoreAPI is not null && ModEntry.SpaceCoreAPI.GetCustomSkills().Contains(skill))
                {
                    professionNumber = ModEntry.SpaceCoreAPI.GetProfessionId(skill, professionString);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or NullReferenceException)
            {
                ModEntry.ModMonitor.Log(I18n.SkillNotFound(professionString, skill), LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("looking up profession", ex);
            }
        }
        return professionNumber;
    }
}
