namespace NPCArrows;

using AtraCore.Framework.Caches;
using AtraCore.Framework.Internal;

using NPCArrows.Framework;
using NPCArrows.Framework.Monitors;
using NPCArrows.Framework.NPCs;

using StardewModdingAPI.Events;

using StardewValley.Characters;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        AssetManager.Initialize(helper.GameContent);

        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        helper.Events.Display.RenderedHud += this.Display_RenderedHud;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        NPC? lewis = NPCCache.GetByVillagerName("Lewis", true);

        Friendship lewisFriendship = Game1.player.friendshipData["Lewis"];

        BasicFriendshipMonitor monitor = new BasicFriendshipMonitor(lewisFriendship, lewis!);
    }

    private void Display_RenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (Game1.game1.takingMapScreenshot || Game1.farmEvent is not null || !Context.IsPlayerFree)
        {
            return;
        }

        IList<NPC>? characters = Game1.CurrentEvent?.actors;
        characters ??= Game1.currentLocation?.characters;

        if (characters is not null)
        {
            foreach (NPC? character in characters)
            {
                if (character?.CanSocialize == true || character is Child)
                {
                    character.DrawArrow(e.SpriteBatch);
                }
            }
        }
    }
}
