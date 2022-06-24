using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace EasierDartPuzzle;

/// <inheritdoc/>
internal class ModEntry : Mod
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets the game content helper for this mod.
    /// </summary>
    internal static IGameContentHelper GameContentHelper { get; private set; }

    /// <summary>
    /// Gets the translation helper for this mod.
    /// </summary>
    internal static ITranslationHelper TranslationHelper { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;

        GameContentHelper = helper.GameContent;
        TranslationHelper = helper.Translation;

        I18n.Init(helper.Translation);
        //Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}