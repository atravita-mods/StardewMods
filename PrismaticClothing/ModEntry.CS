using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace PrismaticClothing;
/// <inheritdoc/>
internal class ModEntry : Mod
{
    internal static IMonitor ModMonitor = null!;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

}
