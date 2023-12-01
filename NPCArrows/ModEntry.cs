namespace NPCArrows;

using AtraCore.Framework.Internal;

using NPCArrows.Framework;
using NPCArrows.Framework.NPCs;

using StardewModdingAPI.Events;

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
    }

    private void Display_RenderedHud(object? sender, RenderedHudEventArgs e)
    {
        IList<NPC>? characters = Game1.CurrentEvent?.actors;
        characters ??= Game1.currentLocation?.characters;

        if (Context.IsPlayerFree && characters is not null)
        {
            foreach (NPC? character in characters)
            {
                if (character?.CanSocialize == true)
                {
                    character.DrawArrow(e.SpriteBatch);
                }
            }
        }
    }
}
