using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

namespace DrawFishPondsOverGrass;

/// <inheritdoc/>
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    public void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        // no snitch necessary, this mod entirely uses ForEachMatch patches.
        // harmony.Snitch(this.Monitor, this.ModManifest.UniqueID, transpilersOnly: true);
    }
}