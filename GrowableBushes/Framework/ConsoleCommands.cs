namespace GrowableBushes.Framework;

/// <summary>
/// Manages console commands for this mod.
/// </summary>
internal static class ConsoleCommands
{
    /// <summary>
    /// Registers these console commands with SMAPI.
    /// </summary>
    /// <param name="commandHelper">Command helper.</param>
    internal static void RegisterCommands(ICommandHelper commandHelper)
    {
        commandHelper.Add("av.gb.add_bush", "Adds a placeable bush to your inventory", AddBushToInventory);
    }

    private static void AddBushToInventory(string command, string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            ModEntry.ModMonitor.Log("Expected one or two arguments", LogLevel.Error);
            return;
        }

        if (args.Length != 2 || !int.TryParse(args[1], out int count))
        {
            count = 1;
        }

        ReadOnlySpan<char> name = args[0].AsSpan().Trim();

        if (name.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (BushSizes possibleBush in BushSizesExtensions.GetValues())
            {
                Game1.player.addItemToInventoryBool(new InventoryBush(possibleBush, count));
            }
            return;
        }

        BushSizes bushIndex;
        if (int.TryParse(name, out int id) && BushSizesExtensions.IsDefined((BushSizes)id))
        {
            bushIndex = (BushSizes)id;
        }
        else if (!BushSizesExtensions.TryParse(name, out bushIndex, ignoreCase: true))
        {
            ModEntry.ModMonitor.Log($"{name.ToString()} is not a valid bush.", LogLevel.Error);
            return;
        }

        Game1.player.addItemToInventoryBool(new InventoryBush(bushIndex, count));
    }
}
