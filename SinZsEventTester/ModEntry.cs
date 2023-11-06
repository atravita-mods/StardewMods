using System.Linq;

namespace SinZsEventTester;

using System.Runtime.CompilerServices;

using Newtonsoft.Json;

using StardewModdingAPI.Events;

using StardewValley.Menus;

/// <inheritdoc />
public sealed class ModEntry : Mod
{
    bool hooked = false;

    // keep track of the current events.
    private readonly Stack<(string location, string eventKey)> evts = new();
    private (string location, string eventKey)? current;

    // keep track of the dialogue responses given.
    private Node tree = new("base", new());
    private Node? workingNode;
    private string? currentEventId;

    // I keep on clicking the stupid dialogues twice. Agh. Don't allow that.
    private readonly ConditionalWeakTable<DialogueBox, object> _seen = new();

    private HashSet<(string location, string eventKey)> completed = new();

    private int IterationstoSkip = 0;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.ConsoleCommands.Add(
            "sinz.playevents",
            "Auto plays events in the current location. If arguments are given is treated as a specific, or all if location is 'ALL'",
            this.QueueEvents);
        helper.ConsoleCommands.Add(
            "sinz.empty_event_queue",
            "Clears the event queue.",
            (_, _) =>
            {
                this.current = null;
                this.evts.Clear();
            });
    }

    private void Hook()
    {
        if (this.hooked)
            return;

        this.hooked = true;
        this.Helper.Events.GameLoop.UpdateTicked += this.GameLoop_UpdateTicked;
    }

    private void UnHook()
    {
        this.hooked = false;
        this.Helper.Events.GameLoop.UpdateTicked -= this.GameLoop_UpdateTicked;
    }

    private void QueueEvents(string cmd, string[] args)
    {
        this.evts.Clear();

        Func<string, bool>? filter = null;
        if (!ArgUtility.TryGetOptionalRemainder(args, 0, out string arg) || arg is null)
        {
            filter = static (_) => true;
        }
        else if (arg.Equals("current", StringComparison.OrdinalIgnoreCase))
        {
            this.PushEvents(Game1.currentLocation, this.evts);
            this.Hook();
            return;
        }
        else if (arg.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filter = static (_) => true;
        }
        else if (arg.StartsWith('*'))
        {
            string endsWidth = arg[1..];
            filter = (a) => a.EndsWith(endsWidth);
        }
        else if (arg.EndsWith('*'))
        {
            string startsWidth = arg[..^1];
            filter = (a) => a.StartsWith(startsWidth);
        }

        if (filter is null)
        {
            if (Utility.fuzzyLocationSearch(arg) is GameLocation location)
            {
                this.PushEvents(location, this.evts);
            }
        }
        else
        {
            foreach (var location in Game1.locations)
            {
                if (filter(location.Name))
                {
                    this.PushEvents(location, this.evts);
                }
            }
        }

        this.Hook();
    }

    private void PushEvents(GameLocation location, Stack<(string location, string eventKey)> evts)
    {
        if (!location.TryGetLocationEvents(out _, out var events))
        {
            this.Monitor.Log($"{location.Name} appears to lack events, skipping.");
            return;
        }

        this.Monitor.Log($"Location {location.Name} has {events.Count} events", LogLevel.Info);

        foreach (string key in events.Keys)
        {
            if (!int.TryParse(key, out var _) && key.IndexOf('/') == -1)
            {
                this.Monitor.Log($"{key} is likely a fork, skipping...");
                continue;
            }
            foreach (var segment in key.Split('/'))
            {
                if (segment.StartsWith("x "))
                {
                    this.Monitor.Log($"{key} contains an x precondition, skipping as it is an unnatural event.");
                    goto Outer;
                }
            }
            evts.Push((location.Name, key));
Outer: ;
        }
    }

    private void GameLoop_UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) return;

        // Run 3 times a second for speeed
        if (!e.IsMultipleOf(20)) return;

        if (this.IterationstoSkip-- > 0) return;

        if (Game1.CurrentEvent is not null || Game1.eventUp)
        {
            // if (!Game1.game1.IsActive) return;
            if (Game1.CurrentEvent is null) return;

            if (Game1.activeClickableMenu is DialogueBox db)
            {
                // speed up the boxen.
                db.finishTyping();
                db.safetyTimer = 0;

                if (db.isQuestion && db.selectedResponse == -1 && !this._seen.TryGetValue(db, out _))
                {
                    this._seen.AddOrUpdate(db, new());
                    string currentCommand = Game1.CurrentEvent.GetCurrentCommand();
                    this.Monitor.Log($"{currentCommand}");
                    this.Monitor.Log($"Asked a question with {db.responses.Length} options", LogLevel.Info);
                    this.Monitor.Log(JsonConvert.SerializeObject(db.responses), LogLevel.Trace);

                    if (Game1.CurrentEvent.id != this.currentEventId)
                    {
                        this.Monitor.Log($"Hey, {Game1.CurrentEvent.id} not an event I launched! Running it to completion.", LogLevel.Info);
                        db.selectedResponse = 0;

                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log("Clicking on the dialogue box");
                        db.receiveLeftClick(0, 0);
                        return;
                    }

                    this.IterationstoSkip = 1;

                    if (currentCommand.StartsWith("question null"))
                    {
                        this.Monitor.Log($"Meaningless choice, skipping.");
                        db.selectedResponse = 0;

                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log("Clicking on the dialogue box");
                        db.receiveLeftClick(0, 0);
                        return;
                    }

                    if (currentCommand.StartsWith("speak "))
                    {
                        var responseDialogues = db.characterDialogue?.speaker?.Dialogue;
                        bool isTrivial = true;
                        if (responseDialogues is not null)
                        {
                            foreach (var r in db.responses)
                            {
                                if (responseDialogues.TryGetValue(r.responseKey, out var data))
                                {
                                    this.Monitor.Log($"speak fork - response {r.responseKey} is {data}.");
                                    if (data.Contains("%fork"))
                                    {
                                        isTrivial = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isTrivial)
                        {
                            this.Monitor.Log($"Meaningless choice, skipping.");
                            db.selectedResponse = 0;

                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log("Clicking on the dialogue box");
                            db.receiveLeftClick(0, 0);
                            return;
                        }

                    }

                    // TODO: speak <talker> "$q", need to figure out how to detect this.
                    if (currentCommand.StartsWith("quickQuestion"))
                    {
                        var splits = currentCommand.Split("(break)");
                        bool mismatchFound = false;
                        for (int i = 2; i < splits.Length; i++)
                        {
                            if (splits[1] != splits[i])
                            {
                                mismatchFound = true;
                                break;
                            }
                        }

                        if (!mismatchFound)
                        {
                            this.Monitor.Log($"Meaningless choice, skipping.");
                            db.selectedResponse = 0;

                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log("Clicking on the dialogue box");
                            db.receiveLeftClick(0, 0);
                            return;
                        }
                    }

                    switch (this.workingNode!.Color)
                    {
                        case Color.White:
                        {
                            this.workingNode.Children.AddRange(db.responses.Select(static response => new Node(response.responseKey, new())));
                            this.Monitor.Log($"First visit, selecting choice 0. {db.responses[0].responseText}", LogLevel.Debug);
                            db.selectedResponse = 0;
                            this.workingNode.Color = Color.Grey;
                            this.workingNode = this.workingNode.Children.First();
                            break;
                        }
                        case Color.Grey:
                        {
                            for (int i = 0; i < this.workingNode.Children.Count; i++)
                            {
                                Node? child = this.workingNode.Children[i];
                                switch (child.Color)
                                {
                                    case Color.Black:
                                        continue;
                                    case Color.Grey:
                                    {
                                        if (child.ChildrenFinished())
                                        {
                                            child.Color = Color.Black;
                                            continue;
                                        }
                                        break;
                                    }
                                }

                                this.Monitor.Log($"Now selecting response {i}. {db.responses[i].responseText}", LogLevel.Debug);
                                db.selectedResponse = i;
                                this.workingNode = child;
                                break;
                            }
                            break;
                        }
                        case Color.Black:
                        {
                            Game1.CurrentEvent.endBehaviors();
                            this.Monitor.Log($"How did I get here?", LogLevel.Warn);
                            this.current = null;
                            break;
                        }
                    }
                }

                if (this.Monitor.IsVerbose)
                    this.Monitor.Log("Clicking on the dialogue box");
                db.receiveLeftClick(0, 0);
            }
            else if (Game1.activeClickableMenu is NamingMenu nm)
            {
                // Hope doing this at 3tps isn't a problem
                nm.receiveLeftClick(nm.doneNamingButton.bounds.Center.X, nm.doneNamingButton.bounds.Center.Y);
            }
            return;
        }

        if (!Game1.game1.IsActive) return;

        // event ended, mark last node as black.
        if (this.workingNode is not null)
        {
            this.workingNode.Color = Color.Black;
            this.workingNode = null;
        }

        // re-launch the SAME event with the next set of choices.
        if (this.current is not null)
        {
            foreach (var node in this.tree.Children)
            {
                switch (node.Color)
                {
                    case Color.Black:
                        continue;
                    case Color.Grey:
                    {
                        if (node.ChildrenFinished())
                        {
                            node.Color = Color.Black;
                            continue;
                        }
                        break;
                    }
                }

                this.workingNode = this.tree;
                this.LaunchEvent(this.current.Value);
                return;
            }
        }

        if (this.current is not null)
        {
            this.completed.Add(this.current.Value);
        }

        this.current = null;
        this.tree = new("base", new());
        this.workingNode = this.tree;

        if (this.evts.TryPop(out var pair))
        {
            this.LaunchEvent(pair);
            return;
        }

        this.Monitor.Log("Done, unhooking.");
        this.UnHook();
        this.Helper.Data.WriteGlobalData("finished-events", this.completed);
    }

    private void LaunchEvent((string location, string eventKey) pair)
    {
        // Burn the players inventory every event to make sure space exists
        for (int i = Game1.player.Items.Count - 1; i >= 0; i--)
        {
            Game1.player.Items[i] = null;
        }

        // copied out of Game1.PlayEvent
        if (Game1.getLocationFromName(pair.location) is not GameLocation actual)
        {
            return;
        }

        if (!actual.TryGetLocationEvents(out var assetName, out var evtDict))
        {
            this.Monitor.Log($"Evts file for {actual.Name} now missing, what.", LogLevel.Warn);
            return;
        }

        var id = pair.eventKey.GetNthChunk('/').ToString();
        this.current = pair;
        this.currentEventId = id;

        this.Monitor.Log($"Playing {id}, {this.evts.Count} events remaining.");
        this.IterationstoSkip = 8;

        if (pair.location != Game1.currentLocation.Name)
        {
            LocationRequest request = Game1.getLocationRequest(pair.location);
            request.OnLoad += () =>
            {
                Game1.currentLocation.currentEvent = new Event(evtDict[pair.eventKey], assetName, id);
                this.current = pair;
                this.currentEventId = id;
            };
            int x = 8;
            int y = 8;
            Utility.getDefaultWarpLocation(request.Name, ref x, ref y);
            Game1.warpFarmer(request, x, y, Game1.player.FacingDirection);
        }
        else
        {
            Game1.globalFadeToBlack(() =>
            {
                Game1.forceSnapOnNextViewportUpdate = true;
                Game1.currentLocation.startEvent(new Event(evtDict[pair.eventKey], assetName, id));
                Game1.globalFadeToClear();
            });
        }
    }

    private record Node(string responseKey, List<Node> Children)
    {
        internal Color Color { get; set; } = Color.White;

        internal bool ChildrenFinished()
        {
            switch (this.Color)
            {
                case Color.Black:
                    return true;
                case Color.White:
                    return false;
            }

            foreach (Node child in this.Children)
            {
                switch (child.Color)
                {
                    case Color.White:
                        return false;
                    case Color.Black:
                        continue;
                    case Color.Grey:
                        if (child.ChildrenFinished())
                        {
                            child.Color = Color.Black;
                            continue;
                        }
                        return false;
                }
            }

            return true;
        }
    }

    private enum Color
    {
        White,
        Grey,
        Black,
    }
}

file static class Extensions
{
    /// <summary>
    /// Faster replacement for str.Split()[index];.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <param name="deliminator">deliminator to use.</param>
    /// <param name="index">index of the chunk to get.</param>
    /// <returns>a readonlyspan char with the chunk, or an empty readonlyspan for failure.</returns>
    public static ReadOnlySpan<char> GetNthChunk(this string str, char deliminator, int index = 0)
    {
        int start = 0;
        int ind = 0;
        while (index-- >= 0)
        {
            ind = str.IndexOf(deliminator, start);
            if (ind == -1)
            {
                // since we've previously decremented index, check against -1;
                // this means we're done.
                if (index == -1)
                {
                    return str.AsSpan()[start..];
                }

                // else, we've run out of entries
                // and return an empty span to mark as failure.
                return ReadOnlySpan<char>.Empty;
            }

            if (index > -1)
            {
                start = ind + 1;
            }
        }
        return str.AsSpan()[start..ind];
    }
}