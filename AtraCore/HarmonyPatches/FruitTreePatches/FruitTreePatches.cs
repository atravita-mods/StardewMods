using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.Caches;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Extensions;
using StardewValley.TerrainFeatures;

namespace AtraCore.HarmonyPatches.FruitTreePatches;

[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreePatches
{
    #region delegates
    private static readonly Lazy<Action<FruitTree, float>> _maxShake = new(static () =>
        typeof(FruitTree).GetCachedField("maxShake", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<FruitTree, float>());
    #endregion

    [HarmonyPrefix]
    [HarmonyPatch(nameof(FruitTree.shake))]
    private static bool PrefixShake(FruitTree __instance)
    {
        int x = (int)__instance.Tile.X;
        int y = (int)__instance.Tile.Y;
        if (__instance.Location?.doesTileHaveProperty(x, y, "atravita.FruitTreeShake", "Back") is string message)
        {
            ShowMessage(message);
            ShakeTree(__instance);
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(FruitTree.performToolAction))]
    private static bool PrefixCut(FruitTree __instance)
    {
        int x = (int)__instance.Tile.X;
        int y = (int)__instance.Tile.Y;
        if (__instance.Location?.doesTileHaveProperty(x, y, "atravita.FruitTreeCut", "Back") is string message)
        {
            ShowMessage(message);
            ShakeTree(__instance);
            return false;
        }

        return true;
    }

    private static void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Game1.showRedMessage(I18n.FruitTree_Forbidden());
        }
        else if (message.TrySplitOnce(':', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
        {
            string name = first.Trim().ToString();
            NPC? npc = NPCCache.GetByVillagerName(name);
            if (npc is null)
            {
                try
                {
                    npc = new NPC(
                        sprite: null,
                        position: Vector2.Zero,
                        defaultMap: string.Empty,
                        facingDirection: 0,
                        name,
                        datable: false,
                        portrait: Game1.temporaryContent.Load<Texture2D>("Portraits\\" + name));
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.LogError($"creating NPC {name}", ex);
                    return;
                }
            }
            Game1.DrawDialogue(new(npc, null, second.Trim().ToString()));
        }
        else
        {
            Game1.drawObjectDialogue(message.Trim());
        }
    }

    private static void ShakeTree(FruitTree tree)
    {
        Farmer player = Game1.player;

        tree.shakeLeft.Value = player.StandingPixel.X > (tree.Tile.X + 0.5f) * 64f || (player.Tile.X == tree.Tile.X && Game1.random.NextBool());
        _maxShake.Value(tree, tree.growthStage.Value >= 4 ? MathF.PI / 128.0f : MathF.PI / 64f);
    }
}
