namespace AtraShared.Utils;

public static class FarmerHelpers
{
    public static IEnumerable<Farmer> GetFarmers()
    => Game1.getAllFarmers().Where(f => f is not null);

    public static bool HasAnyFarmerRecievedFlag(string flag)
    {
        foreach (Farmer farmer in GetFarmers())
        {
            if (farmer.hasOrWillReceiveMail(flag))
            {
                return true;
            }
        }

        return false;
    }
}