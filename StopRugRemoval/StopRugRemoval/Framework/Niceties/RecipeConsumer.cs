using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace StopRugRemoval.Framework.Niceties;

/// <summary>
/// Lets you consume a recipe object to teach yourself the recipe.
/// </summary>
[HarmonyPatch(typeof(SObject))]
internal static class RecipeConsumer
{
    [HarmonyPatch( nameof(SObject.performUseAction))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static void Postfix(SObject __instance, ref bool __result)
    {
        if (!__result && __instance.IsRecipe)
        {
            __result = __instance.ConsumeRecipeImpl();
        }
    }
}
