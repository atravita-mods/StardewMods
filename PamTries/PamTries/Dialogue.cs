using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;


namespace PamTries
{
    class Dialogue
    {
        public static void GrandKidsDialogue(object sender, DayStartedEventArgs e)
        {
            NPC Pam = Game1.getCharacterFromName("Pam");
            if (Pam == null) { return; }
        }

        public static string CurrentMovie()
        {
            if (!Context.IsWorldReady) { return null; }
            return MovieTheater.GetMovieForDate(Game1.Date).ID;
        }

        public static string ChildCount()
        {
            return $"{Game1.player.getChildrenCount()}";
        }

        public static string ListChildren()
        {
            if (!Context.IsWorldReady) { return null; }

            List<Child> Children = Game1.player.getChildren();
            string and = PTUtilities.GetLexicon("and");

            if (Children == null || Children.Count == 0)
            {
                return "";
            }
            else if (Children.Count == 1)
            {
                return Children[0].displayName;
            }
            else if (Children.Count == 2)
            {
                return $"{Children[0].displayName} {and} {Children[1].displayName}";
            }
            else
            {
                List<string> kidnames = Children.Select((Child child) => child.displayName).ToList();
                return $"{String.Join(", ", kidnames, 0, kidnames.Count - 1)}, {and} {kidnames[kidnames.Count]}";
            }//deal with the possibility other countries have different grammer later.
        }

        public static NPC getChildbyGender(string gender)
        {
            if (!Context.IsWorldReady) { return null; }
            int gender_int;
            switch (gender)
            {
                case "girl":
                    gender_int = 1;
                    break;
                case "boy":
                    gender_int = 0;
                    break;
                default:
                    return null;
            }
            foreach (NPC child in Game1.player.getChildren())
            {
                if (child.Gender == gender_int) { return child; }
            }
            return null;
        }

    }
}
