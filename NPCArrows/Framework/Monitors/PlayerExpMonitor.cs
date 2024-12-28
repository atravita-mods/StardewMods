using Microsoft.Xna.Framework;

namespace NPCArrows.Framework.Monitors;

/// <summary>
/// A class that adds text to when a player gains XP.
/// </summary>
internal class PlayerExpMonitor
{
    private readonly Farmer farmer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerExpMonitor"/> class.
    /// </summary>
    /// <param name="farmer">Farmer instance to track.</param>
    internal PlayerExpMonitor(Farmer farmer)
    {
        this.farmer = farmer;
        farmer.experiencePoints.OnFieldCreate += this.OnFieldCreated;
        foreach (var item in farmer.experiencePoints.Fields)
        {
            if (item is not null)
            {
                item.fieldChangeEvent += this.OnFieldChanged;
            }
        }
    }

    private void OnFieldCreated(int index, Netcode.NetInt field)
    {
        field.fieldChangeEvent += this.OnFieldChanged;
    }

    private void OnFieldChanged(Netcode.NetInt field, int oldValue, int newValue)
    {
        if (this.farmer.currentLocation is not { } location)
        {
            return;
        }

        location.debris.Add(new Debris($"EXP {newValue - oldValue}", 1, this.farmer.Position, Color.White, 1f, 0f));
    }
}