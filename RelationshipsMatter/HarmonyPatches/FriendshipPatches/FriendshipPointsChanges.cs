using HarmonyLib;

namespace RelationshipsMatter.HarmonyPatches.FriendshipPatches;

/// <summary>
/// Harmony patches to affect point changes for friendship.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
internal static class FriendshipPointsChanges
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Farmer.changeFriendship))]
    private static void PrefixChangeFriendship(ref int amount)
    {
        if (amount > 0)
        {
            amount = (int)(amount * ModEntry.Config.FriendshipGainFactor);
        }
        else
        {
            amount = (int)(amount * ModEntry.Config.FriendshipLossFactor);
        }
    }
}
