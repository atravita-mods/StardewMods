namespace SpecialOrdersExtended.Tokens
{
    internal class CompletedSpecialOrders: AbstractToken
    {
        /// <summary>Update the values when the context changes.</summary>
        /// <returns>Returns whether the value changed, which may trigger patch updates.</returns>
        public bool UpdateContext()
        {
            List<string> specialOrderNames;
            if (Context.IsWorldReady)
            {
                specialOrderNames = Game1.player.team.completedSpecialOrders.Keys.OrderBy(a => a)?.ToList();
            }
            else
            {
                specialOrderNames = SaveGame.loaded?.completedSpecialOrders?.OrderBy(a => a)?.ToList();
            }

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
