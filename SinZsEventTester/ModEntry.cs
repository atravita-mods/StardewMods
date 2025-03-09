namespace SinZsEventTester;

using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Xna.Framework.Input;

using Newtonsoft.Json;

using SinZsEventTester.Framework;
using SinZsEventTester.Framework.BulkRemoveCommand;

using StardewModdingAPI.Events;

using StardewValley.BellsAndWhistles;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Logging;
using StardewValley.Menus;
using StardewValley.Minigames;

using MiniAtraShared.Models;
using SinZsEventTester.Framework.EventTester;

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Preference.")]
public sealed class ModEntry : BaseMod<ModEntry>
{
    private sealed record class IntHolder(int Value);

    private bool hooked = false;

    internal static ModConfig Config { get; private set; } = null!;

    // keep track of the current events.
    private readonly Stack<EventRecord> evts = new();
    private EventRecord? current;

    // keep track of the dialogue responses given.
    private readonly Node tree = new("base", 0, []);
    private Node? workingNode;
    private string? currentEventId;
    private readonly HashSet<string> seenResponses = [];

    // I keep on clicking the stupid dialogues twice. Agh. Don't allow that.
    private readonly ConditionalWeakTable<DialogueBox, IntHolder> _seen = [];

    private HashSet<EventRecord> completed = [];

    private int iterationstoSkip = 0;

