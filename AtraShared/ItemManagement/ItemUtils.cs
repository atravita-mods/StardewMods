using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley.Objects;
using StardewValley.Tools;

namespace AtraShared.ItemManagement;

internal static class ItemUtils
{
    internal static Item? GetItemFromIdentifier(string type, int id)
    {
        switch (type)
        {
            case "F":
            case "f":
                return Furniture.GetFurnitureInstance(id);
            case "O":
            case "o":
                return new SObject(id, 1);
            case "BL":
            case "bl":
                return new SObject(id, 1, isRecipe: true);
            case "BO": // big craftables...
            case "bo":
                return new SObject(Vector2.Zero, id);
            case "BBL":
            case "bbl":
                return new SObject(Vector2.Zero, id, isRecipe: true);
            case "R":
            case "r":
                return new Ring(id);
            case "B":
            case "b":
                return new Boots(id);
            case "W":
            case "w":
                return new MeleeWeapon(id);
            case "H":
            case "h":
                return new Hat(id);
            case "C":
            case "c":
                return new Clothing(id);
            default:
                return null;
        }
    }

}
