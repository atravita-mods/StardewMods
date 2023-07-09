using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Microsoft.Xna.Framework;

using Netcode;

using StardewValley.Locations;

namespace NovaNPCTest.HarmonyPatches;

[HarmonyPatch(typeof(Utility))]
internal static class TrashCanPatches
{
    [HarmonyPatch(nameof(Utility.isThereAFarmerOrCharacterWithinDistance))]
    private static void Postfix(ref Character? __result, Vector2 tileLocation, int tilesAway, GameLocation environment)
    {
        if (__result is not NPC npc || environment is not Town || (npc.Name != "Nova.Eli" && npc.Name != "Nova.Dylan" ))
        {
            return;
        }

        if (environment.doesTileHaveProperty((int)tileLocation.X, (int)tileLocation.Y, "Action", "Buildings") is string s
            && int.TryParse(s.GetNthChunk(' ', 1), out var whichCan))
        {
            NetArray<bool, NetBool> garbageChecked = ModEntry.ReflectionHelper.GetField<NetArray<bool, NetBool>>((object)environment, "garbageChecked", true).GetValue();
            if (whichCan >= 0 && whichCan < garbageChecked.Length)
            {
                string response = (npc.Name == "Nova.Eli") ? I18n.Eli_TrashReaction() : I18n.Dylan_TrashReaction();
                npc.doEmote(32);
                npc.setNewDialogue(response, add: true, clearOnMovement: true);
                Game1.player.changeFriendship(1, npc);
                Game1.drawDialogue(npc);
                __result = null;
            }
        }
    }
}
