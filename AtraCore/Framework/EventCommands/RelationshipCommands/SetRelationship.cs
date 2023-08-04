using System.Reflection;

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;
using AtraCore.Interfaces;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Locations;

namespace AtraCore.Framework.EventCommands.RelationshipCommands;

#warning - TODO: make sure breakups also remove planned weddings.

/// <summary>
/// Lets you set the relationship status between the current player and an NPC.
/// </summary>
internal sealed class SetRelationship : IEventCommand
{
    private const string RequestNPCMove = "RequestNPCMove";

    private const string DEFAULT = "DEFAULT";

    #region delegates
    private static readonly Lazy<Action<Farmer>?> _freeLoveReload = new(() =>
    {
        try
        {
            Type freeLoveEntry = AccessTools.TypeByName("FreeLove.ModEntry");
            if (freeLoveEntry is null)
            {
                ModEntry.ModMonitor.Log($"Free love not found");
                return null;
            }

            MethodInfo? reload = freeLoveEntry.GetMethod("ReloadSpouses", AccessTools.all, binder: null, new[] { typeof(Farmer) }, null);
            if (reload is null)
            {
                return null;
            }
            return reload.CreateDelegate<Action<Farmer>>();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trying to create delegate for free love's spouse reload", ex);
            return null;
        }
    });
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SetRelationship"/> class.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="multiplayer">SMAPI's multiplayer helper.</param>
    /// <param name="uniqueID">This mod's uniqueID.</param>
    public SetRelationship(string name, IMonitor monitor, IMultiplayerHelper multiplayer, string uniqueID)
    {
        this.Name = name;
        this.Monitor = monitor;
        this.Multiplayer = multiplayer;
        this.UniqueID = string.Intern(uniqueID);
    }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public IMonitor Monitor { get; init; }

    private IMultiplayerHelper Multiplayer { get; init; }

    private string UniqueID { get; init; }

