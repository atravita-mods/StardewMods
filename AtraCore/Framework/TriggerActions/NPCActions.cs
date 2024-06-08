using AtraCore.Framework.Caches;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using StardewValley.Delegates;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace AtraCore.Framework.TriggerActions;
internal static class NPCActions
{
    internal static bool ChangeAppearance(string[] args, TriggerActionContext context, out string? error)
    {
        if (!ArgUtility.TryGet(args, 1, out string? name, out error))
        {
            return false;
        }

        NPC? eventNPC = Game1.CurrentEvent?.actors?.FirstOrDefault(n => n.Name == name);
        NPC? npc = NPCCache.GetByVillagerName(name) ?? eventNPC;

        if (npc is null)
        {
            error = $"Could not locate NPC by name {name}.";
            return false;
        }

        ArgUtility.TryGetOptionalRemainder(args, 2, out string appearanceName);
        if (!string.IsNullOrWhiteSpace(appearanceName))
        {
            CharacterData? data = npc.GetData();
            if (data is null)
            {
                ModEntry.ModMonitor.Log($"Could not find character data for {npc.Name} for trigger action: {string.Join(' ', args)}", LogLevel.Warn);
                ModEntry.ModMonitor.Log(JsonConvert.SerializeObject(context));
                goto fallback;
            }

            var appearance = data.Appearance?.FirstOrDefault(a => a.Id == appearanceName);
            if (appearance is null)
            {
                ModEntry.ModMonitor.Log($"Could not find appearance data {appearanceName} for {npc.Name} for trigger action: {string.Join(' ', args)}", LogLevel.Warn);
                ModEntry.ModMonitor.Log(JsonConvert.SerializeObject(context));
                goto fallback;
            }

            if (!npc.TryLoadPortraits(appearance.Portrait, out error))
            {
                return false;
            }

            if (!npc.TryLoadSprites(appearance.Sprite, out error))
            {
                return false;
            }

            npc.LastAppearanceId = appearanceName;

            if (!ReferenceEquals(npc, eventNPC) && eventNPC is not null)
            {
                if (!eventNPC.TryLoadPortraits(appearance.Portrait, out error))
                {
                    return false;
                }
                if (!eventNPC.TryLoadSprites(appearance.Sprite, out error))
                {
                    return false;
                }
            }

            return true;
        }

        fallback:
        int prevX = npc.Sprite.SpriteWidth;
        int prevY = npc.Sprite.SpriteHeight;

        npc.ChooseAppearance();
        npc.Sprite.SpriteWidth = prevX;
        npc.Sprite.SpriteHeight = prevY;

        if (!ReferenceEquals(npc, eventNPC) && eventNPC is not null)
        {
            int event_prevX = eventNPC.Sprite.SpriteWidth;
            int event_prevY = eventNPC.Sprite.SpriteHeight;

            eventNPC.ChooseAppearance();
            eventNPC.Sprite.SpriteWidth = event_prevX;
            eventNPC.Sprite.SpriteHeight = event_prevY;
        }

        return true;
    }

    internal static bool Emote(string[] args, TriggerActionContext context, out string? error)
    {
        if (!ArgUtility.TryGet(args, 1, out var name, out error))
        {
            return false;
        }

        if (!TryGetActor(name, args, out Character? actor, out error))
        {
            return error is null;
        }

        if (!ArgUtility.TryGetInt(args, 2, out var emoteId, out error))
        {
            return false;
        }

        actor.doEmote(emoteId, false);
        return true;
    }

    internal static bool FaceDirection(string[] args, TriggerActionContext context, out string? error)
    {
        if (!ArgUtility.TryGet(args, 1, out var name, out error))
        {
            return false;
        }

        if (!TryGetActor(name, args, out Character? actor, out error))
        {
            return error is null;
        }

        if (!ArgUtility.TryGet(args, 2, out var sDirection, out error))
        {
            return false;
        }

        if (Utility.TryParseDirection(sDirection, out var direction))
        {
            actor.faceDirection(direction);
        }
        else if (sDirection.Equals("opposite", StringComparison.OrdinalIgnoreCase))
        {
            actor.faceDirection((actor.FacingDirection + 2 ) % 4);
        }
        else if (sDirection.Equals("farmer", StringComparison.OrdinalIgnoreCase))
        {
            actor.faceDirection(-3);
        }
        else
        {
            error = $"could not parse {sDirection} as a valid facing direction.";
            return false;
        }

        return true;

    }

