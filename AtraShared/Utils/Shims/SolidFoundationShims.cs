using AtraBase.Toolkit.Reflection;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

/// <summary>
/// Shims for Solid Foundations.
/// </summary>
public static class SolidFoundationShims
{
    /// <summary>
    /// Gets whether or not the object is an SF building.
    /// </summary>
    public static Func<object, bool>? IsSFBuilding => isSFBuilding.Value;

    private static Lazy<Func<object, bool>?> isSFBuilding = new(() =>
    {
        Type sFBuilding = AccessTools.TypeByName("SolidFoundations.Framework.Models.ContentPack.GenericBuilding");
        return sFBuilding?.GetTypeIs();
    });

}
