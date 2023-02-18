Growable Giant Crops (Shovel Mod)
===========================
![Header image](docs/showcase.gif)

Lets you pick up and put down a lot of things. With a shovel. Also lets you buy certain things from either Robin or the Witch.

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download and install [AtraCore](https://www.nexusmods.com/stardewvalley/mods/12932) and [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Uninstall
Remove any large inventory items you have in your inventory, and then delete from your Mods directory.

A special note for SolidFoundations: **absolutely** make sure to remove every instance of the inventory version of fruit trees, trees, giant crops, and resource clumps from Solid Foundations buildings before removing this mod.

## Configuration
Run SMAPI at least once with this mod installed to generate the `config.json`, or use [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) to configure.

TODO

## Shops
* Robin's shop opens up after you get a single heart from her and you've gotten her letter on the first house expansion. She'll carry grass starters, and also will carry resource clumps after you've reached the bottom of the mines. These grass starters will not die in winter, and you can control if they spread or not.
* The Witch Hut shop will open when you reach that location. It'll carry the shovel (if you're not currently carrying one in your inventory), a selection of giant crops, nodes, twigs, weeds, and some wild trees. After perfection, the limits on this store are removed.

## Technical Notes:
* The large items (fruit trees, trees, giant crops, and resource clumps) in your inventory are a custom subclass and I use SpaceCore's serializer for them. Once placed, however, they're the exact same as every other resource clump and/or tree. If you remove this mod, those will remain and probably be fine. Resource clumps may disappear if you don't have another mod persisting them.
* Small items like the rocks and twigs are actually in the game normally and are fine, although you won't be able to place them if you remove this mod. The various grass starters from this mod will revert to being normal grass starters.
* This mod prevents decor weeds from spreading, etc. If you remove it, your decorative weeds will start to spread.
* You should be able to target everything, after placement, with [Alternative Textures](https://www.nexusmods.com/stardewvalley/mods/9246).
* If you use giant crops or resource clumps to block of NPC pathing, it'll definitely break things. Don't do that.
* **A performance note**: The game is NOT optimized for having a lot of resource clumps everywhere. If you place many giant crops or resource clumps on a map where there are also a lot of NPCs/monsters trying to path, expect slowdown.

## Compatibility

* Works with Stardew Valley 1.5.6 on Linux/macOS/Windows.
* Works in single player, multiplayer, and split-screen mode. **Absolutely** has to be installed by everyone in multiplayer.
* Should be compatible with most other mods. Tested with [Json Assets](https://www.nexusmods.com/stardewvalley/mods/1720)'s giant crops and [More Giant Crops](https://www.nexusmods.com/stardewvalley/mods/5263), as well as giant crops from [Giant Crop Tweaks](https://www.nexusmods.com/stardewvalley/mods/14370).
* Specific compatibility notes: This mod uses SpaceCore's serializer.
    - I did not test this mod with Save Anywhere (either version), use at your own risk.
    - It should work fine with SolidFoundations, just remember to remove all instances of the inventory version of the bushes from all SolidFoundations buildings before removing this mod. (Placed bushes should be fine to leave.)

## See also

[Changelog](docs/changelog.md)
