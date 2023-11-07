// Ignore Spelling: Atra

namespace AtraCore;

using StardewModdingAPI.Utilities;

/// <summary>
/// Holds useful constants.
/// </summary>
public static class AtraCoreConstants
{
    /// <summary>
    /// The path to the Prismatic Mask Data asset.
    /// </summary>
    public static readonly string PrismaticMaskData = PathUtilities.NormalizeAssetName("Mods/atravita/DrawPrismaticData");

    /// <summary>
    /// The path to the Equip Data Extensions asset.
    /// </summary>
    public static readonly string EquipData = PathUtilities.NormalizeAssetName("Mods/atravita/EquipData");

    /// <summary>
    /// Adds a data asset to allow using tokenized strings to override cue names.
    /// </summary>
    public static readonly string MusicNameOverride = PathUtilities.NormalizeAssetName("Mods/atravita/SongNameOverride");
}
