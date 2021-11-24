using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace StopRugRemoval
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor;
        public static ITranslationHelper I18n;
        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;
            I18n = this.Helper.Translation;
            Harmony harmony = new(ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.canBeRemoved)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.PostfixCanBeRemoved))
                );
        }

        private static void PostfixCanBeRemoved(Furniture __instance, ref Farmer __0, ref bool __result)
        {
            try
            {
                if (!__result) { return; } //can't be removed already
                if (!__instance.furniture_type.Value.Equals(Furniture.rug)) { return; } //only want to deal with rugs
                GameLocation currentLocation = __0.currentLocation; //get location of farmer
                if (currentLocation == null) { return; }

                Rectangle bounds = __instance.boundingBox.Value;
                ModMonitor.Log($"Checking rug: {bounds.X / 64f}, {bounds.Y / 64f}, W/H {bounds.Width / 64f}/{bounds.Height / 64f}");

                for (int x = 0; x < bounds.Width / 64; x++)
                {
                    for (int y = 0; y < bounds.Height / 64; y++)
                    {
                        if (!currentLocation.isTileLocationTotallyClearAndPlaceable(x + bounds.X / 64, y + bounds.Y / 64))
                        {
                            Game1.showRedMessage(I18n.Get("rug-removal-message"));
                            __result = false;
                            return;
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Ran into issues with postfix for Furniture::CanBeRemoved for {__instance.Name}\n\n{ex}", LogLevel.Error);
            }
        }
    }
}
