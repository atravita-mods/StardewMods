Contributions README
====================================

Most users should download the mod from the [Nexus page](https://www.nexusmods.com/stardewvalley/mods/12932). That said, if you'd like to contribute:

### Features

#### Equipment Effects

#### Event Commands

#### Game State Queries

### Translations:

This mod uses SMAPI's i18n feature for translations. I'd love to get translations! Please see the wiki's guide [here](https://stardewvalleywiki.com/Modding:Translations), and feel free to message me, contact me on Discord (@atravita) or send me a pull request!

### Compiling from source:

3. Fork [this repository](https://github.com/atravita-mods/StardewMods).
4. Make sure you have [dotnet-8.0-sdk](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed. I do use features from langversion 12, which requires this sdk, minimum.
5. If your copy of the game is not in the standard STEAM or Gog install locations, you may need to edit the csproj to point at it. [Instructions here](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md#available-properties).

This project uses Pathos' multiplatform nuget, so it **should** build on any platform, although admittedly I've only tried it with Windows.

### Referencing AtraCore from other C# mods

....is not recommended, since AtraCore is still under development and features may change without warning. Just because a function is `public` does not mean I intend for it to be in the public API. If it's not documented here, don't use it.

(Most of the useful stuff is in AtraBase anyways, which I'm keeping as its own [separate repo](https://github.com/atravita-mods/AtraBase), so you could just [submodule that](https://git-scm.com/book/en/v2/Git-Tools-Submodules) :P)