using MiniAtraShared.Models;

using StardewModdingAPI.Events;

using StardewValley.Menus;

namespace AlwaysSonarBobber;

/// <inheritdoc />
internal sealed class ModEntry: BaseMod<ModEntry>
{

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        helper.Events.Display.MenuChanged += this.OnMenuChanged;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        const string sonar_bobber = "(O)SonarBobber";
        if (e.NewMenu is BobberBar bobberBar)
        {
            if (!bobberBar.bobbers.Contains(sonar_bobber))
            {
                bobberBar.bobbers.Add(sonar_bobber);
            }
        }
    }
}
