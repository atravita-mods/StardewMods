using HarmonyLib;

using MiniAtraShared.Models;

namespace RecipeSortFix;

/// <inheritdoc/>
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        Patcher.Apply(new Harmony(this.ModManifest.UniqueID));
    }
}
