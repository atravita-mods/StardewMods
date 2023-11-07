// Ignore Spelling: npc isarrivaltime basekey

using System.Text;

using AtraBase.Toolkit;

using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;

namespace AtraShared.Schedules.DataModels;

/// <summary>
/// A single schedule point.
/// </summary>
public sealed class SchedulePoint
{
    private readonly NPC npc;
    private readonly string map;
    private readonly int time;
    private readonly int direction = 2;
    private readonly string? dialoguekey;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulePoint"/> class.
    /// </summary>
    /// <param name="random">Seeded random.</param>
    /// <param name="npc">NPC.</param>
    /// <param name="map">String mapname.</param>
    /// <param name="time">Timestamp for schedule point. Normally is the departure time, but can be set to arrival time (isarrivaltime).</param>
    /// <param name="point">Tile to arrive at.</param>
    /// <param name="isarrivaltime">Whether or not this is an arrival time schedule.</param>
    /// <param name="direction">Direction to face after arriving.</param>
    /// <param name="animation">Which animation to use after arrival.</param>
    /// <param name="basekey">Base dialogue key.</param>
    /// <param name="varKey">Variant dialogue key.</param>
    /// <remarks>If a dialogue key that isn't in the NPC's dialogue is given, will simply convert  to `null`.</remarks>
    public SchedulePoint(
        Random random,
        NPC npc,
        string map,
        int time,
        Point point,
        bool isarrivaltime = false,
        int direction = Game1.down,
        string? animation = null,
        string? basekey = null,
        string? varKey = null)
    {
        this.npc = npc;
        this.map = map;
        this.time = time;
        this.IsArrivalTime = isarrivaltime;
        this.Point = point;
        this.direction = direction;
        this.Animation = animation;
        if (npc.GetRandomDialogue(varKey, random) is string dialogueKey)
        {
            this.dialoguekey = dialogueKey;
        }
        else
        {
            this.dialoguekey = npc.GetRandomDialogue(basekey, random);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether gets whether or not the time should be an arrival time.
    /// </summary>
    /// <remarks>As the first point has to be an arrival, we allow this to be set outside this class.</remarks>
    public bool IsArrivalTime { get; set; } = false;

    /// <summary>
    /// Gets the point the schedule is set to happen on.
    /// </summary>
    public Point Point { get; init; }

    /// <summary>
    /// Gets the animation for this schedule point. Null for no animation.
    /// </summary>
    public string? Animation { get; init; }

    /// <summary>
    /// Method that converts a schedule point into a string Stardew can understand.
    /// </summary>
    /// <returns>Raw schedule string.</returns>
    [Pure]
    public override string ToString()
    {
        StringBuilder sb = StringBuilderCache.Acquire();
        this.AppendToStringBuilder(sb);
        return StringBuilderCache.GetStringAndRelease(sb);
    }

    /// <summary>
    /// Appends the data in this schedulepoint to a stringbuilder.
    /// </summary>
    /// <param name="sb">stringbuilder instance to use.</param>
    /// <returns>Same stringbuilder.</returns>
    public StringBuilder AppendToStringBuilder(StringBuilder sb)
    {
        if (this.IsArrivalTime)
        {
            sb.Append('a');
        }
        sb.Append(this.time).Append(' ')
          .Append(this.map).Append(' ')
          .Append(this.Point.X).Append(' ')
          .Append(this.Point.Y).Append(' ')
          .Append(this.direction);

        if (this.Animation is not null)
        {
            sb.Append(' ').Append(this.Animation);
        }
        if (this.dialoguekey is not null)
        {
            sb.Append(' ')
              .Append("\"Characters\\Dialogue\\")
              .Append(this.npc.Name)
              .Append(':')
              .Append(this.dialoguekey)
              .Append('"');
        }
        return sb;
    }
}