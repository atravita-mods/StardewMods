using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatGiftsRedux.Framework;
public class CatGiftsReduxAPI : ICatGiftReduxAPI
{
    private ModEntry us;
    private IModInfo them;

    internal CatGiftsReduxAPI(ModEntry us, IModInfo them)
    {
        this.us = us;
        this.them = them;
    }

    public void AddPicker(Func<Random, Item?> picker, double weight = 100)
    {
        if (weight > 0)
        {
            this.us.Monitor.Log($"Adding picker from: {this.them.Manifest.UniqueID}");
            this.us.AddPicker(weight, picker);
        }
        else
        {
            this.us.Monitor.Log($"Skipping picker from: {this.them.Manifest.UniqueID}, weight must be positive.");
        }
    }
}
