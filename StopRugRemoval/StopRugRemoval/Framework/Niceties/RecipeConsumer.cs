using AtraShared.Utils.Extensions;
using HarmonyLib;

using StardewModdingAPI.Events;

namespace StopRugRemoval.Framework.Niceties;

/// <summary>
/// Lets you consume a recipe object to teach yourself the recipe.
/// </summary>
internal static class RecipeConsumer
{
    internal static bool ConsumeRecipeIfNeeded(ButtonPressedEventArgs e, IInputHelper helper)
    {
        if (e.Button.IsActionButton() && Game1.player.ActiveObject?.IsRecipe == true
            && Game1.player.ActiveObject.ConsumeRecipeImpl())
        {
            Game1.playSound("newRecipe");
            Game1.player.ActiveObject = null;
            helper.Suppress(e.Button);
            return true;
        }
        return false;
    }
}
