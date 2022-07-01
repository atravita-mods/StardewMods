using AtraBase.Toolkit.Extensions;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

namespace AtraCore.Framework.ItemManagement;
internal static class DataToItemMap
{
    private static readonly SortedList<ItemTypeEnum, IAssetName> _map = new(7);

    private static readonly SortedList<ItemTypeEnum, Lazy<Dictionary<string, int>>> _nameToIDMap = new(8);

    /// <summary>
    /// Given an ItemType and a name, gets the id.
    /// </summary>
    /// <param name="type">type of the item.</param>
    /// <param name="name">name of the item.</param>
    /// <returns>Integer ID, or -1 if not found.</returns>
    public static int GetID(ItemTypeEnum type, string name, bool resolveRecipesSeperately = false)
    {
        if (!resolveRecipesSeperately)
        {
            type &= ~ItemTypeEnum.Recipe;
        }
        if (!_nameToIDMap.TryGetValue(type, out Lazy<Dictionary<string, int>>? asset))
        {
            if (!type.HasFlag(ItemTypeEnum.SObject) || !_nameToIDMap.TryGetValue(ItemTypeEnum.SObject, out asset))
            {
                return -1;
            }
        }
        if (asset.Value.TryGetValue(name, out int id))
        {
            return id;
        }
        return -1;
    }

    /// <summary>
    /// Sets up various maps.
    /// </summary>
    /// <param name="helper">GameContentHelper.</param>
    internal static void Init(IGameContentHelper helper)
    {
        // Populate item-to-asset-map.
        // Note: Rings are in ObjectInformation, because
        // nothing is nice. So are boots, but they have their own data asset as well.
        _map.Add(ItemTypeEnum.BigCraftable, helper.ParseAssetName(@"Data\BigCraftablesInformation"));
        _map.Add(ItemTypeEnum.Boots, helper.ParseAssetName(@"Data\Boots"));
        _map.Add(ItemTypeEnum.Clothing, helper.ParseAssetName(@"Data\ClothingInformation"));
        _map.Add(ItemTypeEnum.Furniture, helper.ParseAssetName(@"Data\Furniture"));
        _map.Add(ItemTypeEnum.Hat, helper.ParseAssetName(@"Data\hats"));
        _map.Add(ItemTypeEnum.SObject, helper.ParseAssetName(@"Data\ObjectInformation"));
        _map.Add(ItemTypeEnum.Weapon, helper.ParseAssetName(@"Data\weapons"));

        // load the lazies.
        Reset();
    }

    /// <summary>
    /// Resets the requested name-to-id maps.
    /// </summary>
    /// <param name="assets">Assets to reset, or null for all.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        bool ShouldReset(IAssetName name) => assets is null || assets.Contains(name);

