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
        IContentPatcherAPI? api = Globals.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
        if (api is null)
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
    }
}