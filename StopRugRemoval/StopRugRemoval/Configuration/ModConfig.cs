// Ignore Spelling: Crystalarium Bobbers Dressup

using AtraShared.Integrations.GMCMAttributes;
using StardewModdingAPI.Utilities;
using StardewValley.Locations;

namespace StopRugRemoval.Configuration;

/// <summary>
/// Configuration class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Keeping fields near accessors.")]
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not the entire mod is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    #region rugs

    /// <summary>
    /// Gets or sets a value indicating whether or not I should be able to place rugs outside.
    /// </summary>
    public bool CanPlaceRugsOutside { get; set; } = false;

#if DEBUG
    /// <summary>
    /// Gets or sets a value indicating whether or not I should be able to place rugs under things.
    /// </summary>
    public bool CanPlaceRugsUnder { get; set; } = true;
#endif

    /// <summary>
    /// Gets or sets a value indicating whether planting on rugs should be allowed.
    /// </summary>
    public bool PreventPlantingOnRugs { get; set; } = true;

    #endregion

    /// <summary>
    /// Gets or sets a value indicating whether or not to prevent the removal of items from a table.
    /// </summary>
    [GMCMSection("Placement", 0)]
    public bool PreventRemovalFromTable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether grass should be placed under objects.
    /// </summary>
    [GMCMSection("Placement", 0)]
    public bool PlaceGrassUnder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether jukeboxes should be playable everywhere.
    /// </summary>
    [GMCMSection("Placement", 0)]
    public bool JukeboxesEverywhere { get; set; } = true;

    [GMCMSection("Placement", 0)]
    public bool ChestSwap { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether golden coconuts should be allowed to appear off the island, if you've cracked at least one before.
    /// </summary>
    public bool GoldenCoconutsOffIsland { get; set; } = false;

#if DEBUG
    /// <summary>
    /// Gets or sets a value indicating whether changes to alerts should happen.
    /// </summary>
    public bool AlertChanges { get; set; } = true;

#endif

    private float phoneSpeedUpFactor = 1.0f;

    /// <summary>
    /// Gets or sets a value indicating how much to speed up the phone calls by.
    /// </summary>
    [GMCMInterval(0.1)]
    [GMCMRange(1.0, 5.0)]
    public float PhoneSpeedUpFactor
    {
        get => this.phoneSpeedUpFactor;
        set => this.phoneSpeedUpFactor = Math.Clamp(value, 1.0f, 5.0f);
    }

    private int craneGameDifficulty = 3;

    /// <summary>
    /// Gets or sets a value indicating how hard to make the crane game.
    /// </summary>
    [GMCMRange(1, 7)]
    public int CraneGameDifficulty
    {
        get => this.craneGameDifficulty;
        set => this.craneGameDifficulty = Math.Clamp(value, 1, 7);
    }

    #region secret notes

    /// <summary>
    /// Gets or sets a value indicating whether or not this mod should handle spawning secret notes.
    /// </summary>
    [GMCMSection("SecretNotesOverride", 1)]
    public bool OverrideSecretNotes { get; set; } = true;

    private float maxNoteChance = GameLocation.FIRST_SECRET_NOTE_CHANCE;

    /// <summary>
    /// Gets or sets a value indicating the maximum chance of spawning a secret note.
    /// </summary>
    [GMCMRange(0, 1)]
    [GMCMInterval(0.01)]
    [GMCMSection("SecretNotesOverride", 1)]
    public float MaxNoteChance
    {
        get => this.maxNoteChance;
        set => this.maxNoteChance = Math.Clamp(value, 0f, 1f);
    }

    private float minNoteChance = GameLocation.LAST_SECRET_NOTE_CHANCE;

    /// <summary>
    /// Gets or sets a value indicating the minimum chance of spawning a secret note.
    /// </summary>
    [GMCMRange(0, 1)]
    [GMCMInterval(0.01)]
    [GMCMSection("SecretNotesOverride", 1)]
    public float MinNoteChance
    {
        get => this.minNoteChance;
        set => this.minNoteChance = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    /// <summary>
    /// Gets or sets a value indicating whether or not the bet1k/bet10k buttons should appear.
    /// </summary>
    public bool BetIcons { get; set; } = true;

    /// <summary>
    /// Gets or sets keybind to use to remove an item from a table.
    /// </summary>
    [GMCMSection("Placement", 0)]
    public KeybindList FurniturePlacementKey { get; set; } = KeybindList.Parse("LeftShift + Z");

    /// <summary>
    /// Gets or sets a value indicating whether or not to hide crab pots during events.
    /// </summary>
    public bool HideCrabPots { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SObjects that are bombed that are forage should be saved.
    /// </summary>
    [GMCMSection("Bombs", 50)]
    public bool SaveBombedForage { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether jukebox songs should be removed from the menu if they're not
    /// actually currently accessible.
    /// </summary>
    public bool FilterJukeboxSongs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not napalm rings should affect safe areas.
    /// </summary>
    [GMCMSection("Bombs", 50)]
    public bool NapalmInSafeAreas { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating the behavior for crystalaria.
    /// </summary>
    public CrystalariumBehavior CrystalariumBehavior { get; set; } = CrystalariumBehavior.Vanilla;

    /// <summary>
    /// Gets or sets a value indicating the behavior for signs.
    /// </summary>
    public SignBehavior SignBehavior { get; set; } = SignBehavior.Vanilla;

    /// <summary>
    /// Gets or sets a value indicating whether or not to confirm bomb placement in safe areas.
    /// </summary>
    [GMCMDefaultIgnore]
    public ConfirmationEnum BombsInSafeAreas { get; set; } = ConfirmationEnum.On;

    /// <summary>
    /// Gets or sets a value indicating whether or not to confirm bomb placement in dangerous areas.
    /// </summary>
    [GMCMDefaultIgnore]
    public ConfirmationEnum BombsInDangerousAreas { get; set; } = ConfirmationEnum.Off;

    /// <summary>
    /// Gets or sets a value indicating whether or not to confirm warps in safe areas.
    /// </summary>
    [GMCMDefaultIgnore]
    public ConfirmationEnum WarpsInSafeAreas { get; set; } = ConfirmationEnum.On;

    /// <summary>
    /// Gets or sets a value indicating whether or not to confirm warps in dangerous areas.
    /// </summary>
    [GMCMDefaultIgnore]
    public ConfirmationEnum WarpsInDangerousAreas { get; set; } = ConfirmationEnum.NotInMultiplayer;

    /// <summary>
    /// Gets or sets a value indicating whether or not to confirm the return scepter in safe areas.
    /// </summary>
    [GMCMDefaultIgnore]
    public ConfirmationEnum ReturnScepterInSafeAreas { get; set; } = ConfirmationEnum.On;

    /// <summary>
    /// Gets or sets a value indicating whether or not to confirm the return scepter in dangerous areas.
    /// </summary>
    [GMCMDefaultIgnore]
    public ConfirmationEnum ReturnScepterInDangerousAreas { get; set; } = ConfirmationEnum.NotInMultiplayer;

    /// <summary>
    /// Gets or sets map to which locations are considered safe.
    /// </summary>
    [GMCMDefaultIgnore]
    public Dictionary<string, IsSafeLocationEnum> SafeLocationMap { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether or not to edit Elliott's event.
    /// </summary>
    public bool EditElliottEvent { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to remove duplicate npcs if found.
    /// </summary>
    public bool RemoveDuplicateNPCs { get; set; } = false;

    /// <summary>
    /// Pre-populates locations.
    /// </summary>
    /// <returns>Whether or not anything was added to the locations list.</returns>
    internal bool PrePopulateLocations()
    {
        if (Game1.locations?.Count is null or 0)
        {
            return false;
        }

        bool changed = false;

        Utility.ForEachLocation(loc =>
        {
            if (loc is SlimeHutch or Town or IslandWest || loc.IsFarm || loc.IsGreenhouse)
            {
                changed |= this.SafeLocationMap.TryAdd(loc.Name, IsSafeLocationEnum.Safe);
            }
            else if (loc is MineShaft or VolcanoDungeon or BugLand)
            {
                changed |= this.SafeLocationMap.TryAdd(loc.Name, IsSafeLocationEnum.Dangerous);
            }
            else
            {
                changed |= this.SafeLocationMap.TryAdd(loc.Name, IsSafeLocationEnum.Dynamic);
            }
            return true;
        });

        return changed;
    }
}
