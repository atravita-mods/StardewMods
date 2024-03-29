﻿using AtraBase.Toolkit.Reflection;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

/// <summary>
/// Shims for Solid Foundations.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Preference.")]
public static class SolidFoundationShims
{
    /// <summary>
    /// Gets whether or not the object is an SF building.
    /// </summary>
    public static Func<object, bool>? IsSFBuilding => isSFBuilding.Value;

    private static readonly Lazy<Func<object, bool>?> isSFBuilding = new(() =>
    {
        return AccessTools.TypeByName("SolidFoundations.Framework.Models.ContentPack.GenericBuilding")?.GetTypeIs();
    });
}