        if (ShouldReset(_map[ItemTypeEnum.SObject]))
        {
            if (!_nameToIDMap.TryGetValue(ItemTypeEnum.SObject, out var sobj) || sobj.IsValueCreated)
            {
                _nameToIDMap[ItemTypeEnum.SObject] = new(() =>
                {
                    ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve normal objects.", LogLevel.Info);

                    Dictionary<string, int> mapping = new();
                    foreach ((int id, string data) in Game1.objectInformation)
                    {
                        // category asdf should never end up in the player inventory.
                        var cat = data.GetNthChunk('/', SObject.objectInfoTypeIndex);
                        if (cat.Equals("asdf", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var name = data.GetNthChunk('/', SObject.objectInfoNameIndex);
                        if (name.Equals("Stone", StringComparison.OrdinalIgnoreCase) && id != 390)
                        {
                            continue;
                        }
                        if (name.Equals("Weeds", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        if (!mapping.TryAdd(name.ToString(), id))
                        {
                            ModEntry.ModMonitor.Log($"{name.ToString()} with {id} seems to be a duplicate SObject and may not be resolved correctly.", LogLevel.Warn);
                        }
                    }
                    return mapping;
                });
            }
            if (!_nameToIDMap.TryGetValue(ItemTypeEnum.Ring, out var rings) || rings.IsValueCreated)
            {
                _nameToIDMap[ItemTypeEnum.Ring] = new(() =>
                {
                    ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve rings.", LogLevel.Info);

                    Dictionary<string, int> mapping = new();
                    foreach ((int id, string data) in Game1.objectInformation)
                    {
                        // wedding ring (801) isn't a real ring.
                        if (id == 801 || !data.GetNthChunk('/',3).Equals("Ring", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var name = data.GetNthChunk('/', 0).ToString();
                        if (!mapping.TryAdd(name, id))
                        {
                            ModEntry.ModMonitor.Log($"{name} with {id} seems to be a duplicate Ring and may not be resolved correctly.", LogLevel.Warn);
                        }
                    }
                    return mapping;
                });
            }
        }

        if (ShouldReset(_map[ItemTypeEnum.Boots]) && (!_nameToIDMap.TryGetValue(ItemTypeEnum.Boots, out var boots) || boots.IsValueCreated))
        {
            _nameToIDMap[ItemTypeEnum.Boots] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Boots", LogLevel.Info);

                Dictionary<string, int> mapping = new();
                foreach ((int id, string data) in Game1.content.Load<Dictionary<int, string>>(_map[ItemTypeEnum.Boots].BaseName))
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (!mapping.TryAdd(name, id))
                    {
                        ModEntry.ModMonitor.Log($"{name} with {id} seems to be a duplicate Boots and may not be resolved correctly.", LogLevel.Warn);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(_map[ItemTypeEnum.BigCraftable]) && (!_nameToIDMap.TryGetValue(ItemTypeEnum.BigCraftable, out var bc) || bc.IsValueCreated))
        {
            _nameToIDMap[ItemTypeEnum.BigCraftable] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve BigCraftables", LogLevel.Info);

                Dictionary<string, int> mapping = new();
                foreach ((int id, string data) in Game1.bigCraftablesInformation)
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (!mapping.TryAdd(name, id))
                    {
                        ModEntry.ModMonitor.Log($"{name} with {id} seems to be a duplicate BigCraftable and may not be resolved correctly.", LogLevel.Warn);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(_map[ItemTypeEnum.Clothing]) && (!_nameToIDMap.TryGetValue(ItemTypeEnum.Clothing, out var clothing) || clothing.IsValueCreated))
        {
            _nameToIDMap[ItemTypeEnum.Clothing] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Clothing", LogLevel.Info);

                Dictionary<string, int> mapping = new();
                foreach ((int id, string data) in Game1.clothingInformation)
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (!mapping.TryAdd(name, id))
                    {
                        ModEntry.ModMonitor.Log($"{name} with {id} seems to be a duplicate ClothingItem and may not be resolved correctly.", LogLevel.Warn);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(_map[ItemTypeEnum.Furniture]) && (!_nameToIDMap.TryGetValue(ItemTypeEnum.Furniture, out var furniture) || furniture.IsValueCreated))
        {
            _nameToIDMap[ItemTypeEnum.Furniture] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Furniture", LogLevel.Info);

                Dictionary<string, int> mapping = new();
                foreach ((int id, string data) in Game1.content.Load<Dictionary<int, string>>(_map[ItemTypeEnum.Furniture].BaseName))
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (!mapping.TryAdd(name, id))
                    {
                        ModEntry.ModMonitor.Log($"{name} with {id} seems to be a duplicate Furniture Item and may not be resolved correctly.", LogLevel.Warn);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(_map[ItemTypeEnum.Hat]) && (!_nameToIDMap.TryGetValue(ItemTypeEnum.Hat, out var hats) || hats.IsValueCreated))
        {
            _nameToIDMap[ItemTypeEnum.Hat] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Hats", LogLevel.Info);

                Dictionary<string, int> mapping = new();
                foreach ((int id, string data) in Game1.content.Load<Dictionary<int, string>>(_map[ItemTypeEnum.Hat].BaseName))
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (!mapping.TryAdd(name, id))
                    {
                        ModEntry.ModMonitor.Log($"{name} with {id} seems to be a duplicate Hat and may not be resolved correctly.", LogLevel.Warn);
                    }
                }
                return mapping;
            });
        }
    }
}
