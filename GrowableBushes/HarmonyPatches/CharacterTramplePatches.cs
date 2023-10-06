using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using GrowableBushes.Framework.Items;
using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace GrowableBushes.HarmonyPatches;

/// <summary>
/// Holds patches that lets NPCs trample bushes.
/// </summary>
[HarmonyPatch(typeof(Character))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class CharacterTramplePatches
{
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(Character.MovePosition))]
    private static void Prefix(Character __instance, GameLocation currentLocation)
    {
        if (!ModEntry.Config.ShouldNPCsTrampleBushes || __instance is not NPC npc || !npc.isVillager()
            || currentLocation?.largeTerrainFeatures?.Count is 0 or null)
        {
            return;
        }

        try
        {
            Rectangle nextPosition = npc.nextPosition(npc.FacingDirection);

            for (int i = currentLocation.largeTerrainFeatures.Count - 1; i >= 0; i--)
            {
                LargeTerrainFeature feature = currentLocation.largeTerrainFeatures[i];
                if (feature is Bush bush && bush.getBoundingBox().Contains(nextPosition)
                    && bush.modData.ContainsKey(InventoryBush.BushModData))
                {
                    bush.health = -1f;
                    Axe axe = new() { UpgradeLevel = 3 };
                    bush.performToolAction(axe, 0, bush.Tile);
                    currentLocation.largeTerrainFeatures.RemoveAt(i);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trampling a bush", ex);
        }
    }
}
