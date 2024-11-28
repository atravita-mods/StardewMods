using RefreshedRandom.Framework.PRNG;

namespace RefreshedRandom.Framework;

/// <summary>
/// The data for this mod.
/// </summary>
public sealed class ModData
{
    /// <summary>
    /// Gets or sets the cached number of ms the player has played.
    /// </summary>
    public int LastMilliseconds { get; set; } = -1;

    /// <summary>
    /// Gets or sets the cached number of steps the player has taken.
    /// </summary>
    public int LastSteps { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value generated randomly once per day.
    /// </summary>
    public int LastSeed { get; set; }

    public ModData()
    {
        this.LastSeed = GenerateSeed();
    }

    internal void Update(Farmer player)
    {
        this.LastMilliseconds = (int)(player.millisecondsPlayed ^ (player.millisecondsPlayed << 32));
        this.LastSteps = (int)player.stats.StepsTaken;
        this.LastSeed = GenerateSeed();
    }

    internal void PopulateIfBlank(Farmer player)
    {
        unchecked
        {
            if (this.LastMilliseconds < 0)
            {
                this.LastMilliseconds = (int)(player.millisecondsPlayed ^ (player.millisecondsPlayed << 32));
            }
        }

        if (this.LastSteps < 0)
        {
            this.LastSteps = (int)player.stats.StepsTaken;
        }
    }

    private static int GenerateSeed()
    {
        Span<byte> buffer = stackalloc byte[4];
        SeededXoshiroFactory.RNG.GetBytes(buffer);
        return BitConverter.ToInt32(buffer);
    }
}