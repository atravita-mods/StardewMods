using System.Collections.Generic;
using System.Linq;

using StardewValley;

namespace SpecialOrdersExtended.Tokens
{
    internal class AvailableSpecialOrders: AbstractToken
    {

        /// <summary>Update the values when the context changes.</summary>
        /// <returns>Returns whether the value changed, which may trigger patch updates.</returns>
        public bool UpdateContext()
        {
            List<string> specialOrderNames = Game1.player?.team?.availableSpecialOrders?.Select((SpecialOrder s) => s.questKey.ToString()).OrderBy(a => a).ToList() 
                ?? SaveGame.loaded?.availableSpecialOrders?.Select((SpecialOrder s) => s.questKey.ToString()).OrderBy(a => a).ToList();
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
