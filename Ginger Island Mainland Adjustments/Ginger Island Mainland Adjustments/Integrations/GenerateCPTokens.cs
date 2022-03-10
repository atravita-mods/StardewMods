using AtraShared.Integrations.Interfaces;
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
    public static void AddTokens(IManifest manifest)
    {
        if (Globals.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher") is not IContentPatcherAPI api)
        {
            return;
        }

        api.RegisterToken(manifest, "IslandOpen", () =>
        {
            if ((Context.IsWorldReady || SaveGame.loaded is not null)
                && Game1.getLocationFromName("IslandSouth") is IslandSouth island)
            {
                return new[] { island.resortOpenToday.Value.ToString() };
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
            if (Context.IsWorldReady)
            {
                List<string> islanders = Islanders.Get();
                if (islanders.Count > 0)
                {
                    return islanders.ToArray();
                }
            }
            return null;
        });
    }
}