    /// <inheritdoc />
    public bool Validate(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        if (args.Length != 3 || args.Length != 4)
        {
            error = "Event requires exactly two or three arguments.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(args[1]))
        {
            error = $"NPC name may not be empty";
            return false;
        }

        if (NPCCache.GetByVillagerName(args[1], searchTheater: true) is not NPC npc)
        {
            error = $"Could not find NPC by name {args[1]}";
            return false;
        }

        try
        {
            if (!npc.CanSocialize)
            {
                error = $"NPC {npc.Name} is antisocial";
                return false;
            }
        }
        catch (Exception ex)
        {
            error = $"Error checking if {npc.Name} is antisocial, weird.";
            this.Monitor.Log(ex.ToString());
            return false;
        }

        if (!FriendshipEnumExtensions.TryParse(args[2], out FriendshipEnum friendship, ignoreCase: true))
        {
            error = $"Could not parse {args[2]} as valid friendship value.";
            return false;
        }

        if (!FriendshipEnumExtensions.IsDefined(friendship))
        {
            error = $"Could not parse {args[2]} as valid friendship value.";
            return false;
        }

        if (friendship == FriendshipEnum.Engaged && Game1.player.isEngaged() && npc.Name != Game1.player.spouse)
        {
            error = $"Farmer is already engaged, becoming engaged again will not end well.";
            return false;
        }

        // extra value to set post-command friendship OR days until wedding.
        if (args.Length == 4 && !int.TryParse(args[3], out _))
        {
            error = $"Could not parse {args[3]} as integer.";
            return false;
        }

        error = null;
        return true;
    }

    /// <inheritdoc />
    public bool Apply(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        if (args.Length != 3 || args.Length != 4)
        {
            error = "Event requires exactly two or three arguments.";
            return true;
        }

        NPC? npc = NPCCache.GetByVillagerName(args[1], searchTheater: true);
        if (npc is null)
        {
            error = $"Could not find NPC by name {npc}";
            return true;
        }

        FriendshipEnum past;
        if (Game1.player.friendshipData.TryGetValue(args[1], out Friendship? friendship))
        {
            past = friendship.RoommateMarriage ? FriendshipEnum.Roommate : (FriendshipEnum)friendship.Status;
        }
        else
        {
            past = FriendshipEnum.Unmet;
        }

        if (!FriendshipEnumExtensions.TryParse(args[2], out FriendshipEnum next, ignoreCase: true) || !FriendshipEnumExtensions.IsDefined(next))
        {
            error = $"Could not parse {args[2]} as valid friendship value.";
            return true;
        }

        int value;
        if (args.Length == 4)
        {
            if (!int.TryParse(args[3], out int tempVal))
            {
                error = $"Could not parse {args[3]} as integer.";
                return true;
            }
            else
            {
                value = tempVal;
            }
        }
        else
        {
            value = next switch
            {
                FriendshipEnum.Engaged => 3,
                FriendshipEnum.Friendly => 1250,
                _ => 0
            };
        }

        if (past == next)
        {
            ModEntry.ModMonitor.Log($"No change needed for {args[1]} - {args[2]}");
            error = null;
            return true;
        }

        // previously unmet
        if (friendship is null)
        {
            Game1.player.friendshipData[args[1]] = friendship = new();
        }

        switch (next)
        {
            case FriendshipEnum.Friendly:
                friendship.Status = FriendshipStatus.Friendly;

                if (past is FriendshipEnum.Dating or FriendshipEnum.Engaged)
                {
                    MultiplayerHelpers.GetMultiplayer().globalChatInfoMessage("BreakUp", Game1.player.Name, npc.displayName);
                    friendship.Points = value;

                    // make sure to break off the engagement too.
                    friendship.WeddingDate = null;
                    if (Game1.player.spouse == npc.Name)
                    {
                        Game1.player.spouse = null;
                    }
                }
                break;
            case FriendshipEnum.Dating:
                friendship.Status = FriendshipStatus.Dating;
                if (past is FriendshipEnum.Friendly or FriendshipEnum.Unmet)
                {
                    MultiplayerHelpers.GetMultiplayer().globalChatInfoMessage("Dating", Game1.player.Name, npc.displayName);
                }
                break;
            case FriendshipEnum.Engaged:
                friendship.Status = FriendshipStatus.Engaged;
                Game1.player.spouse = npc.Name;
                WorldDate weddingDate = new(Game1.Date);
                weddingDate.TotalDays += value;
                while (!Game1.canHaveWeddingOnDay(weddingDate.DayOfMonth, weddingDate.Season))
                {
                    weddingDate.TotalDays++;
                }
                friendship.WeddingDate = weddingDate;
                break;
            case FriendshipEnum.Married:
                friendship.Status = FriendshipStatus.Married;
                friendship.RoommateMarriage = false;
                Game1.player.spouse = npc.Name;
                break;
            case FriendshipEnum.Roommate:
                friendship.Status = FriendshipStatus.Married;
                friendship.RoommateMarriage = true;
                Game1.player.spouse = npc.Name;
                break;
            case FriendshipEnum.Divorced:
                friendship.Status = FriendshipStatus.Divorced;
                friendship.RoommateMarriage = false;
                Game1.player.spouse = null;
                friendship.Points = value;
                break;
            case FriendshipEnum.Unmet:
                Game1.player.friendshipData.Remove(npc.Name);
                break;
        }

        // fix NPC housing.
        if (past is FriendshipEnum.Married or FriendshipEnum.Roommate)
        {
            if (next is not FriendshipEnum.Married and not FriendshipEnum.Roommate)
            {
                // send the NPC home.
                if (Context.IsMainPlayer)
                {
                    npc.reloadDefaultLocation();
                    ClearNPCSchedule(npc);
                }
                else
                {
                    this.SendMoveRequest($"{npc.Name}:{DEFAULT}");
                }

                // we call this so FreeLove is updated too
                try
                {
                    Game1.player.doDivorce();
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.LogError($"calling Farmer.doDivorce for {Game1.player.Name}", ex);
                }
            }
        }
        else if (next is FriendshipEnum.Married or FriendshipEnum.Roommate)
        {
            if (Context.IsMainPlayer)
            {
                MoveNPCtoFarmerHome(npc, Game1.player);
                ClearNPCSchedule(npc);
            }
            else
            {
                this.SendMoveRequest($"{npc.Name}:{Game1.player.UniqueMultiplayerID}");
            }
            try
            {
                _freeLoveReload.Value?.Invoke(Game1.player);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("asking Free Love to refresh spouse cache", ex);
            }
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Processes a move request.
    /// </summary>
    /// <param name="sender">smapi</param>
    /// <param name="e">event args.</param>
    internal void ProcessMoveRequest(object? sender, ModMessageReceivedEventArgs e)
    {
        if (Context.IsMainPlayer || e.FromModID != this.UniqueID || e.Type != RequestNPCMove)
        {
            return;
        }

        string message = e.ReadAs<string>();

        if (!message.TrySplitOnce(':', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
        {
            ModEntry.ModMonitor.Log($"Failed to parse message: {message} while handling move request.", LogLevel.Warn);
            return;
        }

        if (NPCCache.GetByVillagerName(first.ToString()) is not NPC npc)
        {
            ModEntry.ModMonitor.Log($"Failed to find NPC of name {first.ToString()} while handling move request.", LogLevel.Warn);
            return;
        }

        TryMove(npc, second);
    }

    private static void TryMove(NPC npc, ReadOnlySpan<char> home)
    {
        if (home.Equals(DEFAULT, StringComparison.OrdinalIgnoreCase))
        {
            npc.reloadDefaultLocation();
            ClearNPCSchedule(npc);
        }
        else if (long.TryParse(home, out long id) && FarmerHelpers.GetFarmerById(id) is Farmer farmer)
        {
            MoveNPCtoFarmerHome(npc, farmer);
        }
        else
        {
            ModEntry.ModMonitor.Log($"Failed to parse {home.ToString()} as new NPC home", LogLevel.Error);
        }
    }

    private static void ClearNPCSchedule(NPC npc)
    {
        npc.followSchedule = false;
        if (npc.Schedule is not null)
        {
            npc.Schedule = null;
        }
        npc.controller = null;
        npc.temporaryController = null;
        npc.InvalidateMasterSchedule();
        npc.Halt();
        Game1.warpCharacter(npc, npc.DefaultMap, npc.DefaultPosition / Game1.tileSize);
    }

    private static void MoveNPCtoFarmerHome(NPC npc, Farmer farmer)
    {
        // derived from Game1.prepareSpouseForWedding
        npc.DefaultMap = farmer.homeLocation.Value;
        npc.DefaultFacingDirection = 2;
        if (Game1.getLocationFromName(farmer.homeLocation.Value) is FarmHouse house)
        {
            npc.DefaultPosition = Utility.PointToVector2(house.getSpouseBedSpot(npc.Name)) * 64f;
        }
        ClearNPCSchedule(npc);

        try
        {
            _freeLoveReload.Value?.Invoke(farmer);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("asking Free Love to refresh spouse cache", ex);
        }
    }

    private void SendMoveRequest(string message)
    {
        this.Multiplayer.SendMessage(
            message: message,
            messageType: RequestNPCMove,
            modIDs: new[] { this.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }
}