    internal static bool Jump(string[] args, TriggerActionContext context, out string? error)
    {
        if (!ArgUtility.TryGet(args, 1, out var name, out error))
        {
            return false;
        }

        if (!TryGetActor(name, args, out Character? actor, out error))
        {
            return error is null;
        }

        if (!ArgUtility.TryGetOptionalInt(args, 2, out var initialVelocity, out error, defaultValue: 4))
        {
            return false;
        }

        actor.jump(initialVelocity);
        return true;
    }

    internal static bool OpenShop(string[] args, TriggerActionContext context, out string error)
    {
        if (!ArgUtility.TryGet(args, 1, out string? shop, out error))
        {
            return false;
        }

        if (!DataLoader.Shops(Game1.content).ContainsKey(shop))
        {
            error = $"no shop by the name {shop} found";
            return false;
        }

        if (!ArgUtility.TryGetOptional(args, 2, out string? owner, out error))
        {
            return false;
        }

        // TODO: make sure events are sane.

        var stashed_menu = Game1.activeClickableMenu;
        if (!Utility.TryOpenShopMenu(shop, owner) || Game1.activeClickableMenu is not ShopMenu shopMenu)
        {
            error = $"could not open shop {shop}";
            return false;
        }

        if (stashed_menu is not null)
        {
            shopMenu.exitFunction += () =>
            {
                Game1.actionsWhenPlayerFree.Add(() => Game1.activeClickableMenu = stashed_menu);
            };
        }

        return true;
    }

    internal static bool TextOverHead(string[] args, TriggerActionContext context, out string error)
    {
        if (!ArgUtility.TryGet(args, 1, out var name, out error))
        {
            return false;
        }

        NPC? npc = Game1.currentLocation.characters.OfType<NPC>().FirstOrDefault(character => character.Name == name);
        if (npc is null)
        {
            ModEntry.ModMonitor.VerboseLog($"{name} not found on map, skipping action command: {string.Join(' ', args)}");
            if (!Game1.characterData.ContainsKey(name))
            {
                error = $"{name} refers to an NPC that could not be found. Are they not installed?";
                return false;
            }
            return true;
        }

        if (!ArgUtility.TryGet(args, 2, out var text, out error))
        {
            return false;
        }

        var actual = text.ParseTokens();
        if (actual is null)
        {
            error = $"could not parse {text} as valid localized string.";
            return false;
        }

        if (!ArgUtility.TryGetOptionalInt(args, 3, out var duration, out error, defaultValue: 3000))
        {
            return false;
        }

        if (!ArgUtility.TryGetOptional(args, 4, out var sColor, out error))
        {
            return false;
        }

        Color? color = null;
        if (sColor is not null && AtraShared.Utils.ColorHandler.TryParseColor(sColor, out var proposedColor))
        {
            color = proposedColor;
        }

        npc.clearTextAboveHead();
        npc.showTextAboveHead(actual, color, 2, duration);
        return true;
    }

    private static bool TryGetActor(string name, string[] args, [NotNullWhen(true)] out Character? actor, out string? error)
    {
        if (!Context.IsWorldReady)
        {
            error = "a save has not been loaded yet";
            actor = null;
            return false;
        }

        error = null;
        actor = name.Equals("farmer", StringComparison.OrdinalIgnoreCase)
            ? Game1.player
            : Game1.currentLocation?.characters.FirstOrDefault(character => character is NPC npc && npc.IsVillager && npc.Name == name);
        if (actor is null)
        {
            ModEntry.ModMonitor.VerboseLog($"{name} not found on map, skipping action command: {string.Join(' ', args)}");
            if (Game1.characterData?.ContainsKey(name) != true)
            {
                error = $"{name} refers to an NPC that could not be found. Are they not installed?";
            }
            return false;
        }
        return true;
    }
}