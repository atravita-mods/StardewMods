
using HarmonyLib;

using MiniAtraShared.Extensions;

using SlightlyMoreDehardcoding.Framework;
using SlightlyMoreDehardcoding.Models;

namespace SlightlyMoreDehardcoding.HarmonyPatches;

internal static class NPCPatches
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Utility), nameof(Utility.GetMaximumHeartsForCharacter)),
            postfix: new(typeof(NPCPatches), nameof(Postfix)));
    }

    private static void Postfix(Character character, ref int __result)
    {
        if (character is not NPC npc)
        {
            return;
        }

        try
        {
            HeartsOverride? data = AssetManager.GetExtendedFriendshipData(npc.Name)?.HeartsOverride;
            if (data is null)
            {
                return;
            }

            if (Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship))
            {
                if (friendship.IsMarried())
                {
                    if (data.Married != -1)
                    {
                        __result = data.Married;
                    }
                    return;
                }
                else if (friendship.IsDivorced())
                {
                    if (data.Divorced != -1)
                    {
                        __result = data.Divorced;
                    }
                    return;
                }
                else if (friendship.IsDating())
                {
                    if (data.Dating != -1)
                    {
                        __result = data.Dating;
                    }
                    return;
                }
            }

            if (npc.datable.Value)
            {
                if (data.Datable != -1)
                {
                    __result = data.Datable;
                }
            }
            else
            {
                if (data.NonDatable != -1)
                {
                    __result = data.NonDatable;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding NPC hearts", ex);
        }
    }
}