    private FastForwardHandler? fastForwardHandler;
    private MonitorPerformance? performanceMonitor;
    private IChecker? currentChecker;
    private bool _prevPauseWhenUnfocused = true;
    private bool _prevDialogueTyping = true;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);
        try
        {
            Config = this.Helper.ReadConfig<ModConfig>();
            if (Config.AllowCheats)
            {
                Program.enableCheats = true;
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to deserialize config, see errors: {ex}.", LogLevel.Error);
            Config = new();
        }

        DialogueChecker.Init(this.Helper.Reflection);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.SaveCreated += this.OnSaveCreated;

        helper.ConsoleCommands.Add(
            "sinz.playevents",
            "Auto plays events in the current location. If arguments are given is treated as a specific, or all if location is 'ALL'",
            this.QueueLocations);
        helper.ConsoleCommands.Add(
            "sinz.eventbyid",
            "Plays a specific event id at speed, including all branches.",
            this.EventById);
        helper.ConsoleCommands.Add(
            "sinz.empty_event_queue",
            "Clears the event queue.",
            (_, _) =>
            {
                this.current = null;
                this.evts.Clear();
            });
        helper.ConsoleCommands.Add(
            "sinz.phone_call",
            "calls the phone",
            (_, args) => new SimpleConsoleCommand(this.Monitor).RingPhone(args.FirstOrDefault())
            );
        helper.ConsoleCommands.Add(
            "sinz.check_preconditions",
            "checks over all preconditions",
            (_, _) => new SimpleConsoleCommand(this.Monitor).CheckPreconditions());
        helper.ConsoleCommands.Add(
            "sinz.check_gsq",
            "Checks over the game's GSQ",
            (_, args) =>
            {
                GSQTester checker = new(this.Monitor, this.Helper.Reflection, this.Helper.GameContent);
                if (args.Length == 0)
                {
                    checker.Check(Game1.content);
                }
                else
                {
                    foreach (string arg in args)
                    {
                        checker.Check(Game1.content, arg);
                    }
                }
            });
        helper.ConsoleCommands.Add(
            "sinz.check_special_orders",
            "Checks special orders",
            (_, args) => new CheckSpecialOrdersCommand(this.Monitor).GetAvailableOrders()
            );
        helper.ConsoleCommands.Add(
            "sinz.forget_event",
            "Forgets events",
            (_, args) => new SimpleConsoleCommand(this.Monitor).ForgetEvents(args));
        helper.ConsoleCommands.Add(
            "sinz.forget_mail",
            "Forgets mail",
            (_, args) => new SimpleConsoleCommand(this.Monitor).ForgetMail(args));
        helper.ConsoleCommands.Add(
            "sinz.forget_triggers",
            "Forgets triggers",
            (_, args) => new SimpleConsoleCommand(this.Monitor).ForgetTriggers(args));
        helper.ConsoleCommands.Add(
            "sinz.get_music",
            "Gets the track currently playing, if any.",
            (_, _) => new SimpleConsoleCommand(this.Monitor).GetTrack());

        helper.ConsoleCommands.Add(
            "sinz.randomize_friendships",
            "Randomizes the friendship points for everyone in the game.",
            (_, _) => new SimpleConsoleCommand(this.Monitor).RandomizeFriendships());
        helper.ConsoleCommands.Add(
            "sinz.fast_forward",
            "Fasts forward the game",
            (command, args) => this.FastForward(command, args.AsSpan())
            );

        helper.ConsoleCommands.Add(
            "sinz.gc",
            "Checks the amount of memory used.",
            callback: (command, args) => this.GarbageCollect(command, args.AsSpan()));

        helper.ConsoleCommands.Add(
            "sinz.monitor_performance",
            "Adds performance monitoring",
            (command, args) => this.MonitorPerformance(command, args.AsSpan()));

        helper.ConsoleCommands.Add(
            "sinz.check_dialogue",
            "Checks dialogue",
            (command, args) => this.CheckDialogue(command, args.AsSpan()));
        helper.ConsoleCommands.Add(
            "sinz.check_mail",
            "Checks mail",
            (command, args) => this.CheckMail(command, args.AsSpan()));
        helper.ConsoleCommands.Add(
            "sinz.hibernate",
            "Sleeps for X days",
            (command, args) => new HiberationManager(this.Helper.Events.GameLoop, this.Helper.Reflection, int.Parse(args[0])));

        helper.ConsoleCommands.Add(
            "sinz.bulkadd",
            "bulk adds recipes",
            (command, args) => new BulkAddRemoveCommand(this.Monitor).Add(args));

        helper.ConsoleCommands.Add(
            "sinz.bulkremove",
            "bulk remove data",
            (command, args) => new BulkAddRemoveCommand(this.Monitor).Remove(args));
    }

    private void OnSaveCreated(object? sender, SaveCreatedEventArgs e) => this.RunMacros("save_created_commands.txt");
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => this.RunMacros("startup_commands.txt");

    private void RunMacros(string file)
    {
        var dir = new DirectoryInfo(this.Helper.DirectoryPath);
        var fileinfo = dir.GetFiles().Where(a => a.Name.EqualsIgnoreCase(file)).FirstOrDefault();
        if (fileinfo is null)
        {
            this.Monitor.Log($"Could not find file by name {file}");
            return;
        }

        this.Monitor.Log($"Running {fileinfo.FullName}", LogLevel.Debug);
        new RunStartupFile(this.Monitor, this.Helper.Events.GameLoop, fileinfo);
    }

    private void CheckDialogue(string command, Span<string> args, IGameLogger? logger = null)
    {
        if (!Context.IsWorldReady)
        {
            LogHelpers.Warn(logger, "Please load a world first!");
            return;
        }

        this.currentChecker?.Dispose();
        this.currentChecker = new DialogueChecker(this.Monitor, this.Helper.Events.GameLoop, args);
    }

    private void CheckMail(string command, Span<string> args, IGameLogger? logger = null)
    {
        if (!Context.IsWorldReady)
        {
            LogHelpers.Warn(logger, "Please load a world first!");
            return;
        }

        this.currentChecker?.Dispose();
        this.currentChecker = new MailChecker(this.Monitor, this.Helper.Events.GameLoop, args);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Game1.keyboardDispatcher.Subscriber is null && Config.FastForwardKeybind.Keybinds.Where(k => k.GetState() == SButtonState.Pressed).FirstOrDefault() is { } keybind)
        {
            this.ToggleFastForward();
            foreach (SButton button in keybind.Buttons)
            {
                this.Helper.Input.Suppress(button);
            }
        }
    }

    #region fast forward handlers

    private void FastForward(string command, Span<string> args, IGameLogger? logger = null)
    {
        if (args.Length == 0)
        {
            this.ToggleFastForward();
            return;
        }
        if (!int.TryParse(args[0], out int multi))
        {
            LogHelpers.Log(logger, $"Could not parse {args[0]} as valid int");
            return;
        }

        if (multi < 2)
        {
            this.DisableFastForward();
        }
        else
        {
            this.EnableFastForward(multi);
        }
    }

    private void ToggleFastForward()
    {
        if (this.fastForwardHandler is not { } handler || handler.IsDisposed)
        {
            this.EnableFastForward(Config.FastForwardRatio);
        }
        else
        {
            this.DisableFastForward();
        }
    }

    private void EnableFastForward(int ratio)
    {
        this.fastForwardHandler = new(this.Monitor, this.Helper.Events.GameLoop, this.Helper.Reflection, ratio);
        Game1.addHUDMessage(new("FastFoward enabled!", HUDMessage.achievement_type));
    }

    private void DisableFastForward()
    {
        if (this.fastForwardHandler is { } handler)
        {
            handler.Dispose();
            this.fastForwardHandler = null;
            Game1.addHUDMessage(new("FastForward disabled!", HUDMessage.achievement_type));
        }
    }

    #endregion

    /// <inheritdoc />
    public override object? GetApi(IModInfo mod) => new Api(mod, this.Monitor);

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu") is IGenericModConfigMenuApi api)
        {
            api.Register(
                mod: this.ModManifest,
                reset: static () => Config = new(),
                save: () =>
                {
                    Task.Run(() => this.Helper.WriteConfig(Config)).ContinueWith((task) => {
                        if (task.IsCompletedSuccessfully)
                        {
                            this.Monitor.Log("Config saved!");
                        }
                        else
                        {
                            this.Monitor.Log("Config failed to save.", LogLevel.Error);
                            if (task.Exception is { } ex)
                            {
                                this.Monitor.Log(ex.ToString());
                            }
                        }
                    });
                    Program.enableCheats = Config.AllowCheats;
                });
            api.AddNumberOption(
                mod: this.ModManifest,
                getValue: static () => Config.EventSpeedRatio,
                setValue: static value => Config.EventSpeedRatio = value,
                I18n.EventSpeedRatio);
            api.AddNumberOption(
                mod: this.ModManifest,
                getValue: static () => Config.FastForwardRatio,
                setValue: static value => Config.FastForwardRatio = value,
                I18n.FastForwardRatio);
            api.AddKeybindList(
                mod: this.ModManifest,
                getValue: static () => Config.FastForwardKeybind,
                setValue: static value => Config.FastForwardKeybind = value,
                I18n.FastForwardKeybind);
            api.AddBoolOption(
                mod: this.ModManifest,
                getValue: static () => Config.AllowCheats,
                setValue: static value => Config.AllowCheats = value,
                I18n.AllowCheats);
            api.AddBoolOption(
                mod: this.ModManifest,
                getValue: static () => Config.SkipDialogue,
                setValue: static value => Config.SkipDialogue = value,
                I18n.SkipDialogue);
        }

        Dictionary<string, DebugCommandHandlerDelegate> handlers = this.Helper.Reflection.GetField<Dictionary<string, DebugCommandHandlerDelegate>>(typeof(DebugCommands), "Handlers").GetValue();
        handlers.TryAdd("smapicommand", (args, logger) =>
        {
            string command;
            if (args.Length == 2)
            {
                command = args[1];
            }
            else
            {
                StringBuilder builder = new();
                foreach (string? arg in args.AsSpan(1))
                {
                    if (arg.Contains(' '))
                    {
                        builder.Append('"').Append(arg).Append('"');
                    }
                    else
                    {
                        builder.Append(arg);
                    }
                    builder.Append(' ');
                }
                if (builder.Length > 0)
                {
                    builder.Remove(builder.Length - 1, 1);
                }

                command = builder.ToString();
            }

            logger.Debug($"Queuing {command}");

            SMAPICommandQueuer.QueueConsoleCommand(command);
        });

        handlers.TryAdd("fastforward", (args, logger) =>
        {
            this.FastForward(args[0], args.AsSpan(1), logger);
        });

        handlers.TryAdd("gc", (args, logger) =>
        {
            this.GarbageCollect(args[0], args.AsSpan(1), logger);
        });
    }

    private void GarbageCollect(string command, Span<string> args, IGameLogger? logger = null)
    {
        LogHelpers.Log(logger, $"Current memory usage {GC.GetTotalMemory(false):N0} bytes.");
        if (args.Length > 0 && bool.TryParse(args[0], out bool v) && v)
        {
            GC.Collect();
            LogHelpers.Log(logger, $"Post-collection memory usage is {GC.GetTotalMemory(true):N0} bytes.");
        }
    }

    private void MonitorPerformance(string command, Span<string> args, IGameLogger? logger = null)
    {
        if (this.performanceMonitor is null || this.performanceMonitor.IsDisposed)
        {
            this.performanceMonitor = new(this.Helper.Events.GameLoop, this.Helper.Events.Display);
        }
        else
        {
            this.performanceMonitor.Dispose();
            this.performanceMonitor = null;
        }
    }

    private void Hook()
    {
        if (this.hooked)
        {
            return;
        }

        this.hooked = true;
        this.Helper.Events.GameLoop.UpdateTicked += this.GameLoop_UpdateTicked;

        if (Game1.game1?.instanceOptions is { } options)
        {
            this._prevPauseWhenUnfocused = options.pauseWhenOutOfFocus;
            this._prevDialogueTyping = options.dialogueTyping;

            options.pauseWhenOutOfFocus = false;
            options.dialogueTyping = false;
        }
    }

    private void UnHook()
    {
        this.hooked = false;
        this.Helper.Events.GameLoop.UpdateTicked -= this.GameLoop_UpdateTicked;

        if (Game1.game1?.instanceOptions is { } options)
        {
            options.pauseWhenOutOfFocus = this._prevPauseWhenUnfocused;
            options.dialogueTyping = this._prevDialogueTyping;
        }
    }

    #region queues events.

    private void EventById(string cmd, string[] args)
    {
        foreach (string candidate in args)
        {
            int count = 0;

            ReadOnlySpan<char> cand = candidate.AsSpan().Trim();
            Func<string, bool> filter;
            if (cand.Contains('/'))
            {
                var c = cand.ToString();
                filter = (key) => key.Equals(c, StringComparison.OrdinalIgnoreCase);
            }
            else if (candidate == "*")
            {
                filter = (key) => true;
            }
            else if (candidate.EndsWith('*'))
            {
                string startsWith = cand[..^1].ToString();
                filter = (key) => key.GetNthChunk('/').StartsWith(startsWith, StringComparison.OrdinalIgnoreCase);
            }
            else if (candidate.StartsWith('*'))
            {
                string endsWith = cand[1..].ToString();
                filter = (key) => key.GetNthChunk('/').StartsWith(endsWith, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var c = cand.ToString();
                filter = (key) => key.GetNthChunk('/').Equals(c, StringComparison.OrdinalIgnoreCase);
            }

            foreach (GameLocation? location in Game1.locations)
            {
                if (!location.TryGetLocationEvents(out _, out Dictionary<string, string>? events) || events.Count == 0)
                {
                    continue;
                }

                foreach (string? key in events.Keys)
                {

                    if (!int.TryParse(key, out _) && !key.Contains('/'))
                    {
                        // an event to switch to.
                        continue;
                    }
                    if (filter(key))
                    {
                        EventRecord record = new (location.Name, key);
                        this.completed.Remove(record);
                        this.evts.Push(record);

                        count++;
                    }
                }
            }

            this.Monitor.Log($"Adding {count} events for candidate {cand}");
        }

        if (this.evts.Count > 0)
        {
            this.Hook();
        }
    }

    private void QueueLocations(string cmd, string[] args)
    {
        Func<string, bool>? filter = null;
        if (!ArgUtility.TryGetOptionalRemainder(args, 0, out string arg) || arg is null
            || arg.Equals("current", StringComparison.OrdinalIgnoreCase))
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
        else if (arg.TrySplitOnce('*', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
        {
            string firstS = first.ToString();
            string secondS = second.ToString();
            filter = (a) => a.StartsWith(firstS) && a.EndsWith(secondS);
        }

        if (filter is null)
        {
            foreach (string candidate in args)
            {
                if (!string.IsNullOrEmpty(candidate) && Utility.fuzzyLocationSearch(candidate) is GameLocation location)
                {
                    this.Monitor.Log($"Pushing events in {location.Name}");
                    this.PushEvents(location, this.evts);
                }
            }
        }
        else
        {
            foreach (GameLocation? location in Game1.locations)
            {
                if (filter(location.Name))
                {
                    this.PushEvents(location, this.evts);
                }
            }
        }

        this.Hook();
    }

    private void PushEvents(GameLocation location, Stack<EventRecord> evts)
    {
        if (!location.TryGetLocationEvents(out _, out Dictionary<string, string>? events) || events.Count == 0)
        {
            this.Monitor.Log($"{location.Name} appears to lack events, skipping.");
            return;
        }

        this.Monitor.Log($"Location {location.Name} has {events.Count} entries.", LogLevel.Info);

        foreach (string key in events.Keys)
        {
            if (location is not FarmHouse && (key.StartsWith("558291/") || key.StartsWith("558292/")))
            {
                this.Monitor.Log($"Skipping gramps event {key}");
                continue;
            }
            else if (!int.TryParse(key, out int _) && key.IndexOf('/') < 0)
            {
                this.Monitor.Log($"{key} is likely a fork, skipping...");
                continue;
            }
            foreach (string segment in key.Split('/'))
            {
                if (segment.StartsWith("x "))
                {
                    this.Monitor.Log($"{key} contains an x precondition, skipping as it is an unnatural event.");
                    goto Outer;
                }
            }
            evts.Push(new(location.Name, key));
Outer: ;
        }
    }

    #endregion

    private void GameLoop_UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
        {
            return;
        }

        // try to run events faster.
        try
        {
            int count = Config.EventSpeedRatio - 1;
            for (int i = 0; i < count; i++)
            {
                Game1.CurrentEvent?.Update(Game1.currentLocation, Game1.currentGameTime);
                Game1.currentMinigame?.tick(Game1.currentGameTime);

                if (Context.IsMainPlayer)
                {
                    ScreenFade? fade = this.Helper.Reflection.GetField<ScreenFade>(typeof(Game1), "screenFade")?.GetValue();
                    fade?.UpdateFade(Game1.currentGameTime);
                    if (Game1.globalFade)
                    {
                        fade?.UpdateGlobalFade();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Whoops, speeding up events may have been unwise. {ex}", LogLevel.Error);
        }

        // Run 6 times a second for speeed
        if (!e.IsMultipleOf(10)) return;

        if (this.iterationstoSkip-- > 0) return;

        // advance the abby minigame if it's up.
        if (Game1.currentMinigame is AbigailGame abbyGame && AbigailGame.onStartMenu)
        {
            abbyGame.receiveKeyPress(Keys.Space);
        }

        if (Game1.CurrentEvent is not null || Game1.eventUp)
        {
            // if (!Game1.game1.IsActive) return;
            if (Game1.CurrentEvent is not Event current)
            {
                return;
            }

            if (!string.IsNullOrEmpty(current.playerControlSequenceID))
            {
                this.Monitor.Log($"Clicking tile {current.playerControlTargetTile}", LogLevel.Debug);
                Game1.CurrentEvent.receiveActionPress(current.playerControlTargetTile.X, current.playerControlTargetTile.Y);
            }

            if (Game1.activeClickableMenu is DialogueBox db)
            {
                if (db.safetyTimer > 0)
                {
                    db.SpeedUp();
                    return;
                }

                if (db.isQuestion && db.selectedResponse == -1)
                {
                    // processed you before, continue clicking the same thing.
                    if (this._seen.TryGetValue(db, out IntHolder? value))
                    {
                        db.selectedResponse = value.Value;
                        if (ModEntry.Config.SkipDialogue)
                        {
                            db.receiveLeftClick(0, 0, false);
                        }

                        return;
                    }

                    string currentCommand = Game1.CurrentEvent.GetCurrentCommand() ?? string.Empty;
                    this.Monitor.Log($"Asked a question with {db.responses.Length} options: {db.characterDialoguesBrokenUp.FirstOrDefault() ?? db.dialogues.FirstOrDefault() ?? string.Empty}", LogLevel.Info);
                    this.Monitor.Log(currentCommand);
                    if (this.Monitor.IsVerbose)
                    {
                        this.Monitor.Log(JsonConvert.SerializeObject(db.responses), LogLevel.Trace);
                    }

                    if (Game1.CurrentEvent.id != this.currentEventId || this.workingNode is null)
                    {
                        this.Monitor.Log($"Hey, {Game1.CurrentEvent.id} not an event I launched! Running it to completion.", LogLevel.Info);
                        this.TrivialResponse(db);
                        return;
                    }

                    this.iterationstoSkip = 1;

                    if (currentCommand == "cave" || currentCommand.StartsWith("question null"))
                    {
                        this.TrivialResponse(db);
                        return;
                    }

                    if (currentCommand.StartsWith("speak "))
                    {
                        Dictionary<string, string>? responseDialogues = db.characterDialogue?.speaker?.Dialogue;
                        bool isTrivial = true;
                        if (responseDialogues is not null)
                        {
                            foreach (Response? r in db.responses)
                            {
                                if (responseDialogues.TryGetValue(r.responseKey, out string? data))
                                {
                                    this.Monitor.Log($"speak fork - response {r.responseKey} is {data}.");
                                    if (data.Contains("%fork"))
                                    {
                                        isTrivial = false;
                                    }
                                }
                                else
                                {
                                    this.Monitor.Log($"Missing dialogue key {r.responseKey} for {db.characterDialogue!.speaker.Name}", LogLevel.Warn);
                                }
                            }
                        }
                        else
                        {
                            this.Monitor.Log($"Could not find dialogue file for {db.characterDialogue?.speaker?.Name ?? "unknown speaker"}, this might be a problem.", LogLevel.Warn);
                        }
                        if (isTrivial)
                        {
                            this.TrivialResponse(db);
                            return;
                        }

                    }

                    // todo - merge back quickquestions?
                    bool isMergeBackQuickQuestion = false;
                    if (currentCommand.StartsWith("quickQuestion"))
                    {
                        isMergeBackQuickQuestion = true;
                        if (currentCommand.Contains("switchEvent") || currentCommand.Contains("fork") || currentCommand.Contains("$q"))
                        {
                            isMergeBackQuickQuestion = false;
                        }

                        string[] splits = currentCommand.Split("(break)");
                        if (db.responses.Length != splits.Length - 1)
                        {
                            this.Monitor.Log($"Mismatched query and response counts for quickQuestion for {currentCommand}.", LogLevel.Warn);
                        }

                        // check to see if it's just the same command repeated
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
                            this.TrivialResponse(db);
                            return;
                        }

                        // if this quickquestion is just used to add a different dialoguebox, skip.
                        bool isTrivial = true;
                        for (int i = 1; i < splits.Length; i++)
                        {
                            string subcommand = splits[i];
                            if (!subcommand.StartsWith("speak ") || subcommand.IndexOf('\\') != -1 || subcommand.Contains("%fork"))
                            {
                                isTrivial = false;
                            }
                        }

                        if (isTrivial)
                        {
                            this.TrivialResponse(db);
                            return;
                        }
                    }

                    switch (this.workingNode!.Color)
                    {
                        case Color.White:
                        {
                            for (int i = 0; i < db.responses.Length; i++)
                            {
                                Response response = db.responses[i];
                                if (string.IsNullOrEmpty(response.responseText))
                                {
                                    this.Monitor.Log($"{JsonConvert.SerializeObject(response)} appears to be empty, huh.", LogLevel.Warn);
                                    continue;
                                }

                                Node node = new(response.responseKey, i, []);
                                this.workingNode.Children.Add(node);
                            }

                            this.Monitor.Log($"First visit, selecting choice 0. {db.responses[0].responseText}", LogLevel.Debug);
                            if (isMergeBackQuickQuestion)
                            {
                                this.Monitor.Log($"This appears to be a simple quickQuestion, blue-ing extra children.");
                                for (int i = 1; i < this.workingNode.Children.Count; i++)
                                {
                                    this.workingNode.Children[i].Color = Color.Blue;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < db.responses.Length; i++)
                                {
                                    string text = db.responses[i].responseText;
                                    if (text.Length > 4 && !this.seenResponses.Add(text))
                                    {
                                        this.Monitor.Log($"Text response {text} seems to have been seen before, marking blue.");
                                        this.workingNode.Children[i].Color = Color.Blue;
                                    }
                                }
                            }

                            db.selectedResponse = 0;
                            this._seen.AddOrUpdate(db, new(0));
                            this.workingNode.Color = Color.Grey;
                            this.workingNode = this.workingNode.Children.First();
                            break;
                        }
                        case Color.Blue:
                        {
                            this.Monitor.Log($"Reached blue node, queue only single child.");
                            Node blue = new(db.responses[0].responseKey, 0, [])
                            {
                                Color = Color.Blue,
                            };
                            this.workingNode.Children.Add(blue);

                            db.selectedResponse = 0;
                            this._seen.AddOrUpdate(db, new(0));
                            this.workingNode.Color = Color.Grey;
                            this.workingNode = blue;
                            break;
                        }
                        case Color.Grey:
                        {
                            foreach (Node? child in this.workingNode.Children)
                            {
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

                                this.Monitor.Log($"Now selecting response {child.ResponsePosition}. {db.responses[child.ResponsePosition].responseText}", LogLevel.Debug);
                                db.selectedResponse = child.ResponsePosition;
                                this._seen.AddOrUpdate(db, new(child.ResponsePosition));
                                this.workingNode = child;
                                break;
                            }
                            break;
                        }
                        case Color.Black:
                        {
                            // todo - better cleanup.
                            Game1.CurrentEvent.skipEvent();
                            this.Monitor.Log($"How did I get here?", LogLevel.Warn);
                            this.Monitor.Log(JsonConvert.SerializeObject(this.tree), LogLevel.Trace);
                            this.current = null;
                            break;
                        }
                    }
                }

                if (ModEntry.Config.SkipDialogue)
                {
                    this.Monitor.VerboseLog("Clicking on the dialogue box");
                    db.safetyTimer = 0;
                    db.receiveLeftClick(0, 0, false);
                }
            }
            else if (Game1.activeClickableMenu is NamingMenu nm)
            {
                // Hope doing this at 4tps isn't a problem
                nm.receiveLeftClick(nm.doneNamingButton.bounds.Center.X, nm.doneNamingButton.bounds.Center.Y, false);
            }
            return;
        }

        // do I need to kill end of night menus?

        // can't queue new events if the game is trying to save/doing night stuff.
        if (!Game1.game1.IsActive || Game1.newDay || Game1.gameMode != Game1.playingGameMode)
        {
            return;
        }

        if (Game1.activeClickableMenu is { } menu)
        {
            menu.emergencyShutDown();
            menu.exitThisMenu();
            return;
        }

        // event ended, mark last node as black.
        if (this.workingNode is not null)
        {
            this.workingNode.Color = Color.Black;
            this.workingNode = null;
        }

        // re-launch the SAME event with the next set of choices.
        if (this.current is not null)
        {
            foreach (Node node in this.tree.Children)
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
        this.tree.Children.Clear();
        this.tree.Color = Color.White;
        this.workingNode = this.tree;
        this.seenResponses.Clear();

        while (this.evts.TryPop(out EventRecord pair))
        {
            this.LaunchEvent(pair);
            return;
        }

        this.Monitor.Log("Done, unhooking.", LogLevel.Info);
        this.UnHook();
        this.Helper.Data.WriteGlobalData("finished-events", this.completed);
    }

    /// <summary>Marks a response as trivial.</summary>
    private void TrivialResponse(DialogueBox db)
    {
        this.Monitor.Log($"Meaningless choice, skipping.");
        var response = Random.Shared.Next(db.responses.Length);
        db.selectedResponse = response;
        this._seen.AddOrUpdate(db, new(response));

        db.safetyTimer = 0;
        this.Monitor.VerboseLog("Clicking on the dialogue box.");
        db.receiveLeftClick(0, 0);
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private void LaunchEvent(EventRecord pair)
    {
        // Burn the players inventory every event to make sure space exists
        for (int i = Game1.player.Items.Count - 1; i >= 0; i--)
        {
            Item? item = Game1.player.Items[i];
            if (item is not null)
            {
                Game1.player.Items[i] = null;
            }
        }

        // copied out of Game1.PlayEvent
        if (Game1.getLocationFromName(pair.location) is not GameLocation actual)
        {
            return;
        }

        if (!actual.TryGetLocationEvents(out string? assetName, out Dictionary<string, string>? evtDict))
        {
            this.Monitor.Log($"Evts file for {actual.Name} now missing, what.", LogLevel.Warn);
            return;
        }

        string id = pair.eventKey.GetNthChunk('/').ToString();
        this.current = pair;
        this.currentEventId = id;

        this.Monitor.Log($"Playing {pair.eventKey}, {this.evts.Count} events remaining.", LogLevel.Info);
        this.iterationstoSkip = 8;

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
            Game1.globalFadeToBlack(afterFade: () =>
            {
                Game1.forceSnapOnNextViewportUpdate = true;
                Game1.currentLocation.startEvent(new Event(evtDict[pair.eventKey], assetName, id));
                this.current = pair;
                this.currentEventId = id;
                Game1.globalFadeToClear(null, 0.05f);
            },
            fadeSpeed: 0.05f);
        }
    }
}
