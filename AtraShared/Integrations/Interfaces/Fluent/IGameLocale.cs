using System.Globalization;

namespace AtraShared.Integrations.Interfaces.Fluent;

/// <summary>An instance representing a specific game locale, be it a built-in one or a mod-provided one.</summary>
public interface IGameLocale
{
    /// <summary>The locale code of this locale (for example, <c>en-US</c>).</summary>
    string LocaleCode { get; }

    /// <summary>The <see cref="CultureInfo"/> for this locale.</summary>
    CultureInfo CultureInfo { get; }

    /// <summary>Whether this locale is a built-in one.</summary>
    bool IsBuiltInLocale { get; }

    /// <summary>Whether this locale is a mod-provided one.</summary>
    bool IsModLocale { get; }
}
