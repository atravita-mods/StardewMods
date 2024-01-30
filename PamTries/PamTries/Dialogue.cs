using AtraCore.Framework.Caches;

using StardewModdingAPI.Events;
using StardewValley.Characters;
using StardewValley.Locations;

namespace PamTries;

internal static class DialogueManager
{
    public static void GrandKidsDialogue(object? sender, DayStartedEventArgs e)
    {
        if (NPCCache.GetByVillagerName("Pam") is not NPC pam)
        {
            return;
        }
    }

    public static string? CurrentMovie()
    {
        if (!Context.IsWorldReady)
        {
            return null;
        }
        return MovieTheater.GetMovieForDate(Game1.Date)?.Id;
    }

    internal static string ChildCount()
        => $"{Game1.player.getChildrenCount()}";

    internal static string? ListChildren()
    {
        if (!Context.IsWorldReady)
        {
            return null;
        }

        List<Child> children = Game1.player.getChildren();
        string and = PTUtilities.GetLexicon("and");

        if (children is null || children.Count == 0)
        {
            return string.Empty;
        }
        else if (children.Count == 1)
        {
            return children[0].displayName;
        }
        else if (children.Count == 2)
        {
            return $"{children[0].displayName} {and} {children[1].displayName}";
        }
        else
        {
            List<string> kidnames = children.Select((Child child) => child.displayName).ToList();
            return $"{string.Join(", ", kidnames, 0, kidnames.Count - 1)}, {and} {kidnames[kidnames.Count]}";
        }// deal with the possibility other countries have different grammar later.
    }

    // TODO: make this case insensitive.

    internal static NPC? GetChildbyGender(string gender)
    {
        if (!Context.IsWorldReady)
        {
            return null;
        }

        Gender gender_int = gender switch
        {
            "girl" => Gender.Female,
            "boy" => Gender.Male,
            _ => Gender.Undefined,
        };
        foreach (NPC child in Game1.player.getChildren())
        {
            if (child.Gender == gender_int)
            {
                return child;
            }
        }
        return null;
    }
}