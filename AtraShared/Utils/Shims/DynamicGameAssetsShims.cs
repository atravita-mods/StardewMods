using AtraBase.Toolkit.Reflection;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

/// <summary>
/// Shims for DGA.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Preference.")]
public static class DynamicGameAssetsShims
{
    /// <summary>
    /// Gets whether or not something is a DGA giant crop.
    /// </summary>
    public static Func<object, bool>? IsDGAGiantCrop => isDGAGiantCrop.Value;

    private static Lazy<Func<object, bool>?> isDGAGiantCrop = new(() =>
    {
        var type = AccessTools.TypeByName("DynamicGameAssets.Game.CustomGiantCrop");
        return type?.GetTypeIs();
    });
}
