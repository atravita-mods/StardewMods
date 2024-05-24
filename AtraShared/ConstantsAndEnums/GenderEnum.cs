using NetEscapades.EnumGenerators;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// An enum referring to the gender of an NPC.
/// </summary>
[EnumExtensions]
public enum Gender
{
    /// <summary>
    /// Invalid - this is not a valid gender.
    /// </summary>
    Invalid = -2,

    /// <summary>
    /// Male.
    /// </summary>
    Male = NPC.male,

    /// <summary>
    /// Female.
    /// </summary>
    Female = NPC.female,

    /// <summary>
    /// Undefined (usually used for, say, the dwarf.)
    /// </summary>
    Undefined = NPC.undefined,
}