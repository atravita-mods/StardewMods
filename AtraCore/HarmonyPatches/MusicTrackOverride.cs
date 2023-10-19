namespace AtraCore.HarmonyPatches;

using AtraShared.Utils.Extensions;

using HarmonyLib;

[HarmonyPatch(typeof(Utility))]
internal static class MusicTrackOverride
{
    [HarmonyPatch(nameof(Utility.getSongTitleFromCueName))]
    private static bool Prefix(string cueName, ref string __result)
    {
        try
        {
            Dictionary<string, string> overrides = Game1.content.Load<Dictionary<string, string>>(AtraCoreConstants.MusicNameOverride);
            if (overrides.TryGetValue(cueName, out string? data))
            {
                string tokenizedName = TokenParser.ParseText(data, Random.Shared);

                if (!string.IsNullOrWhiteSpace(tokenizedName))
                {
                    __result = tokenizedName;
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("override jukebox names", ex);
        }
        return true;
    }
}
