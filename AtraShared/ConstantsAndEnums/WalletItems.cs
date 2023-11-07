using AtraBase.Toolkit;

using CommunityToolkit.Diagnostics;

using NetEscapades.EnumGenerators;

using static System.Numerics.BitOperations;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// Wallet items as flags....
/// </summary>
[Flags]
[EnumExtensions]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = StyleCopErrorConsts.SelfEvident)]
public enum WalletItems
{
    /// <summary>
    /// No wallet items.
    /// </summary>
    None = 0,

    BearsKnowledge = 0b1,
    ClubCard = 0b1 << 1,
    RustyKey = 0b1 << 2,
    SkullKey = 0b1 << 3,
    SpecialCharm = 0b1 << 4,
    SpringOnion = 0b1 << 5,
    TranslationGuide = 0b1 << 6,
    TownKey = 0b1 << 7,
    MagicInk = 0b1 << 8,
    MagnifyingGlass = 0b1 << 9,
    DarkTalisman = 0b1 << 10,
}

/// <summary>
/// Extensions for the WalletItems enum.
/// </summary>
public static partial class WalletItemsExtensions
{
    private static readonly WalletItems[] _all = GetValues().Where(a => PopCount((uint)a) == 1).ToArray();

    /// <summary>
    /// Gets a span containing all wallet items.
    /// </summary>
    public static ReadOnlySpan<WalletItems> All => new(_all);

    /// <summary>
    /// Checks if this specific farmer has any single wallet item.
    /// </summary>
    /// <param name="farmer">Farmer to check.</param>
    /// <param name="items">Item to check for.</param>
    /// <returns>True if that farmer has this wallet item.</returns>
    public static bool HasSingleWalletItem(this Farmer farmer, WalletItems items)
    {
        Guard.IsEqualTo(PopCount((uint)items), 1);

        return items switch
        {
            WalletItems.BearsKnowledge => farmer.eventsSeen.Contains("2120303"),
            WalletItems.ClubCard => farmer.hasClubCard,
            WalletItems.RustyKey => farmer.hasRustyKey,
            WalletItems.SkullKey => farmer.hasSkullKey,
            WalletItems.SpecialCharm => farmer.hasSpecialCharm,
            WalletItems.SpringOnion => farmer.eventsSeen.Contains("3910979"),
            WalletItems.TranslationGuide => farmer.canUnderstandDwarves,
            WalletItems.TownKey => farmer.HasTownKey,
            WalletItems.MagicInk => farmer.hasMagicInk,
            WalletItems.MagnifyingGlass => farmer.hasMagnifyingGlass,
            WalletItems.DarkTalisman => farmer.hasDarkTalisman,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<bool>($"{items.ToStringFast()} does not correspond to a single wallet item!"),
        };
    }
}