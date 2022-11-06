Holiday Sales
===========================
![Header image](docs/letter.png)

Look, I just wanted a mod of this type to handle the fertilizers from [More Fertilizers](../More Fertilizers/More Fertilizers), okay?

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download and install [AtraCore](https://www.nexusmods.com/stardewvalley/mods/12932).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Uninstall
Simply delete from your Mods directory.

## Configuration
Run SMAPI at least once with this mod installed to generate the `config.json`, or use [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) to configure.


## Technical notes:

* The process used to calculate the number of days for each plant under each condition is fairly slow, and there's nothing I can realistically do about the fact that it's an `O(mn)` problem. I cache as much as I can, but on my computer first startup takes about 400-500 ms.
* If you change professions, you'll temporarily see timing for both your old profession and new profession. This is because I haven't found a good way to 

## Compatibility

* Works with Stardew Valley 1.5.6 on Linux/macOS/Windows.
* Works in single player, multiplayer, and split-screen mode. Should be fine if installed for only one player in multiplayer.
* Should be compatible with most other mods.

## See also

[Changelog](docs/changelog.md)
