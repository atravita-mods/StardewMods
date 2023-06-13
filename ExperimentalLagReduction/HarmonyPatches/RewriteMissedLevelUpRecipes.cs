#if DEBUG
using System.Diagnostics;
#endif

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Shims.NetSeven;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Menus;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Re-writes the function that adds missed recipes.
/// </summary>
[HarmonyPatch(typeof(LevelUpMenu))]
internal class RewriteMissedLevelUpRecipes
{
    private const int SKILLCOUNT = 5; // the total number of skills.

    /// <summary>
    /// Rewrites <see cref="LevelUpMenu.AddMissedLevelRecipes"/> to be faster and less likely to throw exceptions.
    /// </summary>
    /// <param name="farmer">The relevant farmer.</param>
    /// <returns>True to continue to vanilla function, false otherwise.</returns>
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(LevelUpMenu.AddMissedLevelRecipes))]
    private static bool Prefix(Farmer farmer)
    {
        try
        {
            ModEntry.ModMonitor.Log($"Overriding level up recipes for {farmer.Name}");

            int[] buffer = new int[SKILLCOUNT];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = farmer.GetUnmodifiedSkillLevel(i);
            }
            ModEntry.ModMonitor.DebugOnlyLog($"Current levels for {farmer.Name} => {string.Join(',', buffer)}");

            // account for levels not "locked in" yet.
            foreach ((int skill, int level) in farmer.newLevels)
            {
                if (buffer.Length > skill && buffer[skill] >= level)
                {
                    buffer[skill] = Math.Max(level - 1, 0);
                }
            }
            ModEntry.ModMonitor.DebugOnlyLog($"Current levels after removing current level ups for {farmer.Name} => {string.Join(',', buffer)}");

#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif
            int maxLevel = buffer.Max();

            // crafting and cooking are entirely separate, we will handle them on separate threads.
            Task crafting = Task.Run(() =>
            {
                foreach ((string index, string recipe) in CraftingRecipe.craftingRecipes)
                {
                    if (farmer.craftingRecipes.ContainsKey(index))
                    {
                        continue;
                    }

                    if (ShouldUnlock(recipe.GetNthChunk('/', 4), buffer, maxLevel) && farmer.craftingRecipes.TryAdd(index, 0))
                    {
                        ModEntry.ModMonitor.Log($"Added crafting recipe {index} which was missing from skill level up for {farmer.Name}.");
                    }
                }
            });

            foreach ((string index, string recipe) in CraftingRecipe.cookingRecipes)
            {
                if (farmer.cookingRecipes.ContainsKey(index))
                {
                    continue;
                }

                if (ShouldUnlock(recipe.GetNthChunk('/', 3), buffer, maxLevel) && farmer.cookingRecipes.TryAdd(index, 0))
                {
                    ModEntry.ModMonitor.Log($"Added cooking recipe {index} which was missing from skill level up for {farmer.Name}.");
                }
            }

            crafting.Wait();

#if DEBUG
            ModEntry.ModMonitor.LogTimespan("Checking over recipes", sw);
#endif

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding default recipes", ex);
            return true;
        }
    }

    [Pure]
    private static int ParseLowestDigit(ReadOnlySpan<char> unlock)
    {
        int? ret = null;
        foreach (char c in unlock)
        {
            if (char.IsDigit(c))
            {
                int val = c - '0';
                if (!ret.HasValue || ret.Value > val)
                {
                    ret = val;
                }
            }
        }

        return ret is null ? -1 : ret.Value;
    }

    /// <summary>
    /// Should the recipe be unlocked given the current skill array.
    /// </summary>
    /// <param name="unlock">Unlock string.</param>
    /// <param name="levelArray">The player's level in each skill.</param>
    /// <param name="maxLevel">The maximum level the player has achieved.</param>
    /// <returns>true if farmer's past the level to unlock, false otherwise..</returns>
    [Pure]
    private static bool ShouldUnlock(ReadOnlySpan<char> unlock, int[] levelArray, int maxLevel)
    {
        // the shortest skill name is "Luck"
        // we also need at least one digit for the skill level.
        // so anything shorter can just be ignored.
        if (unlock.Length < 5)
        {
            return false;
        }

        // We're looking for a single number here.
        // This matches the game's behavior even if it's weird.
        int level = ParseLowestDigit(unlock);
        if (level < 0 || level > maxLevel)
        {
            return false;
        }

        // every skill has at least one uppercase letter in it.
        bool foundLetter = false;
        foreach (char c in unlock)
        {
            if (CharExtensions.IsAsciiLetterUpper(c))
            {
                foundLetter = true;
                break;
            }
        }
        if (!foundLetter)
        {
            return false;
        }

        for (int i = 0; i < levelArray.Length; i++)
        {
            if (levelArray[i] < level)
            {
                continue;
            }

            string skillName = Farmer.getSkillNameFromIndex(i);
            if (skillName is not null && unlock.Contains(skillName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
