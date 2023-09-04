using AtraShared.ConstantsAndEnums;

using static System.Numerics.BitOperations;
using static StardewValley.GameStateQuery;

namespace AtraCore.Framework.GameStateQueries;

/// <summary>
/// Handles adding a GSQ that checks for a wallet item. wallet item.
/// </summary>
internal static class WalletItemsQuery
{
    /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate"/>
    /// <remarks>Checks if the given player has the specific wallet item.</remarks>
    internal static bool CheckWalletItem(string[] query, GameLocation location, Farmer player, Item targetItem, Item inputItem, Random random)
    {
        if (!ArgUtility.TryGet(query, 1, out string? playerKey, out string? error)
            || !ArgUtility.TryGet(query, 2, out string? walletItem, out error))
        {
            return Helpers.ErrorResult(query, error);
        }
        if (!WalletItemsExtensions.TryParse(walletItem, out WalletItems wallet, ignoreCase: true) || PopCount((uint)wallet) != 1)
        {
            return Helpers.ErrorResult(query, $"could not parse '{walletItem}' as a valid Wallet Item");
        }
        return Helpers.WithPlayer(player, playerKey, (Farmer target) => target.HasSingleWalletItem(wallet));
    }
}
