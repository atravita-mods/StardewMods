Documentation
=============

[← back to readme](../../README.md)

Most users should download the mod from the [Nexus page](https://www.nexusmods.com/stardewvalley/mods/15559). That said, if you'd like to contribute:

### Translations:

This mod uses SMAPI's i18n feature for translations. I'd love to get translations! Please see the wiki's guide [here](https://stardewvalleywiki.com/Modding:Translations), and feel free to message me, contact me on Discord (@atravita#9505) or send me a pull request!

### Textures

You can edit the following textures using Content Patcher.
* Shovel: `Mods/atravita.GrowableGiantCrops/Shovel`
* Shop graphics: `Mods/atravita.GrowableGiantCrops/Shop`
* Palm trees: `Mods/atravita.GrowableGiantCrops/WinterPalm`, `Mods/atravita.GrowableGiantCrops/WinterBigPalm`, `Mods/atravita.GrowableGiantCrops/FallPalm`, and `Mods/atravita.GrowableGiantCrops/FallBigPalm`, 

### Shovel

Got a spawned item that you really, really do not want the player to be able to pick up? Simply add the tile property `"atravita.ShovelForbidden"` on the `"Back"` layer. The value should either be:
    - Whitespace (not empty) for a default message
    - A message that does not contain `:`.
    - A message in the form `"NPCName: message"`, which will display as if from that NPC.

### Compiling from source:

3. Fork [this repository](https://github.com/atravita-mods/StardewMods).
4. Make sure you have [dotnet-5.0-sdk](https://dotnet.microsoft.com/en-us/download/dotnet/5.0) installed.
5. If your copy of the game is not in the standard STEAM or Gog install locations, you may need to edit the csproj to point at it. [Instructions here](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md#available-properties).

This project uses Pathos' multiplatform nuget, so it **should** build on any platform, although admittedly I've only tried it with Windows.