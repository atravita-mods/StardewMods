using System.Diagnostics;
using AtraShared.ConstantsAndEnums;

namespace AtraCore.Models;

[DebuggerDisplay("{itemType} - {Identifier} - {Mask}")]
public class DrawPrismaticModel
{
    public ItemTypeEnum itemType { get; set; } = ItemTypeEnum.SObject;

    public string Identifier { get; set; } = string.Empty;

    public string? Mask { get; set; } = string.Empty;
}
