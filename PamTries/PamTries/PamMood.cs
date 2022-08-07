using NetEscapades.EnumGenerators;

namespace PamTries;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1602 // Enumeration items should be documented. Should be obvious enough.
/// <summary>
/// Enum for Pam's mood.
/// </summary>
[EnumExtensions]
internal enum PamMood
{
    bad,
    neutral,
    good,
}
#pragma warning restore SA1602 // Enumeration items should be documented
#pragma warning restore SA1300 // Element should begin with upper-case letter