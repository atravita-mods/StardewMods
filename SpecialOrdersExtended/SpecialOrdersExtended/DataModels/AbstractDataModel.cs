using System;
using System.Collections.Generic;
using System.Text;
using StardewModdingAPI;

using StardewValley;

namespace SpecialOrdersExtended.DataModels
{
    internal class AbstractDataModel
    {
        public string Savefile { get; set; }

        public virtual void Save(string identifier)
        {
            ModEntry.DataHelper.WriteGlobalData(Savefile + identifier, this);
        }

    }
}
