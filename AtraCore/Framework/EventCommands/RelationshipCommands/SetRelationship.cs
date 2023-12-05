using System.Reflection;

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;

using StardewValley.Delegates;
using StardewValley.Locations;

namespace AtraCore.Framework.EventCommands.RelationshipCommands;

#warning - TODO: make sure breakups also remove planned weddings.

/// <summary>
/// Lets you set the relationship status between the current player and an NPC.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SetRelationship"/> class.
/// </remarks>
/// <param name="multiplayer">SMAPI's multiplayer helper.</param>
/// <param name="uniqueID">This mod's uniqueID.</param>
internal sealed class SetRelationship(IMultiplayerHelper multiplayer, string uniqueID)
{
    /// <summary>
    /// The <see cref="MultiplayerDispatch"/> key used to request an NPC move.
    /// </summary>
    internal const string RequestNPCMove = "RequestNPCMove";

    private const string DEFAULT = "DEFAULT";

    private const char Sep = 'Ω';

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

            MethodInfo? reload = freeLoveEntry.GetMethod("ReloadSpouses", AccessTools.all, binder: null, [typeof(Farmer)], null);
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

    private string UniqueID { get; init; } = string.Intern(uniqueID);

    /// <inheritdoc cref="EventCommandDelegate"/>
    internal void ApplySetRelationship(Event @event, string[] args, EventContext context)
    {
        if (args.Length != 3 || args.Length != 4)
        {
            @event.LogCommandErrorAndSkip(args, "Event requires exactly two or three arguments.");
            return;
        }

        if (string.IsNullOrWhiteSpace(args[1]))
        {
            @event.LogCommandErrorAndSkip(args, $"NPC name may not be empty");
            return;
        }

        if (NPCCache.GetByVillagerName(args[1], searchTheater: true) is not NPC npc)
        {
            @event.LogCommandErrorAndSkip(args, $"Could not find NPC by name {args[1]}");
            return;
        }

        if (!npc.CanSocialize)
        {
            @event.LogCommandErrorAndSkip(args, $"NPC {npc.Name} is antisocial");
            return;
        }

        if (!FriendshipEnumExtensions.TryParse(args[2], out FriendshipEnum next, ignoreCase: true))
        {
            @event.LogCommandErrorAndSkip(args, $"Could not parse {args[2]} as valid friendship value.");
            return;
        }

        if (!FriendshipEnumExtensions.IsDefined(next))
        {
            @event.LogCommandErrorAndSkip(args, $"Could not parse {args[2]} as valid friendship value.");
            return;
        }

        if (next == FriendshipEnum.Engaged && Game1.player.isEngaged() && npc.Name != Game1.player.spouse)
        {
            @event.LogCommandErrorAndSkip(args, $"Farmer is already engaged, becoming engaged again will not end well.");
            return;
        }

        // extra value to set post-command friendship OR days until wedding.
        int val;
        if (args.Length == 4)
        {
            // extra value to set post-command friendship OR days until wedding.
            if (!int.TryParse(args[3], out val))
            {
                @event.LogCommandErrorAndSkip(args, $"Could not parse {args[3]} as integer.");
                return;
            }
        }
        else
        {
            val = next switch
            {
                FriendshipEnum.Engaged => 3,
                FriendshipEnum.Friendly => 1250,
                _ => 0
            };
        }

        FriendshipEnum past;
        if (Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship? friendship))
        {
            past = friendship.RoommateMarriage ? FriendshipEnum.Roommate : (FriendshipEnum)friendship.Status;
        }
        else
        {
            past = FriendshipEnum.Unmet;
        }

        if (past == next)
        {
            ModEntry.ModMonitor.Log($"No change needed for {args[1]} - {args[2]}");
            @event.CurrentCommand++;
            return;
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
                    Game1.Multiplayer.globalChatInfoMessage("BreakUp", Game1.player.Name, npc.displayName);
                    friendship.Points = val;

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
                    Game1.Multiplayer.globalChatInfoMessage("Dating", Game1.player.Name, npc.displayName);
                }
                break;
            case FriendshipEnum.Engaged:
                friendship.Status = FriendshipStatus.Engaged;
                Game1.player.spouse = npc.Name;
                WorldDate weddingDate = new(Game1.Date);
                weddingDate.TotalDays += val;
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
                friendship.Points = val;
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
                    this.SendMoveRequest($"{npc.Name}{Sep}{DEFAULT}");
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
                this.SendMoveRequest($"{npc.Name}{Sep}{Game1.player.UniqueMultiplayerID}");
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

        @event.CurrentCommand++;
    }

    /// <summary>
    /// Processes a move request.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void ProcessMoveRequest(ModMessageReceivedEventArgs e)
    {
        string message = e.ReadAs<string>();

        if (!message.TrySplitOnce(Sep, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
        {
            ModEntry.ModMonitor.Log($"Failed to parse message: {message} while handling move request.", LogLevel.Warn);
            return;
        }

        if (NPCCache.GetByVillagerName(first.ToString(), searchTheater: true) is not NPC npc)
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
        if (npc.Schedule is not null)
        {
            npc.ClearSchedule();
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
        multiplayer.SendMessage(
            message: message,
            messageType: RequestNPCMove,
            modIDs: [this.UniqueID],
            playerIDs: [Game1.MasterPlayer.UniqueMultiplayerID]);
    }
}
