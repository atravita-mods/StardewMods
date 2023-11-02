using System.Diagnostics;
using AtraShared.ConstantsAndEnums;

namespace AtraCore.Framework.Models;

/// <summary>
/// The model used for DrawPrismatic's CP integration.
/// </summary>
[DebuggerDisplay("{ItemType} - {Identifier} - {Mask}")]
public sealed class DrawPrismaticModel
{
    /// <summary>
    /// Gets or sets the type for the item.
    /// </summary>
    public ItemTypeEnum ItemType { get; set; } = ItemTypeEnum.SObject;

    /// <summary>
    /// Gets or sets some sort of identifier - either the ID or the name.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the internal path to the mask.
    /// </summary>
    public string? Mask { get; set; } = string.Empty;
}
