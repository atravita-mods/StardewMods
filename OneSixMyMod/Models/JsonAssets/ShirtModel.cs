using System.Drawing;

namespace OneSixMyMod.Models.JsonAssets;

/// <summary>
/// A shirt model.
/// </summary>
/// <param name="Name"></param>
/// <param name="EnableWithMod"></param>
/// <param name="DisableWithMod"></param>
/// <param name="Description"></param>
/// <param name="HasFemaleVariant"></param>
/// <param name="Price"></param>
/// <param name="DefaultColor"></param>
/// <param name="Dyeable"></param>
/// <param name="Metadata"></param>
/// <param name="NameLocalization"></param>
/// <param name="DescriptionLocalization"></param>
/// <param name="TranslationKey"></param>
public record ShirtModel(
    string Name,
    string? EnableWithMod,
    string? DisableWithMod,
    string? Description,
    bool HasFemaleVariant,
    int Price,
    Color? DefaultColor,
    bool Dyeable,
    string? Metadata,
    Dictionary<string, string>? NameLocalization,
    Dictionary<string, string>? DescriptionLocalization,
    string? TranslationKey)
    : BaseIdModel(Name, EnableWithMod, DisableWithMod)
{
    internal FileInfo? MaleTexturePath { get; set; }
    internal FileInfo? FemaleTexturePath { get; set; }
    internal FileInfo? MaleColorPath { get; set; }
    internal FileInfo? FemaleColorPath { get; set; }
}