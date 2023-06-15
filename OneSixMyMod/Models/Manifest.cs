namespace OneSixMyMod.Models;

public record ContentPackFor(string UniqueID, string? MinimumVersion);

public record Manifest(string UniqueID, ContentPackFor? ContentPackFor);
