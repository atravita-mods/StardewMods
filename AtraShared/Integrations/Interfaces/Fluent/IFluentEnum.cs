namespace AtraShared.Integrations.Interfaces.Fluent;

#pragma warning disable SA1314 // Type parameter names should begin with T. Reviewed.
/// <summary>A specialized type allowing access to Project Fluent translations for <typeparamref name="EnumType"/> values.</summary>
/// <typeparam name="EnumType">The type of values this instance allows retrieving translations for.</typeparam>
public interface IEnumFluent<EnumType> : IFluent<EnumType>
    where EnumType : struct, Enum
{
    /// <summary>Returns an <typeparamref name="EnumType"/> value for a given localized name, or <c>null</c> if none match.</summary>
    /// <param name="localizedName">The localized name.</param>
    EnumType? GetFromLocalizedName(string localizedName);

    /// <summary>Returns localized names for all possible <typeparamref name="EnumType"/> values.</summary>
    IEnumerable<string> GetAllLocalizedNames();
}
#pragma warning restore SA1314 // Type parameter names should begin with T