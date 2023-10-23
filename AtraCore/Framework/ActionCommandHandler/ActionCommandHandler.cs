using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using xTile.Dimensions;

namespace AtraCore.Framework.ActionCommandHandler;

/// <summary>
/// Manages action commands.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
public static class ActionCommandHandler
{
    /// <summary>
    /// The signature of an action command.
    /// </summary>
    /// <param name="parameters">the name of the action.</param>
    /// <param name="who">The farmer who triggered the action.</param>
    /// <param name="tileLocation">The tile location of the click.</param>
    /// <returns>True if handled, false otherwise.</returns>
    public delegate bool ActionCommand(GameLocation loc, ArraySegment<string> parameters, Farmer who, Location tileLocation);

    private static readonly Dictionary<string, ActionCommand> _actions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a new action command.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="command">The delegate to call.</param>
    /// <returns>True if handled, false otherwise.</returns>
    public static bool RegisterActionCommand(string name, ActionCommand command)
        => _actions.TryAdd(name, command);

    [HarmonyPatch(nameof(GameLocation.performAction))]
    private static void Postfix(GameLocation __instance, string action, Farmer who, Location tileLocation, bool __result)
    {
        if (__result)
        {
            return;
        }

        string[] splits = action.Split(' ');
        if (splits.Length > 0 && _actions.TryGetValue(splits[0], out ActionCommand? actionCommand))
        {
            try
            {
                __result = actionCommand.Invoke(
                    __instance,
                    new ArraySegment<string>(splits, 1, splits.Length - 1),
                    who,
                    tileLocation);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"handling action {action}", ex);
                _actions.Remove(splits[0]);
            }
        }
    }
}
