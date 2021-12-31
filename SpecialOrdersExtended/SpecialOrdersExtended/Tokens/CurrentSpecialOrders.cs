namespace SpecialOrdersExtended.Tokens
{
    internal class CurrentSpecialOrders: AbstractToken
    {
        /// <summary>Update the values when the context changes.</summary>
        /// <returns>Returns whether the value changed, which may trigger patch updates.</returns>
        public bool UpdateContext()
        {
            List<string> specialOrderNames = Game1.player?.team?.specialOrders?.Select((SpecialOrder s) => s.questKey.ToString())?.OrderBy(a => a)?.ToList() 
                ?? SaveGame.loaded?.specialOrders?.Select((SpecialOrder s) => s.questKey.ToString())?.OrderBy(a => a)?.ToList();
            if (specialOrderNames == SpecialOrdersCache)
            {
                return false;
            }
            else
            {
                SpecialOrdersCache = specialOrderNames;
                return true;
            }
        }
    }
}
