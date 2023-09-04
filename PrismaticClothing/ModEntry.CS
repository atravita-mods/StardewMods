using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace PrismaticClothing;

/// <inheritdoc/>
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            this.Monitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}
