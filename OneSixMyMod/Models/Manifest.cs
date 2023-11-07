using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneSixMyMod.Models;

/// <summary>
/// A dependency.
/// </summary>
/// <param name="UniqueID">The unique ID of the required mod.</param>
/// <param name="MinimumVersion">Whether a minimum version should be enforced.</param>
/// <param name="IsRequired">Whether that mod is required.</param>
public record ModDependency(string UniqueID, string? MinimumVersion, bool IsRequired = true);

/// <summary>
/// A model that represents the manifest of a mod.
/// </summary>
/// <param name="UniqueID">The unique ID of this mod.</param>
/// <param name="ContentPackFor">The mod that should load this one.</param>
/// <param name="Dependencies">Any listed dependencies.</param>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:Property summary documentation should match accessors", Justification = "Models.")]
public record Manifest(string UniqueID, ModDependency? ContentPackFor, ModDependency[]? Dependencies)
{
    /// <summary>
    /// Any other data on the manifest.
    /// </summary>
    [JsonExtensionData] internal IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();

    /// <summary>
    /// The directory this mod is in.
    /// </summary>
    internal DirectoryInfo? Location { get; set; }

    /// <summary>
    /// If set, refers to a reason why this particular mod could not be fully migrated.
    /// </summary>
    internal List<string> MigrationFailureReason { get; set; } = new();
}
