using StardewValley.Characters;

namespace SingleParenthood;
internal static class SPUtils
{
    internal static bool AllKidsOutOfCrib(this Farmer farmer)
        => farmer.getChildren().AllKidsOutOfCrib();

    internal static bool AllKidsOutOfCrib(this List<Child> kids)
    {
        foreach (Child kid in kids)
        {
            if (kid.Age <= Child.crawler)
            {
                return false;
            }
        }
        return true;
    }
}
