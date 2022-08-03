using Microsoft.Toolkit.Diagnostics;

namespace AtraShared.Utils.HarmonyHelper.Attributes;

public sealed class RequireModAttribute : Attribute
{
    public RequireModAttribute(string uniqueID)
    {
        Guard.IsNotNull(uniqueID, nameof(uniqueID));
        this.UniqueID = uniqueID;
    }

    public RequireModAttribute(string uniqueID, string minversion)
        : this(uniqueID)
        => this.MinVersion = minversion;

    public RequireModAttribute(string uniqueID, string minversion, string maxVersion)
    {
        this.UniqueID = uniqueID;
        this.MinVersion = minversion;
        this.MaxVersion = maxVersion;
    }

    internal string UniqueID { get; init; }

    internal string MinVersion { get; init; } = string.Empty;

    internal string MaxVersion { get; init; } = string.Empty;

}
