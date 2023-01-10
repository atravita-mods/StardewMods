using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SpecialOrdersExtended.Managers;

using StardewModdingAPI.Events;

using StardewValley.Menus;

namespace SpecialOrdersExtended.HarmonyPatches;

[HarmonyPatch(typeof(SpecialOrdersBoard))]
internal static class CustomEmoji
{
    private static readonly HashSet<IAssetName> Failed = new(); // hashset of failed loads.
    private static readonly Dictionary<string, KeyValuePair<Texture2D, Rectangle>> Cache = new();

    private static IGameContentHelper parser = null!;

    internal static void Init(IGameContentHelper gameContentHelper) => parser = gameContentHelper;

    /// <summary>
    /// Handles invalidations.
    /// </summary>
    /// <param name="assets">IReadOnly set of assetnames.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(AssetManager.EmojiOverride))
        {
            Cache.Clear();
        }
        if (assets is not null && Failed.Count > 0)
        {
            Failed.RemoveWhere(assets.Contains);
        }
    }

    /// <summary>
    /// Removes paths from the failed texture load cache if someone readies them.
    /// </summary>
    /// <param name="e">Asset event args.</param>
    internal static void Ready(AssetReadyEventArgs e)
    {
        Failed.Remove(e.Name);
        Failed.Remove(e.NameWithoutLocale);
    }

    [UsedImplicitly]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch("GetPortraitForRequester")]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for harmony.")]
    private static void Postfix(ref KeyValuePair<Texture2D, Rectangle>? __result, string requester_name)
    {
        if (requester_name is null)
        {
            return;
        }

        __result ??= GetEntry(requester_name);
    }

    [MethodImpl(TKConstants.Hot)]
    private static KeyValuePair<Texture2D, Rectangle>? GetEntry(string requesterName)
    {
        if (Cache.TryGetValue(requesterName, out KeyValuePair<Texture2D, Rectangle> entry))
        {
            if (!entry.Key.IsDisposed)
            {
                return entry;
            }
            else
            {
                Cache.Remove(requesterName);
            }
        }

        Dictionary<string, EmojiData>? asset = Game1.temporaryContent.Load<Dictionary<string, EmojiData>>(AssetManager.EmojiOverride.BaseName);

        if (asset.TryGetValue(requesterName, out EmojiData? data))
        {
            IAssetName texLoc = parser.ParseAssetName(data.AssetName);
            if (Failed.Contains(texLoc))
            {
                return null;
            }
            try
            {
                Texture2D? tex = Game1.content.Load<Texture2D>(texLoc.BaseName);
                Rectangle loc = new(data.Location, new(9, 9));
                if (tex.Bounds.Contains(loc))
                {
                    KeyValuePair<Texture2D, Rectangle> kvp = new(tex, loc);
                    Cache[requesterName] = kvp;
                    return kvp;
                }
                else
                {
                    ModEntry.ModMonitor.Log($"{data} appears to be requesting an out of bounds rectangle.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Failed.Add(texLoc);
                ModEntry.ModMonitor.LogOnce($"Failed to load {data.AssetName}.", LogLevel.Error);
                ModEntry.ModMonitor.Log(ex.ToString());
            }
        }

        return null;
    }
}
