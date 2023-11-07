﻿using AtraShared.Integrations.Interfaces.ContentPatcher;
using GingerIslandMainlandAdjustments.ScheduleManager;
using GingerIslandMainlandAdjustments.Utils;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.Integrations;

/// <summary>
/// Class that holds the method that generates the CP tokens for this mod.
/// </summary>
internal class GenerateCPTokens
{
    /// <summary>
    /// Adds the CP tokens for this mod.
    /// </summary>
    /// <param name="manifest">This mod's manifest.</param>
    internal static void AddTokens(IManifest manifest)
    {
        if (Globals.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher") is not IContentPatcherAPI api)
        {
            return;
        }
#warning - is this the token I want, or do I want is the island truly open for business?
        api.RegisterToken(manifest, "IslandOpen", () =>
        {
            if ((Context.IsWorldReady || SaveGame.loaded is not null)
                && Game1.getLocationFromName("IslandSouth") is IslandSouth island)
            {
                return new[] { (island.resortOpenToday.Value && island.resortRestored.Value).ToString() };
            }

            return null;
        });

        /***********
         * The following tokens are never ready at DayStart.
         * Use a higher refresh rate if you want to use them.
         ***********/
        api.RegisterToken(manifest, "Bartender", () => GIScheduler.Bartender is null ? null : new[] { GIScheduler.Bartender.displayName });
        api.RegisterToken(manifest, "Musician", () => GIScheduler.Musician is null ? null : new[] { GIScheduler.Musician.displayName });
        api.RegisterToken(manifest, "Islanders", () =>
        {
            if (Context.IsWorldReady && Game1.netWorldState.Value.IslandVisitors.Count != 0)
            {
                string[] ret = Game1.netWorldState.Value.IslandVisitors.ToArray();
                Array.Sort(ret, StringComparer.Ordinal);
                return ret;
            }
            return null;
        });
    }
}