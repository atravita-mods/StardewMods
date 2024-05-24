﻿using AtraCore.Interfaces;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Removes the following mail.
/// </summary>
internal sealed class RemoveMail : IEventCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveMail"/> class.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="monitor">Monitor to use.</param>
    public RemoveMail(string name, IMonitor monitor)
    {
        this.Name = name;
        this.Monitor = monitor;
    }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public IMonitor Monitor { get; init; }

    /// <inheritdoc />
    public bool Validate(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        if (args.Length < 2)
        {
            error = "Event command requires at least one mail flag to remove!";
            return false;
        }

        error = null;
        return true;
    }

    /// <inheritdoc />
    public bool Apply(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        try
        {
            // We expect the average case is fewer than six values
            // Arrays are faster for this use case.
            ArraySegment<string> mailflags = new(args, 1, args.Length - 1);

            for (int i = Game1.player.mailReceived.Count - 1; i >= 0; i--)
            {
                if (mailflags.Contains(Game1.player.mailReceived[i]))
                {
                    this.Monitor.DebugOnlyLog($"Removing mail flag {Game1.player.mailReceived[i]}");
                    Game1.player.mailReceived.RemoveAt(i);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trying to remove mail", ex);
        }

        error = null;
        return true;
    }
}