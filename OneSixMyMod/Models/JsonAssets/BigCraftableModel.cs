using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneSixMyMod.Models.JsonAssets;
public record BigCraftableModel(
    string Name,
    string? EnableWithMod,
    string? DisableWithMod,

    bool ReserveNextIndex,
    int ReserveExtraIndexCount,

    string? Description,
    int Price,
    bool ProvidesLight)
    : BaseIdModel(Name, EnableWithMod, DisableWithMod);