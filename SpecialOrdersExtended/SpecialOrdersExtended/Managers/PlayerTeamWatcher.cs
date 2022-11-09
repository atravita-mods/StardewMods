using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecialOrdersExtended.Managers;
internal class PlayerTeamWatcher
{
    private void Blah()
    {
        Game1.player.team.specialOrders.OnElementChanged += this.SpecialOrders_OnElementChanged;
        Game1.player.team.specialOrders.OnArrayReplaced += this.SpecialOrders_OnArrayReplaced;
    }

    private void SpecialOrders_OnArrayReplaced(Netcode.NetList<SpecialOrder, Netcode.NetRef<SpecialOrder>> list, IList<SpecialOrder> before, IList<SpecialOrder> after)
    {
    }

    private void SpecialOrders_OnElementChanged(Netcode.NetList<SpecialOrder, Netcode.NetRef<SpecialOrder>> list, int index, SpecialOrder oldValue, SpecialOrder newValue)
    {
    }
}